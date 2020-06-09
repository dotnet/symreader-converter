csc /target:library /debug:portable /optimize- /noconfig /deterministic /pathmap:%~dp0=/_/ Documents.cs
copy /y Documents.pdb Documents.pdbx
copy /y Documents.dll Documents.dllx

csc /target:library /debug+ /optimize- /noconfig /deterministic /pathmap:%~dp0=/_/ Documents.cs


