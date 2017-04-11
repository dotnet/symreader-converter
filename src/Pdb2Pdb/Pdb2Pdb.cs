// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class Pdb2Pdb
    {
        private sealed class Args
        {
            public readonly string PEFilePath;
            public readonly bool Extract;
            public readonly string PdbFilePathOpt;
            public readonly string OutPdbFilePath;

            public Args(string pEFilePath, bool extract, string pdbFilePathOpt, string outPdbFilePath)
            {
                PEFilePath = pEFilePath;
                Extract = extract;
                PdbFilePathOpt = pdbFilePathOpt;
                OutPdbFilePath = outPdbFilePath;
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

            return Convert(parsedArgs);
        }

        private static Args ParseArgs(string[] args)
        {
            string peFile = null;
            bool extract = false;
            string inPdb = null;
            string outPdb = null;

            int i = 0;
            while (i < args.Length)
            {
                var arg = args[i++];

                string ReadValue() => (i < args.Length) ? args[i] : throw new InvalidDataException(string.Format(Resources.MissingValueForOption, arg));

                switch (arg)
                {
                   case "/extract":
                        extract = true;
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

            if (outPdb == null)
            {
                outPdb = Path.ChangeExtension(peFile, "pdb2");
            }

            if (extract && inPdb != null)
            {
                throw new InvalidDataException(Resources.CantSpecifyBothExtractAndPdbOptions);
            }

            if (!File.Exists(peFile))
            {
                throw new InvalidDataException(string.Format(Resources.FileNotFound, peFile));
            }

            return new Args(peFile, extract, inPdb, outPdb);
        }

        private static int Convert(Args args)
        {
            var converter = new PdbConverter();

            try
            {
                using (var peStream = new FileStream(args.PEFilePath, FileMode.Open, FileAccess.Read))
                using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
                {
                    if (args.PdbFilePathOpt != null)
                    {
                        using (var srcPdbStreamOpt = new FileStream(args.PdbFilePathOpt, FileMode.Open, FileAccess.Read))
                        {
                            var outPdbStream = new MemoryStream();
                            if (PdbConverter.IsPortable(srcPdbStreamOpt))
                            {
                                converter.ConvertPortableToWindows(peReader, srcPdbStreamOpt, outPdbStream);
                            }
                            else
                            {
                                converter.ConvertWindowsToPortable(peReader, srcPdbStreamOpt, outPdbStream);
                            }

                            WriteAllBytes(args.OutPdbFilePath, outPdbStream);
                        }
                    }
                    else if (peReader.TryOpenAssociatedPortablePdb(args.PEFilePath, File.OpenRead, out var pdbReaderProvider, out string associatedPdbPathOpt))
                    {
                        using (pdbReaderProvider)
                        {
                            var pdbReader = pdbReaderProvider.GetMetadataReader();
                            if (args.Extract)
                            {
                                File.WriteAllBytes(args.OutPdbFilePath, ReadAllBytes(pdbReader));
                            }
                            else
                            {
                                var dstPdbStream = new MemoryStream();
                                converter.ConvertPortableToWindows(peReader, pdbReader, dstPdbStream);
                                WriteAllBytes(args.OutPdbFilePath, dstPdbStream);
                            }
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine(associatedPdbPathOpt != null ?
                            string.Format(Resources.FileNotFound, associatedPdbPathOpt) :
                            string.Format(Resources.FileDoesntContainEmbeddedPdb, args.PEFilePath));

                        return 4;
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 3;
            }

            return 0;
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
