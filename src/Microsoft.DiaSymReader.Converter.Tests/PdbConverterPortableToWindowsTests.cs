// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System;
using System.Collections.Generic;
using Roslyn.Test.Utilities;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public class PdbConverterPortableToWindowsTests
    {
        [Fact]
        public void ValidateSrcSvrVariables()
        {
            PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "", "");
            PdbConverterPortableToWindows.ValidateSrcSvrVariable("AZaz09_", "", "");
            PdbConverterPortableToWindows.ValidateSrcSvrVariable("ABC", "", "");

            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable(null, "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("-", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("ABC_[", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("0ABC", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "a\r", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "a\n", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "a\0", ""));
        }

        private void ValidateSourceLinkConversion(string[] documents, string sourceLink, string expectedSrcSvr, PdbDiagnostic[] expectedErrors = null)
        {
            var actualErrors = new List<PdbDiagnostic>();
            var converter = new PdbConverterPortableToWindows(actualErrors.Add);
            var actualSrcSvr = converter.ConvertSourceServerData(sourceLink, documents, PortablePdbConversionOptions.Default);

            AssertEx.Equal(expectedSrcSvr, actualSrcSvr);
            AssertEx.Equal(expectedErrors ?? Array.Empty<PdbDiagnostic>(), actualErrors);
        }

        [Fact]
        public void SourceLinkConversion()
        {
            ValidateSourceLinkConversion(new[]
            {
                @"C:\src\a\b.cs",
                @"C:\src\a\b.cs",
                @"C:\src\a\b.cs",
                @"C:\src\a\b.cs",
            },
@"{
   ""documents"" : 
   {
      ""C:\\a*"": ""http://server/1/a*"",
   }
}",
@"

",
            new[] 
            {
                new PdbDiagnostic(PdbDiagnosticId.UnmappedDocumentName, 0, new[] { @"C:\*\5.cs" })
            });
        }
    }
}
