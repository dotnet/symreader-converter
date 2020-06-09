csc /target:library /debug:portable /optimize- /noconfig /deterministic /pathmap:%~dp0=/_/ LanguageOnlyTypes.cs System.cs
copy /y LanguageOnlyTypes.pdb LanguageOnlyTypes.pdbx
copy /y LanguageOnlyTypes.dll LanguageOnlyTypes.dllx

csc /target:library /debug+ /optimize- /noconfig /deterministic /pathmap:%~dp0=/_/ LanguageOnlyTypes.cs System.cs