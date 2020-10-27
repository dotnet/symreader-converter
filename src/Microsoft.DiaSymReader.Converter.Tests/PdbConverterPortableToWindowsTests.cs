// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System;
using System.Collections.Generic;
using Roslyn.Test.Utilities;
using System.Text;
using System.Text.Json;

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

            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable(null!, "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("-", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("ABC_[", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("0ABC", "", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "a\r", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "a\n", ""));
            Assert.Throws<ArgumentException>(() => PdbConverterPortableToWindows.ValidateSrcSvrVariable("A", "a\0", ""));
        }

        private void ValidateSourceLinkConversion(string[] documents, string sourceLink, string expectedSrcSvr, PdbDiagnostic[]? expectedErrors = null)
        {
            var actualErrors = new List<PdbDiagnostic>();
            var converter = new PdbConverterPortableToWindows(actualErrors.Add);
            var actualSrcSvr = converter.ConvertSourceServerData(sourceLink, documents, PortablePdbConversionOptions.Default);

            AssertEx.Equal(expectedErrors ?? Array.Empty<PdbDiagnostic>(), actualErrors);
            AssertEx.AssertLinesEqual(expectedSrcSvr, actualSrcSvr!);
        }

        [Fact]
        public void SourceLinkConversion_NoDocs()
        {
            ValidateSourceLinkConversion(new string[0],
@"{
   ""documents"" : 
   {
      ""C:\\a*"": ""http://server/1/a*"",
   }
}",
            null!);
        }

        [Fact]
        public void SourceLinkConversion_BOM()
        {
            ValidateSourceLinkConversion(new[]
            {
                @"C:\a\1.cs"
            },
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetString(Encoding.UTF8.GetBytes(
@"{
   ""documents"" : 
   {
      ""C:\\a*"": ""http://server/X/Y*"",
   }
}")),
@"
SRCSRV: ini ------------------------------------------------
VERSION=2
SRCSRV: variables ------------------------------------------
RAWURL=http://server/X/Y/1.cs%var2%
SRCSRVVERCTRL=http
SRCSRVTRG=%RAWURL%
SRCSRV: source files ---------------------------------------
C:\a\1.cs*
SRCSRV: end ------------------------------------------------
");
        }

        [Fact]
        public void SourceLinkConversion_EmptySourceLink()
        {
            ValidateSourceLinkConversion(new[]
            {
                @"C:\a\1.cs"
            },
@"{
   ""documents"" : 
   {
   }
}",
            null!,
            new[]
            {
                new PdbDiagnostic(PdbDiagnosticId.UnmappedDocumentName, 0, new[] { @"C:\a\1.cs" }),
                new PdbDiagnostic(PdbDiagnosticId.NoSupportedUrlsFoundInSourceLink, 0, Array.Empty<object>())
            });
        }

        [Fact]
        public void SourceLinkConversion_BadJson_Key()
        {
            var json = @"
{
    ""documents"" : 
    {
        1: ""http://server/X/Y*"",
    }
}";

            string? error = null;
            try
            {
                JsonDocument.Parse(json);
            }
            catch (JsonException e)
            {
                error = e.Message;
            }

            ValidateSourceLinkConversion(new[]
            {
                @"C:\a\1.cs"
            },            
            json,
            null!, 
            new[]
            {
                new PdbDiagnostic(PdbDiagnosticId.InvalidSourceLink, 0, new[] { error! })
            });
        }

        [Fact]
        public void SourceLinkConversion_BadJson_NullValue()
        {
            ValidateSourceLinkConversion(new[]
            {
                @"C:\a\1.cs",
                @"C:\a\2.cs",
            },
@"{
   ""documents"" : 
   {
      ""1"": null,
      ""2"": {},
   }
}",
            null!,
            new[]
            {
                new PdbDiagnostic(PdbDiagnosticId.InvalidSourceLink, 0, new[] { ConverterResources.InvalidJsonDataFormat })
            });
        }

        [Fact]
        public void SourceLinkConversion_BadJson2()
        {
            var json = @"{
   ""documents"" : 
   {
      1: ""http://server/X/Y*"",
};";

            Exception? expectedException = null;
            try
            {
                _ = JsonDocument.Parse(json);
            }
            catch (JsonException e)
            {
                expectedException = e;
            }

            ValidateSourceLinkConversion(new[]
            {
                @"C:\a\1.cs"
            },
            json,
            null!,
            new[]
            {
                new PdbDiagnostic(PdbDiagnosticId.InvalidSourceLink, 0, new[] { expectedException!.Message })
            });
        }

        [Fact]
        public void SourceLinkConversion_MalformedUrls()
        {
            ValidateSourceLinkConversion(new[]
            {
                @"C:\src\a\1.cs",
                @"C:\src\b\2.cs",
                @"C:\src\c\3.cs",
                @"D:",
            },
@"{
   ""documents"" : 
   {
      ""C:\\src\\a\\*"": ""http://server/1/%/*"",
      ""C:\\src\\b\\2.*"": ""*://server/2/"",
      ""C:\\src\\c\\*"": ""https://server/3/"",
      ""D*"": ""http*//server/2/a.cs"",
   }
}",
@"
SRCSRV: ini ------------------------------------------------
VERSION=2
SRCSRV: variables ------------------------------------------
RAWURL=https://server/3/3.cs%var2%
SRCSRVVERCTRL=https
SRCSRVTRG=%RAWURL%
SRCSRV: source files ---------------------------------------
C:\src\c\3.cs*
SRCSRV: end ------------------------------------------------",
            new[] 
            {
                new PdbDiagnostic(PdbDiagnosticId.MalformedSourceLinkUrl, 0, new[] { "http://server/1/%/1.cs" }),
                new PdbDiagnostic(PdbDiagnosticId.UrlSchemeIsNotHttp, 0, new[] { "cs://server/2/" }),
                new PdbDiagnostic(PdbDiagnosticId.MalformedSourceLinkUrl, 0, new[] { "http%3A//server/2/a.cs" }),
            });
        }

        [Fact]
        public void SourceChecksumValidation()
        {
            static void ValidateSourceChecksum(Guid guid, Guid correctedGuid, byte[] checksum, string documentName, params PdbDiagnostic[] expectedErrors)
            {
                var actualErrors = new List<PdbDiagnostic>();
                var converter = new PdbConverterPortableToWindows(actualErrors.Add);
                var originalGuid = guid;
                converter.ValidateAndCorrectSourceChecksum(ref guid, checksum, documentName);

                AssertEx.Equal(expectedErrors, actualErrors);
                Assert.True(expectedErrors.Length != 0 || guid == originalGuid);
                Assert.Equal(correctedGuid, guid);
            }

            var sha1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
            var sha256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");
            var unknown = new Guid("11111111-1111-1111-1111-111111111111");

            // SHA1
            ValidateSourceChecksum(sha1, sha1, Array.Empty<byte>(), "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { "SHA1", "doc1.cs" }));

            ValidateSourceChecksum(sha1, sha1, new byte[32], "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { "SHA1", "doc1.cs" }));

            ValidateSourceChecksum(default, sha1, new byte[20], "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { ConverterResources.None, "doc1.cs" }));

            ValidateSourceChecksum(default, default, new byte[22], "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { ConverterResources.None, "doc1.cs" }));

            ValidateSourceChecksum(sha1, sha1, new byte[20], "doc1.cs");

            // SHA256
            ValidateSourceChecksum(sha256, sha256, Array.Empty<byte>(), "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { "SHA256", "doc1.cs" }));

            ValidateSourceChecksum(sha256, sha256, new byte[20], "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { "SHA256", "doc1.cs" }));

            ValidateSourceChecksum(default, sha256, new byte[32], "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { ConverterResources.None, "doc1.cs" }));

            ValidateSourceChecksum(default, default, new byte[30], "doc1.cs",
                new PdbDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { ConverterResources.None, "doc1.cs" }));

            ValidateSourceChecksum(sha256, sha256, new byte[32], "doc1.cs");

            // unknown
            ValidateSourceChecksum(unknown, unknown, new byte[0], "doc1.cs");
            ValidateSourceChecksum(unknown, unknown, new byte[1], "doc1.cs");
        }
    }
}
