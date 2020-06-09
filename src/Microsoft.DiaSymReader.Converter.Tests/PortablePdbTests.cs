// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    using static PdbValidationXml;
   
    public class PortablePdbTests
    {
        [Fact]
        public void CompilationOptions()
        {
            VerifyPortablePdb(
                TestResources.Documents.DllAndPdb(portable: true),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <customDebugInfo>
    <compilationOptions>
      <option name=""compiler-version"" value=""3.7.0-dev"" />
      <option name=""language"" value=""C#"" />
      <option name=""portability-policy"" value=""0"" />
      <option name=""runtime-version"" value=""4.8.4180.0"" />
      <option name=""optimization"" value=""debug"" />
      <option name=""checked"" value=""False"" />
      <option name=""nullable"" value=""Disable"" />
      <option name=""unsafe"" value=""False"" />
      <option name=""language-version"" value=""8.0"" />
    </compilationOptions>
    <compilationMetadataReferences>
      <reference fileName=""mscorlib.dll"" flags=""Assembly"" timeStamp=""0x5E7D20D3"" fileSize=""0x00530000"" mvid=""4bec26b5-cbc7-4715-8442-f1499e984732"" />
    </compilationMetadataReferences>
  </customDebugInfo>
</symbols>
",
            PdbToXmlOptions.ExcludeDocuments |
            PdbToXmlOptions.ExcludeMethods |
            PdbToXmlOptions.ExcludeSequencePoints |
            PdbToXmlOptions.ExcludeScopes |
            PdbToXmlOptions.ExcludeNamespaces |
            PdbToXmlOptions.ExcludeAsyncInfo |
            PdbToXmlOptions.ExcludeCustomDebugInformation |
            PdbToXmlOptions.IncludeModuleDebugInfo);
        }
    }
}
