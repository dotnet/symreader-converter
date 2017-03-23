﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    internal static class PdbValidationXml
    {
        public static void VerifyWindowsPdb(TestResource portable, TestResource windows, string expectedXml)
        {
            VerifyWindowsMatchesExpected(windows, expectedXml);
            VerifyWindowsConvertedFromPortableMatchesExpected(portable, expectedXml);
        }

        private static void VerifyWindowsMatchesExpected(TestResource windows, string expectedXml)
        {
            var windowsPEStream = new MemoryStream(windows.PE);
            var windowsPdbStream = new MemoryStream(windows.Pdb);
            var actualXml = PdbToXmlConverter.ToXml(windowsPdbStream, windowsPEStream);

            var adjustedExpectedXml = AdjustForInherentDifferences(expectedXml);
            var adjustedActualXml = AdjustForInherentDifferences(actualXml);

            AssertEx.AssertLinesEqual(adjustedExpectedXml, adjustedActualXml, "Comparing Windows PDB with expected XML");
        }

        private static void VerifyWindowsConvertedFromPortableMatchesExpected(TestResource portable, string expectedXml)
        {
            var portablePEStream = new MemoryStream(portable.PE);
            var portablePdbStream = new MemoryStream(portable.Pdb);
            var convertedWindowsPdbStream = new MemoryStream();

            PdbConverter.ConvertPortableToWindows(portablePEStream, portablePdbStream, convertedWindowsPdbStream);
            VerifyPdb(convertedWindowsPdbStream, portablePEStream, expectedXml, "Comparing Windows PDB converted from Portable PDB with expected XML");
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
            var actualXml = PdbToXmlConverter.ToXml(pdbStream, peStream);

            AssertEx.AssertLinesEqual(expectedXml, actualXml, message);
        }
    }
}
