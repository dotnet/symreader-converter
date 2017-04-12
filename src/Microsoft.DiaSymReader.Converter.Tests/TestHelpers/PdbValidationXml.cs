// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    internal static class PdbValidationXml
    {
        public static void VerifyWindowsPdb(TestResource portable, TestResource windows, string expectedXml, PdbDiagnostic[] expectedDiagnostics = null)
        {
            VerifyWindowsMatchesExpected(windows, expectedXml);
            VerifyWindowsConvertedFromPortableMatchesExpected(portable, expectedXml, expectedDiagnostics);
        }

        private static void VerifyWindowsMatchesExpected(TestResource windows, string expectedXml)
        {
            var windowsPEStream = new MemoryStream(windows.PE);
            var windowsPdbStream = new MemoryStream(windows.Pdb);
            var actualXml = PdbToXmlConverter.ToXml(windowsPdbStream, windowsPEStream, PdbToXmlOptions.IncludeSourceServerInformation | PdbToXmlOptions.ResolveTokens);

            var adjustedExpectedXml = AdjustForInherentDifferences(expectedXml);
            var adjustedActualXml = AdjustForInherentDifferences(actualXml);

            AssertEx.AssertLinesEqual(adjustedExpectedXml, adjustedActualXml, "Comparing Windows PDB with expected XML");
        }

        private static void VerifyWindowsConvertedFromPortableMatchesExpected(TestResource portable, string expectedXml, PdbDiagnostic[] expectedDiagnostics)
        {
            var portablePEStream = new MemoryStream(portable.PE);
            var portablePdbStream = new MemoryStream(portable.Pdb);
            var convertedWindowsPdbStream = new MemoryStream();
            var actualDiagnostics = new List<PdbDiagnostic>();

            var converter = new PdbConverter(actualDiagnostics.Add);
            converter.ConvertPortableToWindows(portablePEStream, portablePdbStream, convertedWindowsPdbStream);

            AssertEx.Equal(expectedDiagnostics ?? Array.Empty<PdbDiagnostic>(), actualDiagnostics, itemInspector: InspectDiagnostic);

            VerifyPdb(convertedWindowsPdbStream, portablePEStream, expectedXml, "Comparing Windows PDB converted from Portable PDB with expected XML");

            portablePdbStream.Position = 0;
            convertedWindowsPdbStream.Position = 0;
            VerifyMatchingSignatures(portablePdbStream, convertedWindowsPdbStream);
        }

        private static string InspectDiagnostic(PdbDiagnostic diagnostic)
        {
            string args = diagnostic.Args != null ? $", new[] {{ { string.Join(", ", diagnostic.Args.Select(a => "\"" + a + "\""))} }}" : null;
            string token = diagnostic.Token != 0 ? $"0x{diagnostic.Token:X8}" : "0";
            return $"new PdbDiagnostic(PdbDiagnosticId.{diagnostic.Id}, {token}{args})";
        }

        private static void VerifyMatchingSignatures(Stream portablePdbStream, Stream windowsPdbStream)
        {
            Guid guid;
            uint stamp;
            int age;
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(portablePdbStream))
            {
                SymReaderHelpers.GetWindowsPdbSignature(provider.GetMetadataReader().DebugMetadataHeader.Id, out guid, out stamp, out age);
            }

            var symReader = SymReaderFactory.CreateWindowsPdbReader(windowsPdbStream);
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
                else if (e.Name == "bucket" && e.Parent.Name == "dynamicLocals")
                {
                    // dynamic flags might be 0-padded differently
                    var flags = e.Attribute("flags");
                    flags.SetValue(flags.Value.TrimEnd('0'));
                }
            }

            RemoveUnsupportedElements(element);

            return element.ToString();
        }

        private static void RemoveUnsupportedElements(XElement xml)
        {
            var pendingRemoval = new List<XElement>();

            foreach (var e in xml.DescendantsAndSelf())
            {
                if (e.Name == "defunct")
                {
                    pendingRemoval.Add(e);
                }
                else if (e.Name == "local" && e.Attributes().Any(a => a.Name.LocalName == "name" && a.Value.StartsWith("$VB$ResumableLocal_")))
                {
                    // TODO: Remove once https://github.com/dotnet/roslyn/issues/8473 is implemented
                    pendingRemoval.Add(e);
                }
            }

            foreach (var e in pendingRemoval)
            {
                e.Remove();
            }

            // TODO: Remove once https://github.com/dotnet/roslyn/issues/8473 is implemented
            RemoveEmptyScopes(xml);
        }

        private static void RemoveEmptyScopes(XElement pdb)
        {
            XElement[] emptyScopes;

            do
            {
                emptyScopes = (from e in pdb.DescendantsAndSelf()
                               where e.Name == "scope" && !e.HasElements
                               select e).ToArray();

                foreach (var e in emptyScopes)
                {
                    e.Remove();
                }
            }
            while (emptyScopes.Any());
        }

        private static void VerifyPdb(Stream pdbStream, Stream peStream, string expectedXml, string message)
        {
            pdbStream.Position = 0;
            peStream.Position = 0;
            var actualXml = PdbToXmlConverter.ToXml(pdbStream, peStream, PdbToXmlOptions.IncludeSourceServerInformation | PdbToXmlOptions.ResolveTokens);

            AssertEx.AssertLinesEqual(expectedXml, actualXml, message);
        }
    }
}
