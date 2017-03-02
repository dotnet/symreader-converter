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

        [Fact]
        public void Convert_Scopes()
        {
            VerifyWindowsPdb(TestResources.Scopes.DllAndPdb(portable: true), @"
<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <methods>
    <method containingType=""C`1"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
      </scope>
    </method>
    <method containingType=""C`1"" name=""NestedScopes"">
      <customDebugInfo>
        <forward declaringType=""C`1"" methodName=""F"" />
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x13"">
        <local name=""x0"" il_index=""0"" il_start=""0x0"" il_end=""0x13"" attributes=""0"" />
        <local name=""y0"" il_index=""1"" il_start=""0x0"" il_end=""0x13"" attributes=""0"" />
        <scope startOffset=""0x3"" endOffset=""0x7"">
          <local name=""x1"" il_index=""2"" il_start=""0x3"" il_end=""0x7"" attributes=""0"" />
        </scope>
        <scope startOffset=""0x9"" endOffset=""0x12"">
          <local name=""y1"" il_index=""3"" il_start=""0x9"" il_end=""0x12"" attributes=""0"" />
          <scope startOffset=""0xc"" endOffset=""0x11"">
            <local name=""y2"" il_index=""4"" il_start=""0xc"" il_end=""0x11"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""C`1"" name=""NestedScopesLocals"">
      <customDebugInfo>
        <forward declaringType=""C`1"" methodName=""F"" />
      </customDebugInfo>
      <scope startOffset=""0x0"" endOffset=""0x36"">
        <local name=""a"" il_index=""0"" il_start=""0x0"" il_end=""0x36"" attributes=""0"" />
        <scope startOffset=""0x3"" endOffset=""0xf"">
          <local name=""b"" il_index=""1"" il_start=""0x3"" il_end=""0xf"" attributes=""0"" />
          <scope startOffset=""0x6"" endOffset=""0xa"">
            <local name=""c"" il_index=""2"" il_start=""0x6"" il_end=""0xa"" attributes=""0"" />
          </scope>
          <scope startOffset=""0xa"" endOffset=""0xe"">
            <local name=""d"" il_index=""3"" il_start=""0xa"" il_end=""0xe"" attributes=""0"" />
          </scope>
        </scope>
        <scope startOffset=""0xf"" endOffset=""0x35"">
          <local name=""e"" il_index=""4"" il_start=""0xf"" il_end=""0x35"" attributes=""0"" />
          <scope startOffset=""0x14"" endOffset=""0x19"">
            <local name=""f"" il_index=""5"" il_start=""0x14"" il_end=""0x19"" attributes=""0"" />
          </scope>
          <scope startOffset=""0x19"" endOffset=""0x26"">
            <local name=""g"" il_index=""6"" il_start=""0x19"" il_end=""0x26"" attributes=""0"" />
            <scope startOffset=""0x1d"" endOffset=""0x25"">
              <local name=""h"" il_index=""7"" il_start=""0x1d"" il_end=""0x25"" attributes=""0"" />
              <local name=""d"" il_index=""8"" il_start=""0x1d"" il_end=""0x25"" attributes=""0"" />
            </scope>
          </scope>
          <scope startOffset=""0x26"" endOffset=""0x2b"">
            <local name=""i"" il_index=""9"" il_start=""0x26"" il_end=""0x2b"" attributes=""0"" />
          </scope>
          <scope startOffset=""0x2c"" endOffset=""0x34"">
            <local name=""j"" il_index=""10"" il_start=""0x2c"" il_end=""0x34"" attributes=""0"" />
            <local name=""d"" il_index=""11"" il_start=""0x2c"" il_end=""0x34"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }
    }
}
