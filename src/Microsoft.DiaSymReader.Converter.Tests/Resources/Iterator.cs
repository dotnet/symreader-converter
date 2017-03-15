#line 1 "C:\Iterator.cs"
#pragma checksum "C:\Iterator.cs" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "DBEB2A067B2F0E0D678A002C587A2806056C3DCE"

using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        int a = 1;
        const int x = 1;
        for (int i = 0; i < 10; i++)
        {
            const int y = 2;
            int b = 2;
            yield return x + y + i + a + b;
        }
    }
}