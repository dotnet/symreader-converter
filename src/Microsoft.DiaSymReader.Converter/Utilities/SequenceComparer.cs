﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal sealed class SequenceComparer<T> : IEqualityComparer<T[]>, IEqualityComparer<ImmutableArray<T>>
    {
        internal static readonly SequenceComparer<T> Instance = new SequenceComparer<T>(EqualityComparer<T>.Default);

        private readonly IEqualityComparer<T> _elementComparer;

        private SequenceComparer(IEqualityComparer<T> elementComparer)
        {
            _elementComparer = elementComparer;
        }

        internal static bool Equals(ImmutableArray<T> x, ImmutableArray<T> y, IEqualityComparer<T> elementComparer)
        {
            if (x == y)
            {
                return true;
            }

            if (x.IsDefault || y.IsDefault || x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (!elementComparer.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool Equals(T[] left, int leftStart, T[] right, int rightStart, int length, IEqualityComparer<T> elementComparer)
        {
            if (left == null || right == null)
            {
                return ReferenceEquals(left, right);
            }

            if (ReferenceEquals(left, right) && leftStart == rightStart)
            {
                return true;
            }

            for (var i = 0; i < length; i++)
            {
                if (!elementComparer.Equals(left[leftStart + i], right[rightStart + i]))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool Equals(T[] left, T[] right, IEqualityComparer<T> elementComparer)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (!elementComparer.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        bool IEqualityComparer<T[]>.Equals(T[] x, T[] y) => Equals(x, y, _elementComparer);
        bool IEqualityComparer<ImmutableArray<T>>.Equals(ImmutableArray<T> x, ImmutableArray<T> y) => Equals(x, y, _elementComparer);

        int IEqualityComparer<T[]>.GetHashCode(T[] x) => Hash.CombineValues(x, _elementComparer);
        int IEqualityComparer<ImmutableArray<T>>.GetHashCode(ImmutableArray<T> x) => Hash.CombineValues(x, _elementComparer);
    }
}
