// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class EnumerableUtilities
    {
        public static int IndexOf<T>(this IEnumerable<T> sequence, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (var item in sequence)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static IEnumerable<(T Value, int Index)> SelectWithIndex<T>(this IEnumerable<T> sequence)
        {
            int index = 0;
            foreach (var item in sequence)
            {
                yield return (item, index++);
            }
        }
    }
}
