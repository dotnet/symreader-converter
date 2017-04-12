// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class EnumerableUtilities
    {
        public static int IndexOf<T>(this IEnumerable<T> sequence, Func<T, bool> selector) =>
            SelectWithIndex(sequence, selector).Index;

        public static (T Item, int Index) SelectWithIndex<T>(this IEnumerable<T> sequence, Func<T, bool> selector)
        {
            int index = 0;
            foreach (var item in sequence)
            {
                if (selector(item))
                {
                    return (item, index);
                }

                index++;
            }

            return (default(T), -1);
        }
    }
}
