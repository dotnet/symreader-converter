csc /target:library /debug:portable /optimize- /deterministic /r:"%USERPROFILE%\.nuget\packages\system.valuetuple\4.3.0\lib\portable-net40+sl4+win8+wp8\System.ValueTuple.dll" LanguageOnlyTypes.cs
copy /y LanguageOnlyTypes.pdb LanguageOnlyTypes.pdbx
copy /y LanguageOnlyTypes.dll LanguageOnlyTypes.dllx

csc /target:library /debug+ /optimize- /deterministic /r:"%USERPROFILE%\.nuget\packages\system.valuetuple\4.3.0\lib\portable-net40+sl4+win8+wp8\System.ValueTuple.dll" LanguageOnlyTypes.cs