// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Reflection.PortableExecutable;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public class SymReaderHelpersTest
    {
        [Fact]
        public void TryReadPdbId_CrossGen_Portable()
        {
            using (var peStream = TestResources.CrossGen.PortableDll)
            {
                using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
                {
                    Assert.True(SymReaderHelpers.TryReadPdbId(peReader, out var pePdbId, out int peAge));
                    Assert.True(pePdbId.Guid == TestResources.CrossGen.PortableDllDebugDirectoryEntryGuid);
                    Assert.True(peAge == 1);
                }
            }
        }

        [Fact]
        public void TryReadPdbId_CrossGen_Windows()
        {
            using (var peStream = TestResources.CrossGen.WindowsDll)
            {
                using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
                {
                    Assert.True(SymReaderHelpers.TryReadPdbId(peReader, out var pePdbId, out int peAge));
                    Assert.True(pePdbId.Guid == TestResources.CrossGen.WindowsDllDebugDirectoryEntryGuid);
                    Assert.True(peAge == 1);
                }
            }
        }
    }
}
