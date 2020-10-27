// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    internal static class PdbValidationXml
    {
        private const PdbToXmlOptions Options = PdbToXmlOptions.IncludeSourceServerInformation | PdbToXmlOptions.IncludeEmbeddedSources | PdbToXmlOptions.ResolveTokens;

        public static void VerifyWindowsPdb(TestResource portable, TestResource windows, string expectedXml, PdbDiagnostic[]? expectedDiagnostics = null, PortablePdbConversionOptions? options = null)
        {
            VerifyWindowsMatchesExpected(windows, expectedXml);
            // TODO: VerifyPortableReadNativelyMatchesExpected(portable, expectedXml);
            VerifyWindowsConvertedFromPortableMatchesExpected(portable, expectedXml, expectedDiagnostics, options, validateTimeIndifference: false);
        }

        public static void VerifyWindowsMatchesExpected(TestResource windows, string expectedXml)
        {
            var windowsPEStream = new MemoryStream(windows.PE);
            var windowsPdbStream = new MemoryStream(windows.Pdb);
            var actualXml = PdbToXmlConverter.ToXml(windowsPdbStream, windowsPEStream, Options);

            var adjustedExpectedXml = AdjustForInherentDifferences(expectedXml);
            var adjustedActualXml = AdjustForInherentDifferences(actualXml);

            AssertEx.AssertLinesEqual(adjustedExpectedXml, adjustedActualXml, "Comparing Windows PDB with expected XML");
        }

        public static void VerifyPortablePdb(
            TestResource portable,
            string expectedXml,
            PdbToXmlOptions options = Options)
        {
            var portablePEStream = new MemoryStream(portable.PE);
            var portablePdbStream = new MemoryStream(portable.Pdb);
            var actualXml = PdbToXmlConverter.ToXml(portablePdbStream, portablePEStream, options);

            AssertEx.AssertLinesEqual(expectedXml, actualXml, "Comparing Portable PDB with expected XML");
        }

        public static void VerifyPortableReadNativelyMatchesExpected(TestResource portable, string expectedXml)
        {
            var portablePEStream = new MemoryStream(portable.PE);
            var portablePdbStream = new MemoryStream(portable.Pdb);
            var actualXml = PdbToXmlConverter.ToXml(portablePdbStream, portablePEStream, Options | PdbToXmlOptions.UseNativeReader);

            var adjustedActualXml = RemoveElementsNotSupportedByNativeReader(actualXml);
            var adjustedExpectedXml = RemoveElementsNotSupportedByNativeReader(expectedXml);

            AssertEx.AssertLinesEqual(adjustedExpectedXml, adjustedActualXml, "Comparing Portable PDB read via native reader with expected XML");
        }

        private static string RemoveElementsNotSupportedByNativeReader(string xml)
        {
            var element = XElement.Parse(xml);

            RemoveElements(from e in element.DescendantsAndSelf()
                           where e.Name == "customDebugInfo" ||
                                 e.Name == "scope" ||
                                 e.Name == "asyncInfo"
                           select e);

            foreach (var e in element.DescendantsAndSelf())
            {
                if (e.Name == "file")
                {
                    e.Attribute("languageVendor")?.Remove();
                    e.Attribute("documentType")?.Remove();
                }
            }

            return element.ToString();
        }

        public static void VerifyWindowsConvertedFromPortableMatchesExpected(TestResource portable, string expectedXml, PdbDiagnostic[]? expectedDiagnostics, PortablePdbConversionOptions? options, bool validateTimeIndifference)
        {
            var portablePEStream = new MemoryStream(portable.PE);
            var portablePdbStream = new MemoryStream(portable.Pdb);
            var portablePdbStream2 = new MemoryStream(portable.Pdb);
            var convertedWindowsPdbStream1 = new MemoryStream();
            var convertedWindowsPdbStream2 = new MemoryStream();
            var actualDiagnostics = new List<PdbDiagnostic>();

            var converter = new PdbConverter(actualDiagnostics.Add);
            converter.ConvertPortableToWindows(portablePEStream, portablePdbStream, convertedWindowsPdbStream1, options);
            AssertEx.Equal(expectedDiagnostics ?? Array.Empty<PdbDiagnostic>(), actualDiagnostics, itemInspector: InspectDiagnostic);

            VerifyPdb(convertedWindowsPdbStream1, portablePEStream, expectedXml, "Comparing Windows PDB converted from Portable PDB with expected XML");

            portablePdbStream.Position = 0;
            convertedWindowsPdbStream1.Position = 0;
            VerifyMatchingSignatures(portablePdbStream, convertedWindowsPdbStream1);

            // validate determinism:
            if (validateTimeIndifference)
            {
                Thread.Sleep(1000);
            }

            portablePEStream.Position = 0;
            portablePdbStream.Position = 0;
            converter.ConvertPortableToWindows(portablePEStream, portablePdbStream, convertedWindowsPdbStream2, options);
            AssertEx.Equal(convertedWindowsPdbStream1.ToArray(), convertedWindowsPdbStream2.ToArray());
        }

        private static string InspectDiagnostic(PdbDiagnostic diagnostic)
        {
            var args = diagnostic.Args != null ? $", new[] {{ { string.Join(", ", diagnostic.Args.Select(a => "\"" + a + "\""))} }}" : null;
            var token = diagnostic.Token != 0 ? $"0x{diagnostic.Token:X8}" : "0";
            return $"new PdbDiagnostic(PdbDiagnosticId.{diagnostic.Id}, {token}{args})";
        }

        private static void VerifyMatchingSignatures(Stream portablePdbStream, Stream windowsPdbStream)
        {
            Guid guid;
            uint stamp;
            int age;
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(portablePdbStream, MetadataStreamOptions.LeaveOpen))
            {
                SymReaderHelpers.GetWindowsPdbSignature(provider.GetMetadataReader().DebugMetadataHeader!.Id, out guid, out stamp, out age);
            }

            var symReader = SymReaderHelpers.CreateWindowsPdbReader(windowsPdbStream);
            try
            {
                Marshal.ThrowExceptionForHR(symReader.MatchesModule(guid, stamp, age, out bool result));
                Assert.True(result);
            }
            finally
            {
                ((ISymUnmanagedDispose)symReader).Destroy();
            }
        }

        private static bool RemoveElements(IEnumerable<XElement> elements)
        {
            var array = elements.ToArray();

            foreach (var e in array)
            {
                e.Remove();
            }

            return array.Length > 0;
        }

        private static string AdjustForInherentDifferences(string xml)
        {
            var element = XElement.Parse(xml);

            foreach (var e in element.DescendantsAndSelf())
            {
                if (e.Name == "constant")
                {
                    // only compare constant names; values and signatures might differ:
                    var name = e.Attribute("name");
                    e.RemoveAttributes();
                    e.Add(name);
                }
                else if (e.Name == "bucket" && e.Parent?.Name == "dynamicLocals")
                {
                    // dynamic flags might be 0-padded differently
                    var flags = e.Attribute("flags");
                    flags!.SetValue(flags.Value.TrimEnd('0'));
                }
            }

            return element.ToString();
        }

        private static void VerifyPdb(Stream pdbStream, Stream peStream, string expectedXml, string message)
        {
            pdbStream.Position = 0;
            peStream.Position = 0;
            var actualXml = PdbToXmlConverter.ToXml(pdbStream, peStream, Options);

            AssertEx.AssertLinesEqual(expectedXml, actualXml, message);
        }
    }
}
