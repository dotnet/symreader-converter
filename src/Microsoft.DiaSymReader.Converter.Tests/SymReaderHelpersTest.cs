// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

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
