csc /target:library /debug:portable /optimize- /deterministic /r:ExternAlias1=System.Core.dll Imports.cs
copy /y Imports.pdb Imports.pdbx
copy /y Imports.dll Imports.dllx

csc /target:library /debug+ /optimize- /deterministic /r:ExternAlias1=System.Core.dll Imports.cs


