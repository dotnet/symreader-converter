// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.Metadata.Tools;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    internal static class PdbValidationMetadata
    {
        public static void VerifyPortablePdb(TestResource portable, TestResource windows, string expectedMetadata)
        {
            VerifyPortableMatchesExpected(portable, expectedMetadata);
            VerifyPortableConvertedFromWindowsMatchesExpected(windows, expectedMetadata);
        }

        private static void VerifyPortableMatchesExpected(TestResource portable, string expectedMetadata)
        {
            VerifyPortablePdb(
                new MemoryStream(portable.Pdb), 
                SelectAlternative(expectedMetadata, leftAlternative: true), 
                "Comparing Portable PDB with expected metadata");
        }

        private static void VerifyPortableConvertedFromWindowsMatchesExpected(TestResource windows, string expectedMetadata)
        {
            var windowsPEStream = new MemoryStream(windows.PE);
            var windowsPdbStream = new MemoryStream(windows.Pdb);
            var convertedPortablePdbStream = new MemoryStream();

            var converter = new PdbConverter(d => Assert.False(true, d.ToString()));
            converter.ConvertWindowsToPortable(windowsPEStream, windowsPdbStream, convertedPortablePdbStream);

            convertedPortablePdbStream.Position = 0;
            VerifyPortablePdb(
                convertedPortablePdbStream,
                SelectAlternative(expectedMetadata, leftAlternative: false), 
                "Comparing Portable PDB converted from Windows PDB with expected metadata");
        }

        private static void VerifyPortablePdb(Stream pdbStream, string expectedMetadata, string message)
        {
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
            {
                var mdReader = provider.GetMetadataReader();
                var writer = new StringWriter();
                var mdv = new MetadataVisualizer(mdReader, writer, MetadataVisualizerOptions.NoHeapReferences);

                mdv.WriteDocument();
                mdv.WriteMethodDebugInformation();
                mdv.WriteLocalScope();
                mdv.WriteLocalVariable();
                mdv.WriteLocalConstant();
                mdv.WriteImportScope();
                mdv.WriteCustomDebugInformation();

                AssertEx.AssertLinesEqual(expectedMetadata, writer.ToString(), message);
            }
        }

        private static string SelectAlternative(string expected, bool leftAlternative)
        {
            const string startMarker = "«";
            const string midMarker = "»«";
            const string endMarker = "»";

            if (!expected.Contains(startMarker))
            {
                return expected;
            }

            int offset = 0;
            var builder = new StringBuilder();
            while (true)
            {
                int start = expected.IndexOf(startMarker, offset);
                if (start < 0)
                {
                    builder.Append(expected, offset, expected.Length - offset);
                    return builder.ToString();
                }

                int commonLength = start - offset;
                int leftStart = start + startMarker.Length;
                int separator = expected.IndexOf(midMarker, leftStart);
                int leftLength = separator - leftStart;
                int rightStart = separator + midMarker.Length;
                int end = expected.IndexOf(endMarker, rightStart);
                int rightLength = end - rightStart;

                Assert.True(separator > 0);
                Assert.True(end > 0);

                builder.Append(expected, offset, commonLength);

                if (leftAlternative)
                {
                    builder.Append(expected, leftStart, leftLength);
                }
                else
                {
                    builder.Append(expected, rightStart, rightLength);
                }

                offset = end + 1;
            }
        }

        //private static bool CompareLines(string expected, string actual) => 
        //    expected.Trim() == actual.Trim();

        //private static bool CompareLines(string expected, string actual, bool leftAlternative)
        //{
        //    const string startMarker = "«";
        //    const string midMarker = "»«";
        //    const string endMarker = "»";

        //    int altStart = expected.IndexOf(startMarker);
        //    if (altStart < 0)
        //    {
        //        return CompareLines(expected, actual);
        //    }

        //    int altMid = expected.IndexOf(midMarker, altStart + startMarker.Length);
        //    int altEnd = expected.IndexOf(endMarker, altMid + midMarker.Length);

        //    Assert.True(altMid > 0);
        //    Assert.True(altEnd > 0);

        //    var prefix = expected.Substring(0, altStart);
        //    var suffix = expected.Substring(altEnd + endMarker.Length);

        //    var alt = leftAlternative ? 
        //        expected.Substring(altStart + startMarker.Length, altMid - (altStart + startMarker.Length)) : 
        //        expected.Substring(altMid + midMarker.Length, altEnd - (altMid + midMarker.Length));

        //    return CompareLines(prefix + alt + suffix, actual);
        //}
    }
}
