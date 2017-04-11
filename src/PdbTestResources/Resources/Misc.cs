#line 1 "C:\Misc.cs"
#pragma checksum "C:\Misc.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"

using System;
using System.Collections.Generic;
using X = C<int*[]>;

unsafe class C<T>
{
    private void*[] x = null;
    private void* y = null;

    public void* M<S>(Action<T, S> b)
    {
        var c = new C<void*[]>();
        Console.Write(c.x); // MemberRef to TypeSpec C<void*[]>
        return y;
    }
}