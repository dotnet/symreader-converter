#line 1 "C:\Imports.cs"
#pragma checksum "C:\Imports.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"

namespace Y
{
    extern alias ExternAlias1;
    using System;
    using ExternAlias1::System.Linq;
    using AliasedNamespace1 = System;
    using AliasedNamespace2 = ExternAlias1::System.IO;
    using static System.Math;
    using static ExternAlias1::System.Linq.Queryable;
    using AliasedType1 = System.Char;
    using AliasedType2 = ExternAlias1::System.Linq.ParallelEnumerable;
    using AliasedType3 = X.A.B;
    using AliasedType4 = System.Action<
        System.Action<X.A.B**[][,,], bool, byte, sbyte, short, ushort, char, int, uint>,
        System.Action<System.IntPtr, System.UIntPtr, long, ulong, float, double, object, string>>;
    using AliasedType5 = System.TypedReference;
    using AliasedType6 = System.Action<int[]>;

    namespace X
    {
        using AliasedType7 = C<X.A[,][]>.D<X.A[][,,]>;
        using AliasedType8 = System.Collections.Generic.Dictionary<A, A.B>.KeyCollection;

        public class A
        {
            void F() { }

            public struct B
            {
            }
        }
    }
}

public class C<T>
{
    void G() { }

    public class D<U> { }
}
