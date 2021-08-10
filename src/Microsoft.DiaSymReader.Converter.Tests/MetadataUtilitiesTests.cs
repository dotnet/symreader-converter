// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.DiaSymReader.PortablePdb;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public class MetadataUtilitiesTests
    {
        [Fact]
        public void SerializeDynamicInfo()
        {
            var builder = new BlobBuilder();

            MetadataUtilities.SerializeBitVector(builder, ImmutableArray.Create(
                true, true, false, false, false, false, false, true,
                true, true, true, false, false, false, false, true,
                false, true));

            AssertEx.Equal(new byte[] { 0b10000011, 0b10000111, 0b00000010 }, builder.ToArray());
            builder.Clear();

            MetadataUtilities.SerializeBitVector(builder, ImmutableArray.Create(
                false, true, false, false, false, false, false, false, false, false, false));

            AssertEx.Equal(new byte[] { 0b00000010 }, builder.ToArray());
            builder.Clear();

            MetadataUtilities.SerializeBitVector(builder, ImmutableArray.Create(
                true, true, false, false, false, false, false, true,
                true, true, true, false, false, false, false, true,
                true, true, true, true, false, false, false, true,
                true, true, true, true, true, false, false, true,
                true, true, false, false, false, false, false, true,
                true, true, true, false, false, false, false, true,
                true, true, true, true, false, false, false, true,
                true, true, true, true, true, false, false, true,
                false, true));

            AssertEx.Equal(new byte[] { 0b10000011, 0b10000111, 0b10001111, 0b10011111, 0b10000011, 0b10000111, 0b10001111, 0b10011111, 0b00000010 }, builder.ToArray());
            builder.Clear();
        }
    }
}
