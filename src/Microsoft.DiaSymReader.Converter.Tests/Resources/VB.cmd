set TUPLE=%USERPROFILE%\.nuget\packages\system.valuetuple\4.3.0\lib\portable-net40+sl4+win8+wp8\System.ValueTuple.dll

vbc /target:library /debug:portable /optimize- /deterministic /r:"%TUPLE%" @VB.rsp VB.vb
copy /y VB.pdb VB.pdbx
copy /y VB.dll VB.dllx

vbc /target:library /debug+ /optimize- /deterministic /r:"%TUPLE%" @VB.rsp VB.vb