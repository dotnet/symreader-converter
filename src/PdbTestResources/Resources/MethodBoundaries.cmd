csc /target:library /debug:portable /optimize- /noconfig /deterministic /pathmap:%~dp0=/_/ MethodBoundaries.cs
copy /y MethodBoundaries.pdb MethodBoundaries.pdbx
copy /y MethodBoundaries.dll MethodBoundaries.dllx

csc /target:library /debug+ /optimize- /noconfig /deterministic /pathmap:%~dp0=/_/ MethodBoundaries.cs


