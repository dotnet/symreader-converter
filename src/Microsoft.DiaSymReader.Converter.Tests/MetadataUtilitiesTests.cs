// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.DiaSymReader.PortablePdb;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    internal class MetadataUtilitiesTests
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
