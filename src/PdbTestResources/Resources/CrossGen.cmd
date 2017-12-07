set ASSEMBLIES_PATH=%ProgramFiles(x86)%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1

csc /target:library /debug:portable /optimize- /deterministic CrossGen.cs
C:\ssd\coreclr\bin\Product\Windows_NT.x64.Debug\crossgen.exe /Platform_Assemblies_Paths "%ASSEMBLIES_PATH%" /out CrossGen.ni.dllx CrossGen.dll
copy /y CrossGen.pdb CrossGen.pdbx
copy /y CrossGen.dll CrossGen.dllx

csc /target:library /debug+ /optimize- /deterministic CrossGen.cs
C:\ssd\coreclr\bin\Product\Windows_NT.x64.Debug\crossgen.exe /Platform_Assemblies_Paths "%ASSEMBLIES_PATH%" /out CrossGen.ni.dll CrossGen.dll
