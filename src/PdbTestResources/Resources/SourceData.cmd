csc /target:library /debug:full /optimize- /deterministic /out:SourceData.dll Documents.cs
%1\pdbstr -w -p:SourceLink.pdb -i:SourceData.srcsrv.txt -s:srcsrv