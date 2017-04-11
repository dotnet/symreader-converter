// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public class Pdb2PdbTests
    {
        private readonly TempRoot _temp = new TempRoot();

        [Fact]
        public void ParseArgs_Errors()
        {
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new string[0]));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/abc" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "/extract" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/pdb" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/out" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "b.dll" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/extract", "/sourcelink" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/extract", "/pdb", "a.pdb" }));            
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { ">:<.dll" }));
        }

        [Fact]
        public void ParseArgs()
        {
            var args = Pdb2Pdb.ParseArgs(new[] { "a.dll" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("a.pdb2", args.OutPdbFilePath);
            Assert.False(args.Extract);
            Assert.Equal(PdbConversionOptions.Default, args.Options);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/extract", "/out", "b.pdb" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("b.pdb", args.OutPdbFilePath);
            Assert.True(args.Extract);
            Assert.Equal(PdbConversionOptions.Default, args.Options);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/sourcelink", "/pdb", "b.pdb", "/out", "c.pdb" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Equal("b.pdb", args.PdbFilePathOpt);
            Assert.Equal("c.pdb", args.OutPdbFilePath);
            Assert.False(args.Extract);
            Assert.Equal(PdbConversionOptions.SuppressSourceLinkConversion, args.Options);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/out", "c.pdb" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("c.pdb", args.OutPdbFilePath);
            Assert.False(args.Extract);
            Assert.Equal(PdbConversionOptions.Default, args.Options);
        }

        [Fact]
        public void EndToEnd1()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.dll").WriteAllBytes(TestResources.SourceLink.PortableDll);
            var pdb = dir.CreateFile("SourceLink.x.pdb").WriteAllBytes(TestResources.SourceLink.PortablePdb);
            var outPdbPath = Path.Combine(dir.Path, "SourceLink.pdb2");

            Assert.Equal(0, Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: pdb.Path,
                outPdbFilePath: outPdbPath,
                options: PdbConversionOptions.Default,
                extract: false)));

            using (var peStream = File.OpenRead(pe.Path))
            using (var pdbStream = File.OpenRead(outPdbPath))
            {
                var symReader = SymReaderFactory.CreateWindowsPdbReader(pdbStream);
                // TODO: symReader.GetSoruceServerData
            }
        }

        [Fact]
        public void EndToEnd2()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.dll").WriteAllBytes(TestResources.SourceLink.PortableDll);
            dir.CreateFile("SourceLink.pdb").WriteAllBytes(TestResources.SourceLink.PortablePdb);
            var outPdb = dir.CreateFile("Scopes.converted.pdb").WriteAllText("dummy");

            Assert.Equal(0, Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: null,
                outPdbFilePath: outPdb.Path,
                options: PdbConversionOptions.SuppressSourceLinkConversion,
                extract: false)));

            using (var peStream = File.OpenRead(pe.Path))
            using (var pdbStream = File.OpenRead(outPdb.Path))
            {
                var symReader = SymReaderFactory.CreateWindowsPdbReader(pdbStream);
                // TODO: symReader.GetSoruceServerData
            }
        }
    }
}
