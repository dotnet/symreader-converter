csc /target:library /debug:portable /optimize- /deterministic /noconfig /nosdkpath /r:%WINDIR%\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll /pathmap:%~dp0=/ /checksumalgorithm:sha256 Documents.cs
copy /y Documents.pdb Documents.pdbx
copy /y Documents.dll Documents.dllx

csc /target:library /debug+ /optimize- /deterministic /noconfig /nosdkpath /r:%WINDIR%\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll /pathmap:%~dp0=/ /checksumalgorithm:sha256 Documents.cs


