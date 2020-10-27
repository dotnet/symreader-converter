// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class PdbToXmlApp
    {
        internal sealed class Args
        {
            public readonly string InputPath;
            public readonly string OutputPath;
            public readonly PdbToXmlOptions Options;
            public readonly bool Delta;

            public Args(string inputPath, string outputPath, bool delta, PdbToXmlOptions options)
            {
                InputPath = inputPath;
                OutputPath = outputPath;
                Delta = delta;
                Options = options;
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
                Console.Error.WriteLine("Usage: Pdb2Xml <PEFile | DeltaPdb> [/out <output file>] [/tokens] [/methodSpans] [/delta] [/srcsvr] [/sources] [/native]");
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.Message);
                return 1;
            }

            try
            {
                Convert(parsedArgs);
                Console.WriteLine($"PDB dump written to {parsedArgs.OutputPath}");
                return 0;
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
            string? inputPath = null;
            string? outputPath = null;
            bool delta = false;
            var options = PdbToXmlOptions.ResolveTokens | PdbToXmlOptions.IncludeModuleDebugInfo;

            int i = 0;
            while (i < args.Length)
            {
                var arg = args[i++];

                string ReadValue() => (i < args.Length) ? args[i++] : throw new InvalidDataException(string.Format("Missing value for option '{0}'", arg));

                switch (arg)
                {
                    case "/out":
                        outputPath = ReadValue();
                        break;

                    case "/delta":
                        delta = true;
                        break;

                    case "/tokens":
                        options |= PdbToXmlOptions.IncludeTokens;
                        break;

                    case "/methodSpans":
                        options |= PdbToXmlOptions.IncludeMethodSpans;
                        break;

                    case "/srcsvr":
                        options |= PdbToXmlOptions.IncludeSourceServerInformation;
                        break;

                    case "/sources":
                        options |= PdbToXmlOptions.IncludeEmbeddedSources;
                        break;

                    case "/native":
                        options |= PdbToXmlOptions.UseNativeReader;
                        break;

                    default:
                        if (inputPath == null)
                        {
                            inputPath = arg;
                        }
                        else
                        {
                            throw new InvalidDataException((arg.StartsWith("/", StringComparison.Ordinal) ?
                                string.Format("Unrecognized option: '{0}'", arg) :
                                "Only one input file path can be specified"));
                        }
                        break;
                }
            }

            if (inputPath == null)
            {
                throw new InvalidDataException("Missing input file path.");
            }

            if (outputPath == null)
            {
                try
                {
                    outputPath = Path.ChangeExtension(inputPath, "xml");
                }
                catch (Exception e)
                {
                    throw new InvalidDataException(e.Message);
                }
            }

            return new Args(
                inputPath: inputPath,
                outputPath: outputPath,
                delta: delta,
                options: options);
        }

        public static void Convert(Args args)
        {
            string? peFile;
            string? pdbFile;

            if (args.Delta)
            {
                peFile = null;
                pdbFile = args.InputPath;
            }
            else
            {
                peFile = args.InputPath;
                pdbFile = Path.ChangeExtension(args.InputPath, ".pdb");
            }

            if (peFile != null && !File.Exists(peFile))
            {
                throw new FileNotFoundException($"File not found: {peFile}");
            }

            if (!File.Exists(pdbFile))
            {
                throw new FileNotFoundException($"PDB File not found: {pdbFile}");
            }

            if (args.Delta)
            {
                GenXmlFromDeltaPdb(pdbFile, args.OutputPath);
            }
            else
            {
                GenXmlFromPdb(peFile!, pdbFile, args.OutputPath, args.Options);
            }
        }

        public static void GenXmlFromPdb(string exePath, string pdbPath, string outPath, PdbToXmlOptions options)
        {
            using var peStream = new FileStream(exePath, FileMode.Open, FileAccess.Read);
            using var pdbStream = new FileStream(pdbPath, FileMode.Open, FileAccess.Read);
            using var dstFileStream = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite);
            using var sw = new StreamWriter(dstFileStream, Encoding.UTF8);

            PdbToXmlConverter.ToXml(sw, pdbStream, peStream, options);
        }

        public static void GenXmlFromDeltaPdb(string pdbPath, string outPath)
        {
            using var deltaPdb = new FileStream(pdbPath, FileMode.Open, FileAccess.Read);

            // There is no easy way to enumerate all method tokens that are present in the PDB.
            // So dump the first 255 method tokens (the ones that are not present will be skipped):
            File.WriteAllText(outPath, PdbToXmlConverter.DeltaPdbToXml(deltaPdb, Enumerable.Range(0x06000001, 255)));
        }
    }
}