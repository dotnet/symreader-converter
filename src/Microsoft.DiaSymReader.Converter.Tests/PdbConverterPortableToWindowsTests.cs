// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System;

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
    }
}
