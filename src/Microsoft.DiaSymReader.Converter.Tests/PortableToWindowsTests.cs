// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml.Linq;
using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public class PortableToWindowsTests
    {
        private static void VerifyWindowsPdb(TestResource resource, string expectedXml)
        {
            var peStream = new MemoryStream(resource.PE);
            var portablePdbStream = new MemoryStream(resource.Pdb);
            var windowsPdbStream = new MemoryStream();

            PdbConverter.ConvertPortableToWindows(peStream, portablePdbStream, windowsPdbStream);
            VerifyPdb(windowsPdbStream, peStream, expectedXml);
        }

        private static void VerifyPdb(Stream pdbStream, Stream peStream, string expectedXml)
        {
            pdbStream.Position = 0;
            peStream.Position = 0;
            var actualXml = PdbToXmlConverter.ToXml(pdbStream, peStream);

            AssertEx.AssertLinesEqual(expectedXml, actualXml);
        }

        [Fact]
        public void Convert_Documents()
        {
            VerifyWindowsPdb(TestResources.Documents.DllAndPdb(portable: true),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Documents.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""DB, EB, 2A,  6, 7B, 2F,  E,  D, 67, 8A,  0, 2C, 58, 7A, 28,  6,  5, 6C, 3D, CE, "" />
    <file id=""2"" name=""C:\a\b\c\d\1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""3"" name=""C:\a\b\c\D\2.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""4"" name=""C:\a\b\C\d\3.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""5"" name=""C:\a\b\c\d\x.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""6"" name=""C:\A\b\c\x.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""7"" name=""C:\a\b\x.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""8"" name=""C:\a\B\3.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""9"" name=""C:\a\B\c\4.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""10"" name=""C:\*\5.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""11"" name="":6.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""12"" name=""C:\a\b\X.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""13"" name=""C:\a\B\x.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""5"" endLine=""7"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""30"" document=""2"" />
        <entry offset=""0x8"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""30"" document=""3"" />
        <entry offset=""0xf"" startLine=""30"" startColumn=""9"" endLine=""30"" endColumn=""30"" document=""4"" />
        <entry offset=""0x16"" startLine=""40"" startColumn=""9"" endLine=""40"" endColumn=""30"" document=""4"" />
        <entry offset=""0x1d"" hidden=""true"" document=""4"" />
        <entry offset=""0x23"" startLine=""50"" startColumn=""9"" endLine=""50"" endColumn=""30"" document=""5"" />
        <entry offset=""0x2a"" startLine=""60"" startColumn=""9"" endLine=""60"" endColumn=""30"" document=""6"" />
        <entry offset=""0x31"" startLine=""70"" startColumn=""9"" endLine=""70"" endColumn=""30"" document=""7"" />
        <entry offset=""0x38"" startLine=""80"" startColumn=""9"" endLine=""80"" endColumn=""30"" document=""8"" />
        <entry offset=""0x3f"" startLine=""90"" startColumn=""9"" endLine=""90"" endColumn=""30"" document=""9"" />
        <entry offset=""0x46"" startLine=""100"" startColumn=""9"" endLine=""100"" endColumn=""30"" document=""10"" />
        <entry offset=""0x4d"" startLine=""110"" startColumn=""9"" endLine=""110"" endColumn=""30"" document=""11"" />
        <entry offset=""0x54"" startLine=""120"" startColumn=""9"" endLine=""120"" endColumn=""30"" document=""12"" />
        <entry offset=""0x5b"" startLine=""130"" startColumn=""9"" endLine=""130"" endColumn=""30"" document=""13"" />
        <entry offset=""0x62"" startLine=""131"" startColumn=""5"" endLine=""131"" endColumn=""6"" document=""13"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x63"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact(Skip = "TODO: ExecutionEngineException")]
        public void Convert_Scopes()
        {
            VerifyWindowsPdb(TestResources.Scopes.DllAndPdb(portable: true), @"
");
        }
    }
}
