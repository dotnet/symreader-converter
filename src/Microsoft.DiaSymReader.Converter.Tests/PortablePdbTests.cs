// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    using static PdbValidationXml;
   
    public class PortablePdbTests
    {
        [Fact]
        public void CompilationOptions_Portable()
        {
            VerifyPortablePdb(
                TestResources.Documents.DllAndPdb(portable: true),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <customDebugInfo>
    <compilationOptions>
      <option name=""version"" value=""2"" />
      <option name=""compiler-version"" value=""4.0.0-6.21521.2+68d3c0e77ff8607adca62a883197a5637a596438"" />
      <option name=""language"" value=""C#"" />
      <option name=""source-file-count"" value=""1"" />
      <option name=""output-kind"" value=""DynamicallyLinkedLibrary"" />
      <option name=""platform"" value=""AnyCpu"" />
      <option name=""runtime-version"" value=""4.8.4300.0"" />
      <option name=""language-version"" value=""10.0"" />
    </compilationOptions>
    <compilationMetadataReferences>
      <reference fileName=""mscorlib.dll"" flags=""Assembly"" timeStamp=""0x5F7E60F6"" fileSize=""0x0056C000"" mvid=""220f2aab-ed1d-4738-94a8-8b65aaf6c105"" />
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
            PdbToXmlOptions.IncludeModuleDebugInfo |
            PdbToXmlOptions.SymReaderLoadPolicyUseAlternateDirectory);
        }

        [Fact]
        public void CompilationOptions_Windows()
        {
            VerifyPortablePdb(
                TestResources.Documents.DllAndPdb(portable: false),
@"<?xml version=""1.0"" encoding=""utf-16""?>
<symbols>
  <compilerInfo version=""4.0.21.52102"" name=""C# - 4.0.0-6.21521.2+68d3c0e77ff8607adca62a883197a5637a596438"" />
</symbols>
",
            PdbToXmlOptions.ExcludeDocuments |
            PdbToXmlOptions.ExcludeMethods |
            PdbToXmlOptions.ExcludeSequencePoints |
            PdbToXmlOptions.ExcludeScopes |
            PdbToXmlOptions.ExcludeNamespaces |
            PdbToXmlOptions.ExcludeAsyncInfo |
            PdbToXmlOptions.ExcludeCustomDebugInformation |
            PdbToXmlOptions.IncludeModuleDebugInfo |
            PdbToXmlOptions.SymReaderLoadPolicyUseAlternateDirectory);
        }
    }
}
