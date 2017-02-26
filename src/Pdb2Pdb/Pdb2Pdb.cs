// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class Pdb2Pdb
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine(Resources.Pdb2PdbUsage);
                return 1;
            }

            string peFile = args[0];
            var srcPdb = GetArgumentValue(args, "/src:") ?? Path.ChangeExtension(peFile, "pdb");
            var dstPdb = GetArgumentValue(args, "/dst:") ?? Path.ChangeExtension(peFile, "pdb2");

            if (!File.Exists(peFile))
            {
                Console.Error.WriteLine(string.Format(Resources.FileNotFound, peFile));
                return 2;
            }

            if (!File.Exists(srcPdb))
            {
                Console.Error.WriteLine(string.Format(Resources.FileNotFound, srcPdb));
                return 2;
            }

            try
            {
                using (var peStream = new FileStream(peFile, FileMode.Open, FileAccess.Read))
                using (var srcPdbStream = new FileStream(srcPdb, FileMode.Open, FileAccess.Read))
                using (var dstPdbStream = new FileStream(dstPdb, FileMode.Create, FileAccess.ReadWrite))
                {
                    PdbConverter.Convert(peStream, srcPdbStream, dstPdbStream);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 3;
            }

            return 0;
        }

        private static string GetArgumentValue(string[] args, string prefix)
        {
            return args.
                Where(arg => arg.StartsWith(prefix, StringComparison.Ordinal)).
                Select(arg => arg.Substring(prefix.Length)).LastOrDefault();
        }
    }
}
