// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class StringUtilities
    {
        public static int GetLongestCommonSuffixLength(string left, string right)
        {
            int n = Math.Min(left.Length, right.Length);

            for (int i = 0; i < n - 1; i++)
            {
                if (left[left.Length - i - 1] != right[right.Length - i - 1])
                {
                    return i;
                }
            }

            return n;
        }

        public static string GetLongestCommonPrefix(IEnumerable<string> strings)
        {
            int prefixLength = 0;
            string? firstString = null;
            foreach (var str in strings)
            {
                if (firstString == null)
                {
                    firstString = str;
                    prefixLength = str.Length;
                }
                else
                {
                    int i = IndexOfFirstDifference(str, firstString, prefixLength);
                    if (i == 0)
                    {
                        return string.Empty;
                    }

                    if (i > 0)
                    {
                        prefixLength = i;
                    }
                }
            }

            return firstString?.Substring(0, prefixLength) ?? string.Empty;
        }

        public static int IndexOfFirstDifference(string left, string right, int length)
        {
            for (int i = 0, n = Math.Min(left.Length, length); i < n; i++)
            {
                if (left[i] != right[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
