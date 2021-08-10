// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

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
