csc /target:library /debug:portable /optimize- /unsafe+ /deterministic Misc.cs
copy /y Misc.pdb Misc.pdbx
copy /y Misc.dll Misc.dllx

csc /target:library /debug+ /optimize- /unsafe+ /deterministic Misc.cs