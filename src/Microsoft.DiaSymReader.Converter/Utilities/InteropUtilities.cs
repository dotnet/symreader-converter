// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.DiaSymReader.Tools
{
    internal static unsafe class InteropUtilities
    {
        internal static void CopyQualifiedTypeName(char* qualifiedName, int* qualifiedNameLength, string namespaceStr, string nameStr)
        {
            Debug.Assert(namespaceStr != null);
            Debug.Assert(nameStr != null);

            if (qualifiedNameLength != null)
            {
                *qualifiedNameLength = (namespaceStr.Length > 0 ? namespaceStr.Length + 1 : 0) + nameStr.Length;
            }

            if (qualifiedName != null)
            {
                char* dst = qualifiedName;

                if (namespaceStr.Length > 0)
                {
                    StringCopy(dst, namespaceStr, namespaceStr.Length, terminator: '.');
                    dst += namespaceStr.Length + 1;
                }

                StringCopy(dst, nameStr, nameStr.Length);
            }
        }

        internal static void StringCopy(char* dst, string src, int length, char terminator = '\0')
        {
#if TRUE // remove when not targeting net45
            for (int i = 0; i < length; i++)
            {
                dst[i] = src[i];
            }
#else
            int byteCount = length * sizeof(char);
            fixed (char* srcPtr = src)
            {
                Buffer.MemoryCopy(dst, srcPtr, byteCount, byteCount);
            }
#endif
            dst[length] = terminator;
        }
    }
}
