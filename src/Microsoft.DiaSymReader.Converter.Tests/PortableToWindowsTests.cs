// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Xml.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public class PortableToWindowsTests
    {
        private static void VerifyWindowsPdb(TestResource portable, TestResource? windows, string expectedXml)
        {
            var portablePEStream = new MemoryStream(portable.PE);
            var portablePdbStream = new MemoryStream(portable.Pdb);
            var convertedWindowsPdbStream = new MemoryStream();

            PdbConverter.ConvertPortableToWindows(portablePEStream, portablePdbStream, convertedWindowsPdbStream);
            VerifyPdb(convertedWindowsPdbStream, portablePEStream, expectedXml);

            var windowsPEStream = new MemoryStream(windows.Value.PE);
            var windowsPdbStream = new MemoryStream(windows.Value.Pdb);
            var actualXml = PdbToXmlConverter.ToXml(windowsPdbStream, windowsPEStream);

            var adjustedExpectedXml = AdjustForInherentDifferences(expectedXml);
            var adjustedActualXml = AdjustForInherentDifferences(actualXml);

            AssertEx.AssertLinesEqual(adjustedExpectedXml, adjustedActualXml);
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
                    string originalFlags = flags.Value;
                    string trimmedFlags = flags.Value.TrimEnd('0');
                    flags.SetValue(trimmedFlags);
                    int trimmedLength = originalFlags.Length - trimmedFlags.Length;

                    var flagCount = e.Attribute("flagCount");
                    flagCount.SetValue(int.Parse(flagCount.Value) - trimmedLength);
                }
            }

            return element.ToString();
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
            VerifyWindowsPdb(
                TestResources.Documents.DllAndPdb(portable: true),
                TestResources.Documents.DllAndPdb(portable: false),
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
            VerifyWindowsPdb(
                TestResources.Scopes.DllAndPdb(portable: true),
                TestResources.Scopes.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Scopes.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""DB, EB, 2A,  6, 7B, 2F,  E,  D, 67, 8A,  0, 2C, 58, 7A, 28,  6,  5, 6C, 3D, CE, "" />
  </files>
  <methods>
    <method containingType=""C`1"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flagCount=""8"" flags=""10000000"" slotId=""0"" localName=""NullDynamic"" />
          <bucket flagCount=""8"" flags=""00000100"" slotId=""0"" localName=""NullTypeSpec"" />
        </dynamicLocals>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""21"" startColumn=""5"" endLine=""21"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""54"" startColumn=""5"" endLine=""54"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <constant name=""B"" value=""0"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""C"" value=""0"" runtime-type=""UInt16"" unknown-signature="""" />
        <constant name=""I1"" value=""1"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""U1"" value=""2"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""I2"" value=""3"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""U2"" value=""4"" runtime-type=""UInt16"" unknown-signature="""" />
        <constant name=""I4"" value=""5"" runtime-type=""Int32"" unknown-signature="""" />
        <constant name=""U4"" value=""6"" runtime-type=""UInt32"" unknown-signature="""" />
        <constant name=""I8"" value=""7"" runtime-type=""Int64"" unknown-signature="""" />
        <constant name=""U8"" value=""8"" runtime-type=""UInt64"" unknown-signature="""" />
        <constant name=""R4"" value=""9.1"" runtime-type=""Single"" unknown-signature="""" />
        <constant name=""R8"" value=""10.2"" runtime-type=""Double"" unknown-signature="""" />
        <constant name=""EI1"" value=""1"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""EU1"" value=""2"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""EI2"" value=""3"" runtime-type=""Int16"" unknown-signature="""" />
        <constant name=""EU2"" value=""4"" runtime-type=""UInt16"" unknown-signature="""" />
        <constant name=""EI4"" value=""5"" runtime-type=""Int32"" unknown-signature="""" />
        <constant name=""EU4"" value=""6"" runtime-type=""UInt32"" unknown-signature="""" />
        <constant name=""EI8"" value=""7"" runtime-type=""Int64"" unknown-signature="""" />
        <constant name=""EU8"" value=""8"" runtime-type=""UInt64"" unknown-signature="""" />
        <constant name=""StrWithNul"" value=""\u0000"" runtime-type=""String"" unknown-signature="""" />
        <constant name=""EmptyStr"" value=""null"" unknown-signature="""" />
        <constant name=""NullStr"" value=""null"" unknown-signature="""" />
        <constant name=""NullObject"" value=""null"" unknown-signature="""" />
        <constant name=""NullDynamic"" value=""null"" unknown-signature="""" />
        <constant name=""NullTypeDef"" value=""null"" unknown-signature="""" />
        <constant name=""NullTypeRef"" value=""null"" unknown-signature="""" />
        <constant name=""NullTypeSpec"" value=""null"" unknown-signature="""" />
        <constant name=""D"" value=""123456.78"" type=""Decimal"" />
      </scope>
    </method>
    <method containingType=""C`1"" name=""NestedScopes"">
      <customDebugInfo>
        <forward declaringType=""C`1"" methodName=""F"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""117"" />
          <slot kind=""0"" offset=""83"" />
          <slot kind=""0"" offset=""153"" />
          <slot kind=""0"" offset=""291"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""57"" startColumn=""5"" endLine=""57"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""58"" startColumn=""9"" endLine=""58"" endColumn=""20"" document=""1"" />
        <entry offset=""0x3"" startLine=""59"" startColumn=""9"" endLine=""59"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""61"" startColumn=""13"" endLine=""61"" endColumn=""24"" document=""1"" />
        <entry offset=""0x6"" startLine=""62"" startColumn=""9"" endLine=""62"" endColumn=""10"" document=""1"" />
        <entry offset=""0x7"" startLine=""64"" startColumn=""9"" endLine=""64"" endColumn=""20"" document=""1"" />
        <entry offset=""0x9"" startLine=""65"" startColumn=""9"" endLine=""65"" endColumn=""10"" document=""1"" />
        <entry offset=""0xa"" startLine=""66"" startColumn=""13"" endLine=""66"" endColumn=""24"" document=""1"" />
        <entry offset=""0xc"" startLine=""67"" startColumn=""13"" endLine=""67"" endColumn=""14"" document=""1"" />
        <entry offset=""0xd"" startLine=""70"" startColumn=""17"" endLine=""70"" endColumn=""28"" document=""1"" />
        <entry offset=""0x10"" startLine=""71"" startColumn=""13"" endLine=""71"" endColumn=""14"" document=""1"" />
        <entry offset=""0x11"" startLine=""72"" startColumn=""9"" endLine=""72"" endColumn=""10"" document=""1"" />
        <entry offset=""0x12"" startLine=""73"" startColumn=""5"" endLine=""73"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x13"">
        <local name=""x0"" il_index=""0"" il_start=""0x0"" il_end=""0x13"" attributes=""0"" />
        <local name=""y0"" il_index=""1"" il_start=""0x0"" il_end=""0x13"" attributes=""0"" />
        <scope startOffset=""0x3"" endOffset=""0x7"">
          <local name=""x1"" il_index=""2"" il_start=""0x3"" il_end=""0x7"" attributes=""0"" />
          <constant name=""c1"" value=""11"" runtime-type=""Int32"" unknown-signature="""" />
        </scope>
        <scope startOffset=""0x9"" endOffset=""0x12"">
          <local name=""y1"" il_index=""3"" il_start=""0x9"" il_end=""0x12"" attributes=""0"" />
          <scope startOffset=""0xc"" endOffset=""0x11"">
            <local name=""y2"" il_index=""4"" il_start=""0xc"" il_end=""0x11"" attributes=""0"" />
            <constant name=""c2"" value=""c2"" runtime-type=""String"" unknown-signature="""" />
            <constant name=""d2"" value=""d2"" runtime-type=""String"" unknown-signature="""" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""C`1"" name=""NestedScopesLocals"">
      <customDebugInfo>
        <forward declaringType=""C`1"" methodName=""F"" /> 
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""0"" offset=""50"" />
          <slot kind=""0"" offset=""93"" />
          <slot kind=""0"" offset=""151"" />
          <slot kind=""0"" offset=""212"" />
          <slot kind=""0"" offset=""278"" />
          <slot kind=""0"" offset=""348"" />
          <slot kind=""0"" offset=""407"" />
          <slot kind=""0"" offset=""443"" />
          <slot kind=""0"" offset=""536"" />
          <slot kind=""0"" offset=""613"" />
          <slot kind=""0"" offset=""641"" />
         </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""76"" startColumn=""5"" endLine=""76"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""77"" startColumn=""9"" endLine=""77"" endColumn=""19"" document=""1"" />
        <entry offset=""0x3"" startLine=""78"" startColumn=""9"" endLine=""78"" endColumn=""10"" document=""1"" />
        <entry offset=""0x4"" startLine=""79"" startColumn=""13"" endLine=""79"" endColumn=""23"" document=""1"" />
        <entry offset=""0x6"" startLine=""80"" startColumn=""13"" endLine=""80"" endColumn=""14"" document=""1"" />
        <entry offset=""0x7"" startLine=""81"" startColumn=""17"" endLine=""81"" endColumn=""27"" document=""1"" />
        <entry offset=""0x9"" startLine=""82"" startColumn=""13"" endLine=""82"" endColumn=""14"" document=""1"" />
        <entry offset=""0xa"" startLine=""83"" startColumn=""13"" endLine=""83"" endColumn=""14"" document=""1"" />
        <entry offset=""0xb"" startLine=""84"" startColumn=""17"" endLine=""84"" endColumn=""27"" document=""1"" />
        <entry offset=""0xd"" startLine=""85"" startColumn=""13"" endLine=""85"" endColumn=""14"" document=""1"" />
        <entry offset=""0xe"" startLine=""86"" startColumn=""9"" endLine=""86"" endColumn=""10"" document=""1"" />
        <entry offset=""0xf"" startLine=""87"" startColumn=""9"" endLine=""87"" endColumn=""10"" document=""1"" />
        <entry offset=""0x10"" startLine=""88"" startColumn=""13"" endLine=""88"" endColumn=""23"" document=""1"" />
        <entry offset=""0x13"" startLine=""89"" startColumn=""13"" endLine=""89"" endColumn=""14"" document=""1"" />
        <entry offset=""0x14"" startLine=""90"" startColumn=""17"" endLine=""90"" endColumn=""18"" document=""1"" />
        <entry offset=""0x15"" startLine=""91"" startColumn=""21"" endLine=""91"" endColumn=""31"" document=""1"" />
        <entry offset=""0x18"" startLine=""92"" startColumn=""17"" endLine=""92"" endColumn=""18"" document=""1"" />
        <entry offset=""0x19"" startLine=""93"" startColumn=""17"" endLine=""93"" endColumn=""18"" document=""1"" />
        <entry offset=""0x1a"" startLine=""94"" startColumn=""21"" endLine=""94"" endColumn=""31"" document=""1"" />
        <entry offset=""0x1d"" startLine=""95"" startColumn=""21"" endLine=""95"" endColumn=""22"" document=""1"" />
        <entry offset=""0x1e"" startLine=""96"" startColumn=""25"" endLine=""96"" endColumn=""35"" document=""1"" />
        <entry offset=""0x21"" startLine=""97"" startColumn=""25"" endLine=""97"" endColumn=""35"" document=""1"" />
        <entry offset=""0x24"" startLine=""98"" startColumn=""21"" endLine=""98"" endColumn=""22"" document=""1"" />
        <entry offset=""0x25"" startLine=""99"" startColumn=""17"" endLine=""99"" endColumn=""18"" document=""1"" />
        <entry offset=""0x26"" startLine=""100"" startColumn=""17"" endLine=""100"" endColumn=""18"" document=""1"" />
        <entry offset=""0x27"" startLine=""101"" startColumn=""21"" endLine=""101"" endColumn=""31"" document=""1"" />
        <entry offset=""0x2a"" startLine=""102"" startColumn=""17"" endLine=""102"" endColumn=""18"" document=""1"" />
        <entry offset=""0x2b"" startLine=""103"" startColumn=""13"" endLine=""103"" endColumn=""14"" document=""1"" />
        <entry offset=""0x2c"" startLine=""104"" startColumn=""13"" endLine=""104"" endColumn=""14"" document=""1"" />
        <entry offset=""0x2d"" startLine=""105"" startColumn=""17"" endLine=""105"" endColumn=""27"" document=""1"" />
        <entry offset=""0x30"" startLine=""106"" startColumn=""17"" endLine=""106"" endColumn=""27"" document=""1"" />
        <entry offset=""0x33"" startLine=""107"" startColumn=""13"" endLine=""107"" endColumn=""14"" document=""1"" />
        <entry offset=""0x34"" startLine=""108"" startColumn=""9"" endLine=""108"" endColumn=""10"" document=""1"" />
        <entry offset=""0x35"" startLine=""109"" startColumn=""5"" endLine=""109"" endColumn=""6"" document=""1"" />
      </sequencePoints>
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

        [Fact]
        public void Convert_Async()
        {
            VerifyWindowsPdb(
                TestResources.Async.DllAndPdb(portable: true),
                TestResources.Async.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Async.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""DB, EB, 2A,  6, 7B, 2F,  E,  D, 67, 8A,  0, 2C, 58, 7A, 28,  6,  5, 6C, 3D, CE, "" />
  </files>
  <methods>
    <method containingType=""C"" name=""M1"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M1&gt;d__0"" />
      </customDebugInfo>
    </method>
    <method containingType=""C"" name=""M2"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M2&gt;d__1"" />
      </customDebugInfo>
    </method>
    <method containingType=""C+&lt;M1&gt;d__0"" name=""MoveNext"">
       <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""20"" offset=""0"" />
          <slot kind=""33"" offset=""5"" />
          <slot kind=""temp"" />
          <slot kind=""33"" offset=""35"" />
          <slot kind=""33"" offset=""65"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0x27"" startLine=""8"" startColumn=""2"" endLine=""8"" endColumn=""3"" document=""1"" />
        <entry offset=""0x28"" startLine=""9"" startColumn=""3"" endLine=""9"" endColumn=""28"" document=""1"" />
        <entry offset=""0x34"" hidden=""true"" document=""1"" />
        <entry offset=""0x90"" startLine=""10"" startColumn=""3"" endLine=""10"" endColumn=""28"" document=""1"" />
        <entry offset=""0x9d"" hidden=""true"" document=""1"" />
        <entry offset=""0xfb"" startLine=""11"" startColumn=""3"" endLine=""11"" endColumn=""28"" document=""1"" />
        <entry offset=""0x108"" hidden=""true"" document=""1"" />
        <entry offset=""0x163"" startLine=""13"" startColumn=""3"" endLine=""13"" endColumn=""12"" document=""1"" />
        <entry offset=""0x167"" hidden=""true"" document=""1"" />
        <entry offset=""0x181"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""6"" document=""1"" />
        <entry offset=""0x189"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x197"">
        <namespace name=""System.Threading.Tasks"" />
      </scope>
      <asyncInfo>
        <kickoffMethod declaringType=""C"" methodName=""M1"" />
        <await yield=""0x46"" resume=""0x64"" declaringType=""C+&lt;M1&gt;d__0"" methodName=""MoveNext"" />
        <await yield=""0xaf"" resume=""0xce"" declaringType=""C+&lt;M1&gt;d__0"" methodName=""MoveNext"" />
        <await yield=""0x11a"" resume=""0x136"" declaringType=""C+&lt;M1&gt;d__0"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
    <method containingType=""C+&lt;M2&gt;d__1"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C+&lt;M1&gt;d__0"" methodName=""MoveNext"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""33"" offset=""5"" />
          <slot kind=""temp"" />
          <slot kind=""temp"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x7"" hidden=""true"" document=""1"" />
        <entry offset=""0xe"" startLine=""17"" startColumn=""2"" endLine=""17"" endColumn=""3"" document=""1"" />
        <entry offset=""0xf"" startLine=""18"" startColumn=""3"" endLine=""18"" endColumn=""28"" document=""1"" />
        <entry offset=""0x1b"" hidden=""true"" document=""1"" />
        <entry offset=""0x76"" hidden=""true"" document=""1"" />
        <entry offset=""0x8e"" startLine=""19"" startColumn=""5"" endLine=""19"" endColumn=""6"" document=""1"" />
        <entry offset=""0x96"" hidden=""true"" document=""1"" />
      </sequencePoints>
      <asyncInfo>
        <catchHandler offset=""0x76"" />
        <kickoffMethod declaringType=""C"" methodName=""M2"" />
        <await yield=""0x2d"" resume=""0x48"" declaringType=""C+&lt;M2&gt;d__1"" methodName=""MoveNext"" />
      </asyncInfo>
    </method>
  </methods>
</symbols>
");
        }
    }
}
