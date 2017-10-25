// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class Pdb2Pdb
    {
        // internal for testing
        internal sealed class Args
        {
            public readonly string PEFilePath;
            public readonly string PdbFilePathOpt;
            public readonly string OutPdbFilePathOpt;
            public readonly PdbConversionOptions Options;
            public readonly bool Extract;
            public readonly bool Verbose;

            public Args(string peFilePath, string pdbFilePathOpt, string outPdbFilePathOpt, PdbConversionOptions options, bool extract, bool verbose)
            {
                PEFilePath = peFilePath;
                PdbFilePathOpt = pdbFilePathOpt;
                OutPdbFilePathOpt = outPdbFilePathOpt;
                Options = options;
                Extract = extract;
                Verbose = verbose;
            }
        }

        public static int Main(string[] args)
        {
            Args parsedArgs;
            try
            {
                parsedArgs = ParseArgs(args);
            }
            catch (InvalidDataException e)
            {
                Console.Error.WriteLine(Resources.Pdb2PdbUsage);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.Message);
                return 1;
            }

            try
            {
                return Convert(parsedArgs) ? 0 : -1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 2;
            }
        }

        // internal for testing
        internal static Args ParseArgs(string[] args)
        {
            string peFile = null;
            bool extract = false;
            bool sourceLink = false;
            bool verbose = false;
            string inPdb = null;
            string outPdb = null;

            int i = 0;
            while (i < args.Length)
            {
                var arg = args[i++];

                string ReadValue() => (i < args.Length) ? args[i++] : throw new InvalidDataException(string.Format(Resources.MissingValueForOption, arg));

                switch (arg)
                {
                   case "/extract":
                        extract = true;
                        break;

                    case "/sourcelink":
                        sourceLink = true;
                        break;

                    case "/verbose":
                        verbose = true;
                        break;

                    case "/pdb":
                        inPdb = ReadValue();
                        break;

                    case "/out":
                        outPdb = ReadValue();
                        break;

                    default:
                        if (peFile == null)
                        {
                            peFile = arg;
                        }
                        else 
                        {
                            throw new InvalidDataException((arg.StartsWith("/", StringComparison.Ordinal) ? 
                                string.Format(Resources.UnrecognizedOption, arg) : 
                                Resources.OnlyOneDllExeCanBeSpecified));
                        }
                        break;
                }
            }

            if (peFile == null)
            {
                throw new InvalidDataException(Resources.MissingDllExePath);
            }

            if (!extract && outPdb == null)
            {
                try
                {
                    outPdb = Path.ChangeExtension(peFile, "pdb2");
                }
                catch (Exception e)
                {
                    throw new InvalidDataException(e.Message);
                }
            }

            if (extract && sourceLink)
            {
                throw new InvalidDataException(Resources.CantSpecifyBothExtractAndSourcelinkOptions);
            }

            if (extract && inPdb != null)
            {
                throw new InvalidDataException(Resources.CantSpecifyBothExtractAndPdbOptions);
            }

            var options = default(PdbConversionOptions);
            if (sourceLink)
            {
                options |= PdbConversionOptions.SuppressSourceLinkConversion;
            }

            return new Args(peFile, inPdb, outPdb, options, extract, verbose);
        }

        // internal for testing
        internal static bool Convert(Args args)
        {
            bool success = true;
            var reporter = args.Verbose ? new Action<PdbDiagnostic>(d => 
            {
                Console.Error.WriteLine(d.ToString(CultureInfo.CurrentCulture));
                success = false;
            }) : null;

            var converter = new PdbConverter(reporter);

            using (var peStream = new FileStream(args.PEFilePath, FileMode.Open, FileAccess.Read))
            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                string portablePdbFileCandidate = null;

                if (args.PdbFilePathOpt != null)
                {
                    Debug.Assert(!args.Extract);
                    Debug.Assert(args.OutPdbFilePathOpt != null);

                    using (var srcPdbStreamOpt = OpenFileForRead(args.PdbFilePathOpt))
                    {
                        var outPdbStream = new MemoryStream();
                        if (PdbConverter.IsPortable(srcPdbStreamOpt))
                        {
                            converter.ConvertPortableToWindows(peReader, srcPdbStreamOpt, outPdbStream, args.Options);
                        }
                        else
                        {
                            converter.ConvertWindowsToPortable(peReader, srcPdbStreamOpt, outPdbStream);
                        }

                        WriteAllBytes(args.OutPdbFilePathOpt, outPdbStream);
                    }
                }
                else if (peReader.TryOpenAssociatedPortablePdb(args.PEFilePath, path => File.OpenRead(portablePdbFileCandidate = path), out var pdbReaderProvider, out _))
                {
                    using (pdbReaderProvider)
                    {
                        var pdbReader = pdbReaderProvider.GetMetadataReader();
                        if (args.Extract)
                        {
                            string pdbPath = 
                                args.OutPdbFilePathOpt ??
                                GetPdbPathFromCodeViewEntry(peReader, args.PEFilePath, portable: true) ?? 
                                Path.ChangeExtension(args.PEFilePath, "pdb");

                            File.WriteAllBytes(pdbPath, ReadAllBytes(pdbReader));
                        }
                        else
                        {
                            Debug.Assert(args.OutPdbFilePathOpt != null);

                            var dstPdbStream = new MemoryStream();
                            converter.ConvertPortableToWindows(peReader, pdbReader, dstPdbStream, args.Options);
                            WriteAllBytes(args.OutPdbFilePathOpt, dstPdbStream);
                        }
                    }
                }
                else if (portablePdbFileCandidate != null)
                {
                    throw new FileNotFoundException(string.Format(Resources.MatchingPdbNotFound, portablePdbFileCandidate, args.PEFilePath), portablePdbFileCandidate);
                }
                else if (args.Extract)
                {
                    throw new IOException(string.Format(Resources.FileDoesntContainEmbeddedPdb, args.PEFilePath));
                }
                else
                {
                    Debug.Assert(args.OutPdbFilePathOpt != null);

                    // We don't have Portable PDB nor Embedded PDB. Try to find Windows PDB.

                    string path = GetPdbPathFromCodeViewEntry(peReader, args.PEFilePath, portable: false);
                    if (path == null)
                    {
                        throw new IOException(string.Format(Resources.NoAssociatedOrEmbeddedPdb, args.PEFilePath));
                    }

                    using (var srcPdbStreamOpt = OpenFileForRead(path))
                    {
                        var outPdbStream = new MemoryStream();
                        converter.ConvertWindowsToPortable(peReader, srcPdbStreamOpt, outPdbStream);
                        WriteAllBytes(args.OutPdbFilePathOpt, outPdbStream);
                    }
                }
            }

            return success;
        }

        private static string GetPdbPathFromCodeViewEntry(PEReader peReader, string peFilePath, bool portable)
        {
            var directory = peReader.ReadDebugDirectory();

            var codeViewEntry = directory.FirstOrDefault(entry => entry.Type == DebugDirectoryEntryType.CodeView && entry.IsPortableCodeView == portable);
            if (codeViewEntry.DataSize == 0)
            {
                return null;
            }

            var data = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);

            try
            {
                return Path.Combine(Path.GetDirectoryName(peFilePath), Path.GetFileName(data.Path));
            }
            catch (Exception)
            {
                throw new BadImageFormatException(string.Format(Resources.InvalidPdbPathInDebugDirectory, peFilePath));
            }
        }

        private static FileStream OpenFileForRead(string path)
        {
            try
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read);
            }
            catch (Exception e) when (!(e is IOException))
            {
                throw new FileNotFoundException(string.Format(Resources.FileNotFound, path), path);
            }
        }

        private static void WriteAllBytes(string path, Stream stream)
        {
            // Create the file once we know we have successfully converted the PDB:
            using (var dstFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                stream.Position = 0;
                stream.CopyTo(dstFileStream);
            }
        }

        private unsafe static byte[] ReadAllBytes(MetadataReader reader)
        {
            var buffer = new byte[reader.MetadataLength];
            fixed (byte* bufferPtr = &buffer[0])
            {
                Buffer.MemoryCopy(reader.MetadataPointer, bufferPtr, buffer.Length, buffer.Length);
            }

            return buffer;
        }
    }
}
