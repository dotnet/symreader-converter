// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System.Collections.Generic;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    using static PdbValidationXml;

    public class PortableToWindowsTests
    {
        [Fact]
        public void Convert_Documents()
        {
            VerifyWindowsPdb(
                TestResources.Documents.DllAndPdb(portable: true),
                TestResources.Documents.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""/_/Documents.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""46-E0-DA-8D-C6-03-94-AE-09-FA-AD-1C-D8-6F-60-19-64-BE-4E-5A-B0-D8-D9-60-0D-79-E8-92-50-16-04-06"" />
    <file id=""2"" name=""C:\a\b\c\d\1.cs"" language=""C#"" />
    <file id=""3"" name=""C:\a\b\c\D\2.cs"" language=""C#"" />
    <file id=""4"" name=""C:\a\b\C\d\3.cs"" language=""C#"" />
    <file id=""5"" name=""C:\a\b\c\d\x.cs"" language=""C#"" />
    <file id=""6"" name=""C:\A\b\c\x.cs"" language=""C#"" />
    <file id=""7"" name=""C:\a\b\x.cs"" language=""C#"" />
    <file id=""8"" name=""C:\a\B\3.cs"" language=""C#"" />
    <file id=""9"" name=""C:\a\B\c\4.cs"" language=""C#"" />
    <file id=""10"" name=""C:\*\5.cs"" language=""C#"" />
    <file id=""11"" name="":6.cs"" language=""C#"" />
    <file id=""12"" name=""C:\a\b\X.cs"" language=""C#"" />
    <file id=""13"" name=""C:\a\B\x.cs"" language=""C#"" />
  </files>
  <methods>
    <method containingType=""C"" name=""M"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""5"" endLine=""6"" endColumn=""6"" document=""1"" />
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
    <file id=""1"" name=""C:\Scopes.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
  </files>
  <methods>
    <method containingType=""C`1"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
        </using>
        <dynamicLocals>
          <bucket flags=""10000000"" slotId=""0"" localName=""NullDynamic"" />
          <bucket flags=""00000100"" slotId=""0"" localName=""NullTypeSpec"" />
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
        <constant name=""R4"" value=""0x4111999A"" runtime-type=""Single"" unknown-signature="""" />
        <constant name=""R8"" value=""0x4024666666666666"" runtime-type=""Double"" unknown-signature="""" />
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
    <file id=""1"" name=""C:\Async.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
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

        [Fact]
        public void Convert_Iterator()
        {
            VerifyWindowsPdb(
               TestResources.Iterator.DllAndPdb(portable: true),
               TestResources.Iterator.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Iterator.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
  </files>
  <methods>
    <method containingType=""C`1+D`1"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__0"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""19"" />
          <slot kind=""0"" offset=""78"" />
          <slot kind=""0"" offset=""168"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""C`1+D`1"" name=""M"">
      <customDebugInfo>
        <forwardIterator name=""&lt;M&gt;d__1"" />
      </customDebugInfo>
    </method>
    <method containingType=""C`1+D`1+&lt;M&gt;d__0`1"" name=""MoveNext"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <hoistedLocalScopes>
          <slot startOffset=""0x1f"" endOffset=""0x85"" />
          <slot startOffset=""0x27"" endOffset=""0x83"" />
          <slot startOffset=""0x30"" endOffset=""0x65"" />
        </hoistedLocalScopes>
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
          <slot kind=""temp"" />
          <slot kind=""1"" offset=""69"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""1"" />
        <entry offset=""0x20"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""23"" document=""1"" />
        <entry offset=""0x27"" startLine=""13"" startColumn=""18"" endLine=""13"" endColumn=""27"" document=""1"" />
        <entry offset=""0x2e"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" startLine=""14"" startColumn=""13"" endLine=""14"" endColumn=""14"" document=""1"" />
        <entry offset=""0x31"" startLine=""16"" startColumn=""17"" endLine=""16"" endColumn=""27"" document=""1"" />
        <entry offset=""0x38"" startLine=""17"" startColumn=""17"" endLine=""17"" endColumn=""48"" document=""1"" />
        <entry offset=""0x5d"" hidden=""true"" document=""1"" />
        <entry offset=""0x64"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""14"" document=""1"" />
        <entry offset=""0x65"" startLine=""13"" startColumn=""37"" endLine=""13"" endColumn=""40"" document=""1"" />
        <entry offset=""0x75"" startLine=""13"" startColumn=""29"" endLine=""13"" endColumn=""35"" document=""1"" />
        <entry offset=""0x80"" hidden=""true"" document=""1"" />
        <entry offset=""0x83"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x85"">
        <namespace name=""System.Collections.Generic"" />
        <scope startOffset=""0x1f"" endOffset=""0x85"">
          <constant name=""x"" value=""1"" runtime-type=""Int32"" unknown-signature="""" />
          <scope startOffset=""0x30"" endOffset=""0x65"">
            <constant name=""y"" value=""2"" runtime-type=""Int32"" unknown-signature="""" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""C`1+D`1+&lt;M&gt;d__1`2"" name=""MoveNext"">
      <customDebugInfo>
        <forward declaringType=""C`1+D`1+&lt;M&gt;d__0`1"" methodName=""MoveNext"" />
        <encLocalSlotMap>
          <slot kind=""27"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x1f"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x20"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""28"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x37"" startLine=""24"" startColumn=""9"" endLine=""24"" endColumn=""10"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void Convert_Imports()
        {
            VerifyWindowsPdb(
               TestResources.Imports.DllAndPdb(portable: true),
               TestResources.Imports.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Imports.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
  </files>
  <methods>
    <method containingType=""C`1"" name=""G"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""39"" startColumn=""14"" endLine=""39"" endColumn=""15"" document=""1"" />
        <entry offset=""0x1"" startLine=""39"" startColumn=""16"" endLine=""39"" endColumn=""17"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <externinfo alias=""ExternAlias1"" assembly=""System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
      </scope>
    </method>
    <method containingType=""Y.X.A"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""2"" />
          <namespace usingCount=""13"" />
          <namespace usingCount=""0"" />
        </using>
        <forwardToModule declaringType=""C`1"" methodName=""G"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""28"" startColumn=""22"" endLine=""28"" endColumn=""23"" document=""1"" />
        <entry offset=""0x1"" startLine=""28"" startColumn=""24"" endLine=""28"" endColumn=""25"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <alias name=""AliasedType7"" target=""C`1+D`1[Y.X.A[][,],Y.X.A[,,][]]"" kind=""type"" />
        <alias name=""AliasedType8"" target=""System.Collections.Generic.Dictionary`2+KeyCollection[Y.X.A,Y.X.A+B], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <extern alias=""ExternAlias1"" />
        <namespace name=""System"" />
        <namespace name=""System.Linq"" />
        <type name=""System.Math, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
        <type name=""System.Linq.Queryable, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" />
        <alias name=""AliasedNamespace1"" target=""System"" kind=""namespace"" />
        <alias name=""AliasedNamespace2"" target=""System.IO"" kind=""namespace"" />
        <alias name=""AliasedType1"" target=""System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""AliasedType2"" target=""System.Linq.ParallelEnumerable, System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""AliasedType3"" target=""Y.X.A+B"" kind=""type"" />
        <alias name=""AliasedType4"" target=""System.Action`2[[System.Action`9[Y.X.A+B**[,,][],[System.Boolean, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Byte, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.SByte, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int16, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.UInt16, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Char, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.UInt32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Action`8[[System.IntPtr, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.UIntPtr, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int64, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.UInt64, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Single, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Double, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Object, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""AliasedType5"" target=""System.TypedReference, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
        <alias name=""AliasedType6"" target=""System.Action`1[[System.Int32[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" kind=""type"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void Convert_MethodBoundaries()
        {
            VerifyWindowsPdb(
                TestResources.MethodBoundaries.DllAndPdb(portable: true),
                TestResources.MethodBoundaries.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\MethodBoundaries1.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
    <file id=""2"" name=""C:\MethodBoundaries2.cs"" language=""C#"" />
    <file id=""3"" name=""C:\MethodBoundaries3.cs"" language=""C#"" />
    <file id=""4"" name=""/_/MethodBoundaries.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""62-AB-2C-7D-4E-39-5E-50-62-5B-A8-CF-EA-41-DD-5C-C0-81-A2-AB-B1-F7-14-EE-49-2C-51-F0-26-D7-DB-A9"" />
  </files>
  <methods>
    <method containingType=""C"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""7"" endColumn=""17"" document=""1"" />
        <entry offset=""0x11"" startLine=""14"" startColumn=""5"" endLine=""14"" endColumn=""17"" document=""1"" />
        <entry offset=""0x1c"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""15"" document=""1"" />
        <entry offset=""0x23"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""1"" />
        <entry offset=""0x24"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""13"" document=""1"" />
        <entry offset=""0x2a"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""13"" document=""1"" />
        <entry offset=""0x7"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""13"" document=""1"" />
        <entry offset=""0xd"" startLine=""7"" startColumn=""9"" endLine=""7"" endColumn=""13"" document=""1"" />
        <entry offset=""0x13"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""13"" document=""1"" />
        <entry offset=""0x19"" startLine=""5"" startColumn=""9"" endLine=""5"" endColumn=""13"" document=""1"" />
        <entry offset=""0x1f"" startLine=""1"" startColumn=""9"" endLine=""1"" endColumn=""13"" document=""2"" />
        <entry offset=""0x25"" startLine=""20"" startColumn=""9"" endLine=""20"" endColumn=""13"" document=""1"" />
        <entry offset=""0x2b"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""18"" document=""1"" />
        <entry offset=""0x2f"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""G"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""7"" endColumn=""11"" document=""1"" />
        <entry offset=""0x7"" startLine=""8"" startColumn=""9"" endLine=""8"" endColumn=""18"" document=""1"" />
        <entry offset=""0xb"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""E0"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""31"" endLine=""5"" endColumn=""34"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""E1"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""31"" endLine=""7"" endColumn=""34"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""H"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""6"" document=""2"" />
        <entry offset=""0x1"" startLine=""5"" startColumn=""9"" endLine=""8"" endColumn=""11"" document=""2"" />
        <entry offset=""0x7"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""18"" document=""2"" />
        <entry offset=""0xb"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""E2"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""31"" endLine=""6"" endColumn=""34"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""E3"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""9"" endColumn=""8"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""E4"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""8"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""J1"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""2"" />
        <entry offset=""0x1"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""13"" document=""2"" />
        <entry offset=""0x7"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""I"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
        <encLocalSlotMap>
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""2"" />
        <entry offset=""0x1"" startLine=""12"" startColumn=""9"" endLine=""21"" endColumn=""11"" document=""2"" />
        <entry offset=""0x7"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""18"" document=""2"" />
        <entry offset=""0xb"" startLine=""23"" startColumn=""5"" endLine=""23"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""J2"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""16"" startColumn=""5"" endLine=""16"" endColumn=""6"" document=""2"" />
        <entry offset=""0x1"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""13"" document=""2"" />
        <entry offset=""0x7"" startLine=""28"" startColumn=""5"" endLine=""28"" endColumn=""6"" document=""2"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""K1"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""1"" startColumn=""5"" endLine=""1"" endColumn=""6"" document=""3"" />
        <entry offset=""0x1"" startLine=""2"" startColumn=""9"" endLine=""11"" endColumn=""11"" document=""3"" />
        <entry offset=""0x7"" startLine=""12"" startColumn=""5"" endLine=""12"" endColumn=""6"" document=""3"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""K2"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""3"" startColumn=""5"" endLine=""3"" endColumn=""6"" document=""3"" />
        <entry offset=""0x1"" startLine=""4"" startColumn=""9"" endLine=""10"" endColumn=""11"" document=""3"" />
        <entry offset=""0x7"" startLine=""11"" startColumn=""5"" endLine=""11"" endColumn=""6"" document=""3"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""K3"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""5"" startColumn=""5"" endLine=""5"" endColumn=""6"" document=""3"" />
        <entry offset=""0x1"" startLine=""6"" startColumn=""9"" endLine=""9"" endColumn=""11"" document=""3"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""6"" document=""3"" />
      </sequencePoints>
    </method>
    <method containingType=""C"" name=""K4"">
      <customDebugInfo>
        <forward declaringType=""C"" methodName="".ctor"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""7"" startColumn=""9"" endLine=""8"" endColumn=""10"" document=""3"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void Convert_LanguageOnlyTypes()
        {
            VerifyWindowsPdb(
               TestResources.LanguageOnlyTypes.DllAndPdb(portable: true),
               TestResources.LanguageOnlyTypes.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""/_/LanguageOnlyTypes.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""98-C7-6E-B0-6E-F0-49-67-5A-F3-73-86-AF-FE-24-EC-F9-D3-DC-C1-0A-09-99-52-32-9A-25-C0-0A-B5-E8-D0"" />
    <file id=""2"" name=""/_/System.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""5C-47-20-0B-0F-35-34-EA-05-59-9A-86-5A-49-38-C9-6D-90-BD-10-A0-4B-08-58-1F-18-0A-ED-BE-B7-2B-6D"" />
  </files>
  <methods>
    <method containingType=""C"" name=""F"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
        <dynamicLocals>
          <bucket flags=""01000000"" slotId=""0"" localName=""c1"" />
          <bucket flags=""00110000"" slotId=""0"" localName=""c2"" />
          <bucket flags=""0100101110000000"" slotId=""2"" localName=""v1"" />
          <bucket flags=""0100101110000000"" slotId=""3"" localName=""v2"" />
          <bucket flags=""01010000"" slotId=""0"" localName=""c1"" />
          <bucket flags=""01010000"" slotId=""0"" localName=""c2"" />
        </dynamicLocals>
        <tupleElementNames>
          <local elementNames=""|a0|a1|a2|a3|a4|a5|n0|n1|n2|n3|n4|n5|n6|n7|n8|n9||||n0|n1|n2|n3|n4|n5|n6|n7|n8|n9||||n0|n1|n2|n3|n4|n5|n6|n7|n8|n9||||n0|n1|n2|n3|n4|n5|n6|n7|n8|n9||||n0|n1|n2|n3|n4|n5|n6|n7|n8|n9||||n0|n1|n2|n3|n4|n5|n6|n7|n8|n9|||"" slotIndex=""1"" localName=""v2"" scopeStart=""0x0"" scopeEnd=""0x0"" />
          <local elementNames=""|a1|a7|a8||||a4|"" slotIndex=""3"" localName=""v2"" scopeStart=""0x0"" scopeEnd=""0x0"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""839"" />
          <slot kind=""0"" offset=""869"" />
          <slot kind=""0"" offset=""1961"" />
          <slot kind=""0"" offset=""2055"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""8"" startColumn=""5"" endLine=""8"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""9"" startColumn=""9"" endLine=""9"" endColumn=""10"" document=""1"" />
        <entry offset=""0x2"" startLine=""10"" startColumn=""13"" endLine=""17"" endColumn=""125"" document=""1"" />
        <entry offset=""0x4"" startLine=""19"" startColumn=""13"" endLine=""25"" endColumn=""144"" document=""1"" />
        <entry offset=""0xc"" startLine=""29"" startColumn=""9"" endLine=""29"" endColumn=""10"" document=""1"" />
        <entry offset=""0xd"" startLine=""31"" startColumn=""9"" endLine=""31"" endColumn=""10"" document=""1"" />
        <entry offset=""0xe"" startLine=""32"" startColumn=""13"" endLine=""32"" endColumn=""99"" document=""1"" />
        <entry offset=""0x10"" startLine=""37"" startColumn=""9"" endLine=""37"" endColumn=""10"" document=""1"" />
        <entry offset=""0x11"" startLine=""38"" startColumn=""5"" endLine=""38"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x12"">
        <namespace name=""System"" />
        <scope startOffset=""0x1"" endOffset=""0xd"">
          <local name=""v1"" il_index=""0"" il_start=""0x1"" il_end=""0xd"" attributes=""0"" />
          <local name=""v2"" il_index=""1"" il_start=""0x1"" il_end=""0xd"" attributes=""0"" />
          <constant name=""c1"" value=""null"" unknown-signature="""" />
          <constant name=""c2"" value=""null"" unknown-signature="""" />
        </scope>
        <scope startOffset=""0xd"" endOffset=""0x11"">
          <local name=""v1"" il_index=""2"" il_start=""0xd"" il_end=""0x11"" attributes=""0"" />
          <local name=""v2"" il_index=""3"" il_start=""0xd"" il_end=""0x11"" attributes=""0"" />
          <constant name=""c1"" value=""null"" unknown-signature="""" />
          <constant name=""c2"" value=""null"" unknown-signature="""" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>
");
        }

        [Fact]
        public void Convert_VB()
        {
            VerifyWindowsPdb(
               TestResources.VB.DllAndPdb(portable: true),
               TestResources.VB.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\VB.vb"" language=""VB"" />
  </files>
  <methods>
    <method containingType=""N1.C"" name=""Foo"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_1_Foo"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""4"" />
          <slot kind=""6"" offset=""62"" />
          <slot kind=""8"" offset=""62"" />
          <slot kind=""0"" offset=""71"" />
          <slot kind=""6"" offset=""174"" />
          <slot kind=""8"" offset=""174"" />
          <slot kind=""0"" offset=""183"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""F"" name=""Tuples"">
      <customDebugInfo>
        <tupleElementNames>
          <local elementNames=""|x||z"" slotIndex=""3"" localName=""a"" scopeStart=""0x0"" scopeEnd=""0x0"" />
          <local elementNames=""|u|"" slotIndex=""8"" localName=""a"" scopeStart=""0x0"" scopeEnd=""0x0"" />
        </tupleElementNames>
        <encLocalSlotMap>
          <slot kind=""6"" offset=""0"" />
          <slot kind=""8"" offset=""0"" />
          <slot kind=""0"" offset=""9"" />
          <slot kind=""0"" offset=""52"" />
          <slot kind=""1"" offset=""0"" />
          <slot kind=""6"" offset=""129"" />
          <slot kind=""8"" offset=""129"" />
          <slot kind=""0"" offset=""138"" />
          <slot kind=""0"" offset=""181"" />
          <slot kind=""1"" offset=""129"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""42"" startColumn=""5"" endLine=""42"" endColumn=""17"" document=""1"" />
        <entry offset=""0x1"" startLine=""43"" startColumn=""9"" endLine=""43"" endColumn=""43"" document=""1"" />
        <entry offset=""0x15"" hidden=""true"" document=""1"" />
        <entry offset=""0x1b"" startLine=""44"" startColumn=""17"" endLine=""44"" endColumn=""68"" document=""1"" />
        <entry offset=""0x23"" startLine=""45"" startColumn=""9"" endLine=""45"" endColumn=""13"" document=""1"" />
        <entry offset=""0x24"" hidden=""true"" document=""1"" />
        <entry offset=""0x28"" hidden=""true"" document=""1"" />
        <entry offset=""0x30"" hidden=""true"" document=""1"" />
        <entry offset=""0x34"" startLine=""47"" startColumn=""9"" endLine=""47"" endColumn=""43"" document=""1"" />
        <entry offset=""0x4a"" hidden=""true"" document=""1"" />
        <entry offset=""0x53"" startLine=""48"" startColumn=""17"" endLine=""48"" endColumn=""54"" document=""1"" />
        <entry offset=""0x5b"" startLine=""49"" startColumn=""9"" endLine=""49"" endColumn=""13"" document=""1"" />
        <entry offset=""0x5c"" hidden=""true"" document=""1"" />
        <entry offset=""0x62"" hidden=""true"" document=""1"" />
        <entry offset=""0x6c"" hidden=""true"" document=""1"" />
        <entry offset=""0x70"" startLine=""50"" startColumn=""5"" endLine=""50"" endColumn=""12"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x71"">
        <xmlnamespace prefix=""file1"" name=""http://stuff/fromFile"" importlevel=""file"" />
        <xmlnamespace prefix="""" name=""http://stuff/fromFile1"" importlevel=""file"" />
        <alias name=""AliasE"" target=""N2.D.E"" kind=""namespace"" importlevel=""file"" />
        <namespace name=""System"" importlevel=""file"" />
        <namespace name=""System.Collections.Generic"" importlevel=""file"" />
        <xmlnamespace prefix=""prjlevel1"" name=""http://NewNamespace"" importlevel=""project"" />
        <alias name=""A1"" target=""System.Collections.Generic"" kind=""namespace"" importlevel=""project"" />
        <alias name=""A2"" target=""System.Int64"" kind=""namespace"" importlevel=""project"" />
        <namespace name=""System.Threading"" importlevel=""project"" />
        <currentnamespace name="""" />
        <scope startOffset=""0x17"" endOffset=""0x27"">
          <local name=""x"" il_index=""2"" il_start=""0x17"" il_end=""0x27"" attributes=""0"" />
          <scope startOffset=""0x1b"" endOffset=""0x22"">
            <local name=""a"" il_index=""3"" il_start=""0x1b"" il_end=""0x22"" attributes=""0"" />
          </scope>
        </scope>
        <scope startOffset=""0x4c"" endOffset=""0x61"">
          <local name=""x"" il_index=""7"" il_start=""0x4c"" il_end=""0x61"" attributes=""0"" />
          <scope startOffset=""0x53"" endOffset=""0x5a"">
            <local name=""a"" il_index=""8"" il_start=""0x53"" il_end=""0x5a"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""N3.G"" name=""It1"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_1_It1"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""4"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""N3.G"" name=""It2"">
      <customDebugInfo>
        <forwardIterator name=""VB$StateMachine_2_It2"" />
        <encLocalSlotMap>
          <slot kind=""0"" offset=""4"" />
          <slot kind=""0"" offset=""41"" />
          <slot kind=""0"" offset=""78"" />
          <slot kind=""0"" offset=""115"" />
        </encLocalSlotMap>
      </customDebugInfo>
    </method>
    <method containingType=""N1.C+VB$StateMachine_1_Foo"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""20"" offset=""-1"" />
          <slot kind=""27"" offset=""-1"" />
          <slot kind=""1"" offset=""62"" />
          <slot kind=""1"" offset=""174"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x41"" startLine=""11"" startColumn=""9"" endLine=""11"" endColumn=""59"" document=""1"" />
        <entry offset=""0x42"" startLine=""12"" startColumn=""17"" endLine=""12"" endColumn=""23"" document=""1"" />
        <entry offset=""0x4e"" startLine=""13"" startColumn=""13"" endLine=""13"" endColumn=""24"" document=""1"" />
        <entry offset=""0x58"" startLine=""15"" startColumn=""13"" endLine=""15"" endColumn=""41"" document=""1"" />
        <entry offset=""0x6b"" hidden=""true"" document=""1"" />
        <entry offset=""0x80"" startLine=""16"" startColumn=""17"" endLine=""16"" endColumn=""24"" document=""1"" />
        <entry offset=""0xa0"" startLine=""17"" startColumn=""17"" endLine=""17"" endColumn=""24"" document=""1"" />
        <entry offset=""0xc0"" startLine=""18"" startColumn=""13"" endLine=""18"" endColumn=""17"" document=""1"" />
        <entry offset=""0xc1"" hidden=""true"" document=""1"" />
        <entry offset=""0xcf"" hidden=""true"" document=""1"" />
        <entry offset=""0xe0"" hidden=""true"" document=""1"" />
        <entry offset=""0xe3"" startLine=""20"" startColumn=""13"" endLine=""20"" endColumn=""47"" document=""1"" />
        <entry offset=""0x101"" hidden=""true"" document=""1"" />
        <entry offset=""0x116"" startLine=""21"" startColumn=""17"" endLine=""21"" endColumn=""24"" document=""1"" />
        <entry offset=""0x136"" startLine=""22"" startColumn=""17"" endLine=""22"" endColumn=""24"" document=""1"" />
        <entry offset=""0x156"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""17"" document=""1"" />
        <entry offset=""0x157"" hidden=""true"" document=""1"" />
        <entry offset=""0x165"" hidden=""true"" document=""1"" />
        <entry offset=""0x176"" hidden=""true"" document=""1"" />
        <entry offset=""0x179"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""21"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x17b"">
        <xmlnamespace prefix=""file1"" name=""http://stuff/fromFile"" importlevel=""file"" />
        <xmlnamespace prefix="""" name=""http://stuff/fromFile1"" importlevel=""file"" />
        <alias name=""AliasE"" target=""N2.D.E"" kind=""namespace"" importlevel=""file"" />
        <namespace name=""System"" importlevel=""file"" />
        <namespace name=""System.Collections.Generic"" importlevel=""file"" />
        <xmlnamespace prefix=""prjlevel1"" name=""http://NewNamespace"" importlevel=""project"" />
        <alias name=""A1"" target=""System.Collections.Generic"" kind=""namespace"" importlevel=""project"" />
        <alias name=""A2"" target=""System.Int64"" kind=""namespace"" importlevel=""project"" />
        <namespace name=""System.Threading"" importlevel=""project"" />
        <currentnamespace name=""N1"" />
        <scope startOffset=""0x41"" endOffset=""0x17a"">
          <local name=""$VB$ResumableLocal_arr$0"" il_index=""0"" il_start=""0x41"" il_end=""0x17a"" attributes=""0"" />
          <scope startOffset=""0x6d"" endOffset=""0xce"">
            <local name=""$VB$ResumableLocal_x$3"" il_index=""3"" il_start=""0x6d"" il_end=""0xce"" attributes=""0"" />
          </scope>
          <scope startOffset=""0x103"" endOffset=""0x164"">
            <local name=""$VB$ResumableLocal_x$6"" il_index=""6"" il_start=""0x103"" il_end=""0x164"" attributes=""0"" />
          </scope>
        </scope>
      </scope>
    </method>
    <method containingType=""N2.D+E"" name=""M"">
      <sequencePoints>
        <entry offset=""0x0"" startLine=""32"" startColumn=""13"" endLine=""32"" endColumn=""20"" document=""1"" />
        <entry offset=""0x1"" startLine=""36"" startColumn=""13"" endLine=""36"" endColumn=""20"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <xmlnamespace prefix=""file1"" name=""http://stuff/fromFile"" importlevel=""file"" />
        <xmlnamespace prefix="""" name=""http://stuff/fromFile1"" importlevel=""file"" />
        <alias name=""AliasE"" target=""N2.D.E"" kind=""namespace"" importlevel=""file"" />
        <namespace name=""System"" importlevel=""file"" />
        <namespace name=""System.Collections.Generic"" importlevel=""file"" />
        <xmlnamespace prefix=""prjlevel1"" name=""http://NewNamespace"" importlevel=""project"" />
        <alias name=""A1"" target=""System.Collections.Generic"" kind=""namespace"" importlevel=""project"" />
        <alias name=""A2"" target=""System.Int64"" kind=""namespace"" importlevel=""project"" />
        <namespace name=""System.Threading"" importlevel=""project"" />
        <currentnamespace name=""N2"" />
        <constant name=""D1"" value=""0"" type=""Decimal"" />
        <constant name=""D2"" value=""1.23"" type=""Decimal"" />
        <constant name=""DT"" value=""0x08D1F36D05308000"" runtime-type=""Double"" unknown-signature="""" />
      </scope>
    </method>
    <method containingType=""N3.G+VB$StateMachine_1_It1"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""20"" offset=""-1"" />
          <slot kind=""27"" offset=""-1"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""65"" startColumn=""9"" endLine=""65"" endColumn=""59"" document=""1"" />
        <entry offset=""0x22"" startLine=""66"" startColumn=""17"" endLine=""66"" endColumn=""35"" document=""1"" />
        <entry offset=""0x29"" startLine=""67"" startColumn=""13"" endLine=""67"" endColumn=""20"" document=""1"" />
        <entry offset=""0x44"" startLine=""68"" startColumn=""13"" endLine=""68"" endColumn=""35"" document=""1"" />
        <entry offset=""0x50"" startLine=""69"" startColumn=""9"" endLine=""69"" endColumn=""21"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x52"">
        <xmlnamespace prefix=""file1"" name=""http://stuff/fromFile"" importlevel=""file"" />
        <xmlnamespace prefix="""" name=""http://stuff/fromFile1"" importlevel=""file"" />
        <alias name=""AliasE"" target=""N2.D.E"" kind=""namespace"" importlevel=""file"" />
        <namespace name=""System"" importlevel=""file"" />
        <namespace name=""System.Collections.Generic"" importlevel=""file"" />
        <xmlnamespace prefix=""prjlevel1"" name=""http://NewNamespace"" importlevel=""project"" />
        <alias name=""A1"" target=""System.Collections.Generic"" kind=""namespace"" importlevel=""project"" />
        <alias name=""A2"" target=""System.Int64"" kind=""namespace"" importlevel=""project"" />
        <namespace name=""System.Threading"" importlevel=""project"" />
        <currentnamespace name=""N3"" />
        <scope startOffset=""0x21"" endOffset=""0x51"">
          <local name=""$VB$ResumableLocal_var$0"" il_index=""0"" il_start=""0x21"" il_end=""0x51"" attributes=""0"" />
        </scope>
      </scope>
    </method>
    <method containingType=""N3.G+VB$StateMachine_2_It2"" name=""MoveNext"">
      <customDebugInfo>
        <encLocalSlotMap>
          <slot kind=""20"" offset=""-1"" />
          <slot kind=""27"" offset=""-1"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" hidden=""true"" document=""1"" />
        <entry offset=""0x21"" startLine=""71"" startColumn=""9"" endLine=""71"" endColumn=""59"" document=""1"" />
        <entry offset=""0x22"" startLine=""72"" startColumn=""17"" endLine=""72"" endColumn=""36"" document=""1"" />
        <entry offset=""0x29"" startLine=""73"" startColumn=""17"" endLine=""73"" endColumn=""36"" document=""1"" />
        <entry offset=""0x30"" startLine=""74"" startColumn=""17"" endLine=""74"" endColumn=""36"" document=""1"" />
        <entry offset=""0x37"" startLine=""75"" startColumn=""17"" endLine=""75"" endColumn=""36"" document=""1"" />
        <entry offset=""0x3e"" startLine=""76"" startColumn=""13"" endLine=""76"" endColumn=""20"" document=""1"" />
        <entry offset=""0x59"" startLine=""77"" startColumn=""13"" endLine=""77"" endColumn=""36"" document=""1"" />
        <entry offset=""0x65"" startLine=""78"" startColumn=""13"" endLine=""78"" endColumn=""36"" document=""1"" />
        <entry offset=""0x71"" startLine=""79"" startColumn=""13"" endLine=""79"" endColumn=""36"" document=""1"" />
        <entry offset=""0x7d"" startLine=""80"" startColumn=""13"" endLine=""80"" endColumn=""36"" document=""1"" />
        <entry offset=""0x89"" startLine=""81"" startColumn=""9"" endLine=""81"" endColumn=""21"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x8b"">
        <importsforward declaringType=""N3.G+VB$StateMachine_1_It1"" methodName=""MoveNext"" />
        <scope startOffset=""0x21"" endOffset=""0x8a"">
          <local name=""$VB$ResumableLocal_var1$0"" il_index=""0"" il_start=""0x21"" il_end=""0x8a"" attributes=""0"" />
          <local name=""$VB$ResumableLocal_var2$1"" il_index=""1"" il_start=""0x21"" il_end=""0x8a"" attributes=""0"" />
          <local name=""$VB$ResumableLocal_var3$2"" il_index=""2"" il_start=""0x21"" il_end=""0x8a"" attributes=""0"" />
          <local name=""$VB$ResumableLocal_var4$3"" il_index=""3"" il_start=""0x21"" il_end=""0x8a"" attributes=""0"" />
        </scope>
      </scope>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Convert_Misc()
        {
            VerifyWindowsPdb(
                TestResources.Misc.DllAndPdb(portable: true),
                TestResources.Misc.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Misc.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
  </files>
  <methods>
    <method containingType=""C`1"" name=""M"" parameterNames=""b"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""3"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""0"" offset=""15"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""13"" startColumn=""5"" endLine=""13"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""14"" startColumn=""9"" endLine=""14"" endColumn=""34"" document=""1"" />
        <entry offset=""0x7"" startLine=""15"" startColumn=""9"" endLine=""15"" endColumn=""28"" document=""1"" />
        <entry offset=""0x13"" startLine=""16"" startColumn=""9"" endLine=""16"" endColumn=""18"" document=""1"" />
        <entry offset=""0x1c"" startLine=""17"" startColumn=""5"" endLine=""17"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <namespace name=""System"" />
        <namespace name=""System.Collections.Generic"" />
        <alias name=""X"" target=""C`1[[System.Int32*[], mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]"" kind=""type"" />
        <local name=""c"" il_index=""0"" il_start=""0x0"" il_end=""0x1e"" attributes=""0"" />
      </scope>
    </method>
    <method containingType=""C`1"" name="".ctor"">
      <customDebugInfo>
        <forward declaringType=""C`1"" methodName=""M"" parameterNames=""b"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""30"" document=""1"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""5"" endLine=""10"" endColumn=""28"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>");
        }

        [Fact]
        public void Convert_SourceLinkToSourceData()
        {
            VerifyWindowsMatchesExpected(
                TestResources.SourceData.WindowsDllAndPdb,
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Documents.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
    <file id=""2"" name=""C:\a\b\c\d\1.cs"" language=""C#"" />
    <file id=""3"" name=""C:\a\b\c\D\2.cs"" language=""C#"" />
    <file id=""4"" name=""C:\a\b\C\d\3.cs"" language=""C#"" />
    <file id=""5"" name=""C:\a\b\c\d\x.cs"" language=""C#"" />
    <file id=""6"" name=""C:\A\b\c\x.cs"" language=""C#"" />
    <file id=""7"" name=""C:\a\b\x.cs"" language=""C#"" />
    <file id=""8"" name=""C:\a\B\3.cs"" language=""C#"" />
    <file id=""9"" name=""C:\a\B\c\4.cs"" language=""C#"" />
    <file id=""10"" name=""C:\*\5.cs"" language=""C#"" />
    <file id=""11"" name="":6.cs"" language=""C#"" />
    <file id=""12"" name=""C:\a\b\X.cs"" language=""C#"" />
    <file id=""13"" name=""C:\a\B\x.cs"" language=""C#"" />
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
  <srcsvr><![CDATA[SRCSRV: ini ------------------------------------------------
VERSION=2
SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRVVERCTRL=http
SRCSRVTRG=%RAWURL%
SRCSRV: source files ---------------------------------------
C:\Documents.cs*3/Documents.cs.g
C:\a\b\c\d\1.cs*1/a/b/c/d/1.cs
C:\a\b\c\D\2.cs*1/a/b/c/D/2.cs
C:\a\b\C\d\3.cs*1/a/b/C/d/3.cs
C:\a\b\c\d\x.cs*1/a/b/c/d/x.cs
C:\A\b\c\x.cs*1/a/b/c/x.cs
C:\a\b\x.cs*1/a/b/x.cs
C:\a\B\3.cs*1/a/B/3.cs
C:\a\B\c\4.cs*1/a/B/c/4.cs
:6.cs*4/%3A6.cs
C:\a\b\X.cs*1/a/b/X.cs
C:\a\B\x.cs*1/a/B/x.cs
SRCSRV: end ------------------------------------------------]]></srcsvr>
</symbols>
");

            VerifyWindowsConvertedFromPortableMatchesExpected(
                TestResources.SourceLink.DllAndPdb(portable: true),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Documents.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
    <file id=""2"" name=""C:\a\b\c\d\1.cs"" language=""C#"" />
    <file id=""3"" name=""C:\a\b\c\D\2.cs"" language=""C#"" />
    <file id=""4"" name=""C:\a\b\C\d\3.cs"" language=""C#"" />
    <file id=""5"" name=""C:\a\b\c\d\x.cs"" language=""C#"" />
    <file id=""6"" name=""C:\A\b\c\x.cs"" language=""C#"" />
    <file id=""7"" name=""C:\a\b\x.cs"" language=""C#"" />
    <file id=""8"" name=""C:\a\B\3.cs"" language=""C#"" />
    <file id=""9"" name=""C:\a\B\c\4.cs"" language=""C#"" />
    <file id=""10"" name=""C:\*\5.cs"" language=""C#"" />
    <file id=""11"" name="":6.cs"" language=""C#"" />
    <file id=""12"" name=""C:\a\b\X.cs"" language=""C#"" />
    <file id=""13"" name=""C:\a\B\x.cs"" language=""C#"" />
    <file id=""14"" name=""D:\symreader-converter\src\PdbTestResources\Resources\EmbeddedSourceNoCode.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""FF-08-24-28-1D-46-0C-E0-4C-DE-9B-1E-8E-7C-20-2A-7D-14-B5-95-B4-CC-E9-F8-9D-BD-3B-70-3A-37-C9-E4""><![CDATA[// file with no code
]]></file>
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
  <sourceLink><![CDATA[{
  ""documents"": {
    ""C:\\a*"": ""http://server/1/a*"",
    ""C:\\A*"": ""http://server/2/A*"",
    ""C:\\*"": ""http://server/3/*.g"",
    "":*"": ""http://server/4/*""
  }
}]]></sourceLink>
  <srcsvr><![CDATA[SRCSRV: ini ------------------------------------------------
VERSION=2
SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRVVERCTRL=http
SRCSRVTRG=%RAWURL%
SRCSRV: source files ---------------------------------------
C:\Documents.cs*3/Documents.cs.g
C:\a\b\c\d\1.cs*1/a/b/c/d/1.cs
C:\a\b\c\D\2.cs*1/a/b/c/D/2.cs
C:\a\b\C\d\3.cs*1/a/b/C/d/3.cs
C:\a\b\c\d\x.cs*1/a/b/c/d/x.cs
C:\A\b\c\x.cs*1/a/b/c/x.cs
C:\a\b\x.cs*1/a/b/x.cs
C:\a\B\3.cs*1/a/B/3.cs
C:\a\B\c\4.cs*1/a/B/c/4.cs
:6.cs*4/6.cs
C:\a\b\X.cs*1/a/b/X.cs
C:\a\B\x.cs*1/a/B/x.cs
SRCSRV: end ------------------------------------------------]]></srcsvr>
</symbols>
",
            new[]
            {
                new PdbDiagnostic(PdbDiagnosticId.UnmappedDocumentName, 0, new[] { @"C:\*\5.cs" })
            },
            PortablePdbConversionOptions.Default, 
            validateTimeIndifference: false);
        }

        [Fact]
        public void Convert_SourceLinkToSourceDataSrcSvrVars()
        {
            VerifyWindowsConvertedFromPortableMatchesExpected(
                TestResources.SourceLink.DllAndPdb(portable: true),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\Documents.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE"" />
    <file id=""2"" name=""C:\a\b\c\d\1.cs"" language=""C#"" />
    <file id=""3"" name=""C:\a\b\c\D\2.cs"" language=""C#"" />
    <file id=""4"" name=""C:\a\b\C\d\3.cs"" language=""C#"" />
    <file id=""5"" name=""C:\a\b\c\d\x.cs"" language=""C#"" />
    <file id=""6"" name=""C:\A\b\c\x.cs"" language=""C#"" />
    <file id=""7"" name=""C:\a\b\x.cs"" language=""C#"" />
    <file id=""8"" name=""C:\a\B\3.cs"" language=""C#"" />
    <file id=""9"" name=""C:\a\B\c\4.cs"" language=""C#"" />
    <file id=""10"" name=""C:\*\5.cs"" language=""C#"" />
    <file id=""11"" name="":6.cs"" language=""C#"" />
    <file id=""12"" name=""C:\a\b\X.cs"" language=""C#"" />
    <file id=""13"" name=""C:\a\B\x.cs"" language=""C#"" />
    <file id=""14"" name=""D:\symreader-converter\src\PdbTestResources\Resources\EmbeddedSourceNoCode.cs"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""FF-08-24-28-1D-46-0C-E0-4C-DE-9B-1E-8E-7C-20-2A-7D-14-B5-95-B4-CC-E9-F8-9D-BD-3B-70-3A-37-C9-E4""><![CDATA[// file with no code
]]></file>
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
  <sourceLink><![CDATA[{
  ""documents"": {
    ""C:\\a*"": ""http://server/1/a*"",
    ""C:\\A*"": ""http://server/2/A*"",
    ""C:\\*"": ""http://server/3/*.g"",
    "":*"": ""http://server/4/*""
  }
}]]></sourceLink>
  <srcsvr><![CDATA[SRCSRV: ini ------------------------------------------------
VERSION=2
SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRVVERCTRL=http
SRCSRVTRG=%RAWURL%
ABC=*
XYZ=123
SRCSRV: source files ---------------------------------------
C:\Documents.cs*3/Documents.cs.g
C:\a\b\c\d\1.cs*1/a/b/c/d/1.cs
C:\a\b\c\D\2.cs*1/a/b/c/D/2.cs
C:\a\b\C\d\3.cs*1/a/b/C/d/3.cs
C:\a\b\c\d\x.cs*1/a/b/c/d/x.cs
C:\A\b\c\x.cs*1/a/b/c/x.cs
C:\a\b\x.cs*1/a/b/x.cs
C:\a\B\3.cs*1/a/B/3.cs
C:\a\B\c\4.cs*1/a/B/c/4.cs
:6.cs*4/6.cs
C:\a\b\X.cs*1/a/b/X.cs
C:\a\B\x.cs*1/a/B/x.cs
SRCSRV: end ------------------------------------------------]]></srcsvr>
</symbols>
",
new[]
{
    new PdbDiagnostic(PdbDiagnosticId.UnmappedDocumentName, 0, new[] { @"C:\*\5.cs" })
},
new PortablePdbConversionOptions(srcSvrVariables: new[] 
{
    new KeyValuePair<string, string>("ABC", "*"),
    new KeyValuePair<string, string>("XYZ", "123")
}),
    validateTimeIndifference: true);
        }

        [Fact]
        public void Convert_EmbeddedSource()
        {
            VerifyWindowsPdb(
                TestResources.EmbeddedSource.DllAndPdb(portable: true),
                TestResources.EmbeddedSource.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <files>
    <file id=""1"" name=""C:\EmbeddedSourceSmall.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""48-30-92-B9-4A-92-50-A7-75-33-E8-05-0D-2E-DD-CD-3F-58-9F-7F""><![CDATA[// should be less than compression threshold (200 chars)
public class Small
{
    public Small() {}
}]]></file>
    <file id=""2"" name=""C:\EmbeddedSource.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""1D-BB-A3-11-47-F6-42-7D-82-99-B4-31-E9-32-7D-6B-09-C3-59-EB""><![CDATA[// should be higher than compression threshold (200 chars)

using System;

namespace Test
{
    public static class SomeCode
    {
        public static int SomeMethod(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return checked(value + 42);
        }
    }
}
]]></file>
    <file id=""3"" name=""C:\EmbeddedSourceNoCode.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""4B-4C-47-82-20-10-AC-63-A9-24-1E-33-CE-BA-74-76-40-F3-33-BB""><![CDATA[// file with no code
]]></file>
    <file id=""4"" name=""C:\EmbeddedSourceNoSequencePoints.cs"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8B-74-C4-70-25-F1-77-43-94-81-81-6A-D1-80-BA-F4-57-12-58-5E""><![CDATA[// file with no sequence points

interface I { }
]]></file>
  </files>
  <methods>
    <method containingType=""Small"" name="".ctor"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""4"" startColumn=""5"" endLine=""4"" endColumn=""19"" document=""1"" />
        <entry offset=""0x7"" startLine=""4"" startColumn=""20"" endLine=""4"" endColumn=""21"" document=""1"" />
        <entry offset=""0x8"" startLine=""4"" startColumn=""21"" endLine=""4"" endColumn=""22"" document=""1"" />
      </sequencePoints>
    </method>
    <method containingType=""Test.SomeCode"" name=""SomeMethod"" parameterNames=""value"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""0"" />
          <namespace usingCount=""1"" />
        </using>
        <encLocalSlotMap>
          <slot kind=""1"" offset=""15"" />
          <slot kind=""21"" offset=""0"" />
        </encLocalSlotMap>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""10"" document=""2"" />
        <entry offset=""0x1"" startLine=""11"" startColumn=""13"" endLine=""11"" endColumn=""27"" document=""2"" />
        <entry offset=""0x6"" hidden=""true"" document=""2"" />
        <entry offset=""0x9"" startLine=""12"" startColumn=""13"" endLine=""12"" endColumn=""14"" document=""2"" />
        <entry offset=""0xa"" startLine=""13"" startColumn=""17"" endLine=""13"" endColumn=""70"" document=""2"" />
        <entry offset=""0x15"" startLine=""16"" startColumn=""13"" endLine=""16"" endColumn=""40"" document=""2"" />
        <entry offset=""0x1c"" startLine=""17"" startColumn=""9"" endLine=""17"" endColumn=""10"" document=""2"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x1e"">
        <namespace name=""System"" />
      </scope>
    </method>
  </methods>
</symbols>
");
        }
    }
}
