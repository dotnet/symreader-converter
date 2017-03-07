// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public unsafe class InteropUtilitiesTests
    {
        [Fact]
        public void CopyQualifiedTypeName()
        {
            InteropUtilities.CopyQualifiedTypeName(null, null, "", "");
            InteropUtilities.CopyQualifiedTypeName(null, null, "Alpha", "Beta");

            var buffer = new char[12];

            void ClearBuffer()
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = 'x';
                }
            }

            fixed (char* bufferPtr = &buffer[0])
            {
                int length;

                ClearBuffer();

                InteropUtilities.CopyQualifiedTypeName(null, &length, "", "");
                Assert.Equal(0, length);
                length = -1;

                InteropUtilities.CopyQualifiedTypeName(bufferPtr + 1, &length, "", "");
                AssertEx.Equal(new char[] { 'x', '\0', 'x', 'x', 'x', 'x', 'x', 'x', 'x', 'x', 'x', 'x' }, buffer);
                Assert.Equal(0, length);

                ClearBuffer();

                InteropUtilities.CopyQualifiedTypeName(null, &length, "", "B");
                Assert.Equal(1, length);
                length = -1;

                InteropUtilities.CopyQualifiedTypeName(bufferPtr + 1, &length, "", "B");
                AssertEx.Equal(new char[] { 'x', 'B', '\0', 'x', 'x', 'x', 'x', 'x', 'x', 'x', 'x', 'x' }, buffer);
                Assert.Equal(1, length);

                ClearBuffer();

                InteropUtilities.CopyQualifiedTypeName(null, &length, "A", "B");
                Assert.Equal(3, length);
                length = -1;

                InteropUtilities.CopyQualifiedTypeName(bufferPtr + 1, &length, "A", "B");
                AssertEx.Equal(new char[]  { 'x', 'A', '.', 'B', '\0', 'x', 'x', 'x', 'x', 'x', 'x', 'x' }, buffer);
                Assert.Equal(3, length);

                ClearBuffer();

                InteropUtilities.CopyQualifiedTypeName(null, &length, "Alpha", "Beta");
                Assert.Equal(10, length);
                length = -1;

                InteropUtilities.CopyQualifiedTypeName(bufferPtr, &length, "Alpha", "Beta");
                AssertEx.Equal(new char[] { 'A', 'l', 'p', 'h', 'a', '.', 'B', 'e', 't', 'a', '\0', 'x' }, buffer);
                Assert.Equal(10, length);
            }
        }
    }
}
