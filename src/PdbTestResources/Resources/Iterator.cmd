csc /target:library /debug:portable /optimize- /deterministic Iterator.cs
copy /y Iterator.pdb Iterator.pdbx
copy /y Iterator.dll Iterator.dllx

csc /target:library /debug+ /optimize- /deterministic Iterator.cs


