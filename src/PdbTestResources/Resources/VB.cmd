set TUPLE=%USERPROFILE%\.nuget\packages\system.valuetuple\4.4.0\lib\net461\System.ValueTuple.dll
set SYSTEM=%PROGRAM_FILES_32%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\System.dll
set CORLIB=%PROGRAM_FILES_32%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\mscorlib.dll

vbc /target:library /debug:portable /optimize- /deterministic /nostdlib /noconfig /r:"%SYSTEM%" /r:"%CORLIB%" /r:"%TUPLE%" @VB.rsp VB.vb
copy /y VB.pdb VB.pdbx
copy /y VB.dll VB.dllx

vbc /target:library /debug+ /optimize- /deterministic /nostdlib /noconfig /r:"%SYSTEM%" /r:"%CORLIB%" /r:"%TUPLE%" @VB.rsp VB.vb