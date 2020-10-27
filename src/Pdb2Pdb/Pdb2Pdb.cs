// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
            public readonly string? PdbFilePath;
            public readonly string? OutPdbFilePath;
            public readonly PortablePdbConversionOptions Options;
            public readonly bool Extract;
            public readonly ImmutableArray<PdbDiagnosticId> SuppressedWarnings;
            public readonly bool SuppressAllWarnings;

            public Args(string peFilePath, string? pdbFilePath, string? outPdbFilePath, PortablePdbConversionOptions options, ImmutableArray<PdbDiagnosticId> suppressedWarnings, bool suppressAllWarnings, bool extract)
            {
                PEFilePath = peFilePath;
                PdbFilePath = pdbFilePath;
                OutPdbFilePath = outPdbFilePath;
                Options = options;
                Extract = extract;
                SuppressAllWarnings = suppressAllWarnings;
                SuppressedWarnings = suppressedWarnings;
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
            string? peFile = null;
            bool extract = false;
            bool sourceLink = false;
            string? inPdb = null;
            string? outPdb = null;
            bool suppressAllWarnings = false;
            var suppressedWarnings = new List<PdbDiagnosticId>();
            var srcSvrVariables = new List<KeyValuePair<string, string>>();

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

                    case "/pdb":
                        inPdb = ReadValue();
                        break;

                    case "/out":
                        outPdb = ReadValue();
                        break;

                    case "/nowarn":
                        var value = ReadValue();
                        if (value == "*")
                        {
                            suppressAllWarnings = true;
                        }
                        else
                        {
                            suppressedWarnings.AddRange(value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(
                            n => (int.TryParse(n, out int id) && id != 0 && ((PdbDiagnosticId)id).IsValid()) ?
                                (PdbDiagnosticId)id :
                                throw new InvalidDataException(string.Format(Resources.InvalidWarningNumber, n))));
                        }

                        break;

                    case "/srcsvrvar":
                    case "/srcsvrvariable":
                        srcSvrVariables.Add(ParseSrcSvrVariable(ReadValue()));
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

            if (sourceLink && srcSvrVariables.Count > 0)
            {
                throw new InvalidDataException(Resources.CantSpecifyBothSrcSvrVariableAndSourcelinkOptions);
            }

            PortablePdbConversionOptions options;
            try
            {
                options = new PortablePdbConversionOptions(
                    suppressSourceLinkConversion: sourceLink,
                    srcSvrVariables: srcSvrVariables);
            }
            catch (ArgumentException e)
            {
                throw new InvalidDataException(e.Message);
            }

            return new Args(peFile, inPdb, outPdb, options, suppressedWarnings.ToImmutableArray(), suppressAllWarnings, extract);
        }

        private static KeyValuePair<string, string> ParseSrcSvrVariable(string value)
        {
            int eq = value.IndexOf('=');
            if (eq < 0)
            {
                return default;
            }

            return new KeyValuePair<string, string>(value.Substring(0, eq), value.Substring(eq + 1));
        }

        // internal for testing
        internal static bool Convert(Args args)
        {
            bool success = true;

            Action<PdbDiagnostic>? reporter;
            if (args.SuppressAllWarnings)
            {
                reporter = null;
            }
            else
            {
                var suppressedWarnings = new HashSet<PdbDiagnosticId>(args.SuppressedWarnings);
                reporter = d =>
                {
                    if (!suppressedWarnings.Contains(d.Id))
                    {
                        Console.Error.WriteLine(d.ToString(CultureInfo.CurrentCulture));
                        success = false;
                    }
                };
            }

            var converter = new PdbConverter(reporter);

            using (var peStream = new FileStream(args.PEFilePath, FileMode.Open, FileAccess.Read))
            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                string? portablePdbFileCandidate = null;

                if (args.PdbFilePath != null)
                {
                    Debug.Assert(!args.Extract);
                    NullableDebug.Assert(args.OutPdbFilePath != null);

                    using (var srcPdbStreamOpt = OpenFileForRead(args.PdbFilePath))
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

                        WriteAllBytes(args.OutPdbFilePath, outPdbStream);
                    }
                }
                else if (peReader.TryOpenAssociatedPortablePdb(args.PEFilePath, path => File.OpenRead(portablePdbFileCandidate = path), out var pdbReaderProvider, out _))
                {
                    using (pdbReaderProvider)
                    {
                        var pdbReader = pdbReaderProvider!.GetMetadataReader();
                        if (args.Extract)
                        {
                            string pdbPath = 
                                args.OutPdbFilePath ??
                                GetPdbPathFromCodeViewEntry(peReader, args.PEFilePath, portable: true) ?? 
                                Path.ChangeExtension(args.PEFilePath, "pdb");

                            File.WriteAllBytes(pdbPath, ReadAllBytes(pdbReader));
                        }
                        else
                        {
                            NullableDebug.Assert(args.OutPdbFilePath != null);

                            var dstPdbStream = new MemoryStream();
                            converter.ConvertPortableToWindows(peReader, pdbReader, dstPdbStream, args.Options);
                            WriteAllBytes(args.OutPdbFilePath, dstPdbStream);
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
                    NullableDebug.Assert(args.OutPdbFilePath != null);

                    // We don't have Portable PDB nor Embedded PDB. Try to find Windows PDB.

                    var path = GetPdbPathFromCodeViewEntry(peReader, args.PEFilePath, portable: false);
                    if (path == null)
                    {
                        throw new IOException(string.Format(Resources.NoAssociatedOrEmbeddedPdb, args.PEFilePath));
                    }

                    using (var srcPdbStreamOpt = OpenFileForRead(path))
                    {
                        var outPdbStream = new MemoryStream();
                        converter.ConvertWindowsToPortable(peReader, srcPdbStreamOpt, outPdbStream);
                        WriteAllBytes(args.OutPdbFilePath, outPdbStream);
                    }
                }
            }

            return success;
        }

        private static string? GetPdbPathFromCodeViewEntry(PEReader peReader, string peFilePath, bool portable)
        {
            var directory = peReader.ReadDebugDirectory();

            var codeViewEntry = directory.LastOrDefault(entry => entry.Type == DebugDirectoryEntryType.CodeView && entry.IsPortableCodeView == portable);
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
