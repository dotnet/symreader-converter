// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class StringUtilities
    {
        internal static string GetLongestCommonPrefix(IEnumerable<string> strings)
        {
            int prefixLength = 0;
            string firstString = null;
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

        private static int IndexOfFirstDifference(string left, string right, int length)
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
