﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/sorucelink", "/srcsvrvar", "x=y" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/srcsvrvar" }));
            Assert.Throws<InvalidDataException>(() => Pdb2Pdb.ParseArgs(new[] { "a.dll", "/srcsvrvar", "0=y" }));
        }

        [Fact]
        public void ParseArgs()
        {
            var args = Pdb2Pdb.ParseArgs(new[] { "a.dll" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("a.pdb2", args.OutPdbFilePathOpt);
            Assert.False(args.Extract);
            Assert.False(args.Verbose);
            Assert.False(args.Options.SuppressSourceLinkConversion);
            Assert.Empty(args.Options.SrcSvrVariables);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/extract" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Null(args.OutPdbFilePathOpt);
            Assert.True(args.Extract);
            Assert.False(args.Verbose);
            Assert.False(args.Options.SuppressSourceLinkConversion);
            Assert.Empty(args.Options.SrcSvrVariables);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/extract", "/out", "b.pdb" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("b.pdb", args.OutPdbFilePathOpt);
            Assert.True(args.Extract);
            Assert.False(args.Verbose);
            Assert.False(args.Options.SuppressSourceLinkConversion);
            Assert.Empty(args.Options.SrcSvrVariables);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/sourcelink", "/pdb", "b.pdb", "/out", "c.pdb" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Equal("b.pdb", args.PdbFilePathOpt);
            Assert.Equal("c.pdb", args.OutPdbFilePathOpt);
            Assert.False(args.Extract);
            Assert.False(args.Verbose);
            Assert.True(args.Options.SuppressSourceLinkConversion);
            Assert.Empty(args.Options.SrcSvrVariables);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/out", "c.pdb", "/verbose" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("c.pdb", args.OutPdbFilePathOpt);
            Assert.False(args.Extract);
            Assert.True(args.Verbose);
            Assert.False(args.Options.SuppressSourceLinkConversion);
            Assert.Empty(args.Options.SrcSvrVariables);

            args = Pdb2Pdb.ParseArgs(new[] { "a.dll", "/out", "c.pdb", "/verbose", "/srcsvrvar", "a=b", "/srcsvrvar", "c=d" });
            Assert.Equal("a.dll", args.PEFilePath);
            Assert.Null(args.PdbFilePathOpt);
            Assert.Equal("c.pdb", args.OutPdbFilePathOpt);
            Assert.False(args.Extract);
            Assert.True(args.Verbose);
            Assert.False(args.Options.SuppressSourceLinkConversion);
            Assert.Equal(new[] { new KeyValuePair<string, string>("a", "b"), new KeyValuePair<string, string>("c", "d") }, args.Options.SrcSvrVariables);
        }

        [Fact]
        public void EndToEnd_PortableToWindows_ExplicitPath_SourceLinkConversion()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.dll").WriteAllBytes(TestResources.SourceLink.PortableDll);
            var pdb = dir.CreateFile("SourceLink.x.pdb").WriteAllBytes(TestResources.SourceLink.PortablePdb);
            var outPdbPath = Path.Combine(dir.Path, "SourceLink.pdb2");

            Assert.True(Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: pdb.Path,
                outPdbFilePathOpt: outPdbPath,
                options: PortablePdbConversionOptions.Default,
                extract: false,
                verbose: false)));

            using (var peStream = File.OpenRead(pe.Path))
            using (var pdbStream = File.OpenRead(outPdbPath))
            {
                var symReader = SymReaderHelpers.CreateWindowsPdbReader(pdbStream);
                Assert.Null(symReader.GetSourceLinkData());

                string actual = symReader.GetSourceServerData();
                AssertEx.AssertLinesEqual(
@"SRCSRV: ini ------------------------------------------------
VERSION=2
SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRVVERCTRL=http
SRCSRVTRG=%RAWURL%
SRCSRV: source files ---------------------------------------
C:\Documents.cs*3/Documents.cs.g
C:\a\b\c\d\1.cs*1/a/b/c/d/1.cs
C:\a\b\c\D\2.cs*1/a/b/c/D/2.cs
C:\a\b\C\d\3.cs*1/a/b/C/d/3.cs
C:\a\b\c\d\x.cs*1/a/b/c/d/x.cs
C:\A\b\c\x.cs*1/a/b/c/x.cs
C:\a\b\x.cs*1/a/b/x.cs
C:\a\B\3.cs*1/a/B/3.cs
C:\a\B\c\4.cs*1/a/B/c/4.cs
:6.cs*4/:6.cs
C:\a\b\X.cs*1/a/b/X.cs
C:\a\B\x.cs*1/a/B/x.cs
SRCSRV: end ------------------------------------------------", actual);
            }
        }

        [Fact]
        public void EndToEnd_PortableToWindows_ImplicitPath_SuppressSourceLinkConversion()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.dll").WriteAllBytes(TestResources.SourceLink.PortableDll);
            dir.CreateFile("SourceLink.pdb").WriteAllBytes(TestResources.SourceLink.PortablePdb);
            var outPdb = dir.CreateFile("SourceLink.converted.pdb").WriteAllText("dummy");

            Assert.True(Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: null,
                outPdbFilePathOpt: outPdb.Path,
                options: new PortablePdbConversionOptions(suppressSourceLinkConversion: true),
                extract: false,
                verbose: false)));

            using (var peStream = File.OpenRead(pe.Path))
            using (var pdbStream = File.OpenRead(outPdb.Path))
            {
                var symReader = SymReaderHelpers.CreateWindowsPdbReader(pdbStream);
                AssertEx.Equal(TestResources.SourceLink.SourceLinkJson, symReader.GetRawSourceLinkData());
                Assert.Null(symReader.GetSourceServerData());
            }
        }

        [Fact]
        public void EndToEnd_EmbeddedToWindows_SuppressSourceLinkConversion()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.Embedded.dll").WriteAllBytes(TestResources.SourceLink.EmbeddedDll);
            var outPdb = dir.CreateFile("SourceLink.converted.pdb").WriteAllText("dummy");

            Assert.True(Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: null,
                outPdbFilePathOpt: outPdb.Path,
                options: new PortablePdbConversionOptions(suppressSourceLinkConversion: true),
                extract: false,
                verbose: false)));

            using (var peStream = File.OpenRead(pe.Path))
            using (var pdbStream = File.OpenRead(outPdb.Path))
            {
                var symReader = SymReaderHelpers.CreateWindowsPdbReader(pdbStream);
                AssertEx.Equal(TestResources.SourceLink.SourceLinkJson, symReader.GetRawSourceLinkData());
                Assert.Null(symReader.GetSourceServerData());
            }
        }

        [Fact]
        public void EndToEnd_EmbeddedToWindows_Extraction()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.Embedded.dll").WriteAllBytes(TestResources.SourceLink.EmbeddedDll);
            var outPdb = dir.CreateFile("SourceLink.Embedded.pdb").WriteAllText("dummy");

            Assert.True(Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: null,
                outPdbFilePathOpt: null,
                options: PortablePdbConversionOptions.Default,
                extract: true,
                verbose: false)));

            AssertEx.Equal(TestResources.SourceLink.PortablePdb, File.ReadAllBytes(outPdb.Path));
        }

        [Fact]
        public void EndToEnd_EmbeddedToWindows_Extraction_OutPath()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceLink.Embedded.dll").WriteAllBytes(TestResources.SourceLink.EmbeddedDll);
            var outPdb = dir.CreateFile("SourceLink.extracted.pdb").WriteAllText("dummy");

            Assert.True(Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: null,
                outPdbFilePathOpt: outPdb.Path,
                options: PortablePdbConversionOptions.Default,
                extract: true,
                verbose: false)));

            AssertEx.Equal(TestResources.SourceLink.PortablePdb, File.ReadAllBytes(outPdb.Path));
        }


        [Fact]
        public void EndToEnd_Extraction_Error()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceData.dll").WriteAllBytes(TestResources.SourceData.WindowsDll);
            var outPdb = dir.CreateFile("SourceLink.extracted.pdb").WriteAllText("dummy");

            Assert.Throws<IOException>(() =>
                Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                    peFilePath: pe.Path,
                    pdbFilePathOpt: null,
                    outPdbFilePathOpt: outPdb.Path,
                    options: PortablePdbConversionOptions.Default,
                    extract: true,
                    verbose: false)));

            Assert.Equal("dummy", outPdb.ReadAllText());
        }

        [Fact]
        public void EndToEnd_WindowsToPortable_ImplicitPath()
        {
            var dir = _temp.CreateDirectory();
            var pe = dir.CreateFile("SourceData.dll").WriteAllBytes(TestResources.SourceData.WindowsDll);
            dir.CreateFile("SourceData.pdb").WriteAllBytes(TestResources.SourceData.WindowsPdb);
            var outPdb = dir.CreateFile("SourceLink.pdb").WriteAllText("dummy");

            Assert.True(Pdb2Pdb.Convert(new Pdb2Pdb.Args(
                peFilePath: pe.Path,
                pdbFilePathOpt: null,
                outPdbFilePathOpt: outPdb.Path,
                options: PortablePdbConversionOptions.Default,
                extract: false,
                verbose: false)));

            using (var provider = MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(outPdb.Path)))
            {
                var sourceLinkCdiGuid = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

                var mdReader = provider.GetMetadataReader();
                var sourceLink = from cdiHandle in mdReader.CustomDebugInformation
                                 let cdi = mdReader.GetCustomDebugInformation(cdiHandle)
                                 where mdReader.GetGuid(cdi.Kind) == sourceLinkCdiGuid
                                 select Encoding.UTF8.GetString(mdReader.GetBlobBytes(cdi.Value));

                AssertEx.AssertLinesEqual(@"
{
""documents"": {
    ""C:\Documents.cs"": ""http://server/3/Documents.cs.g"",
    ""C:\a\b\c\d\1.cs"": ""http://server/1/a/b/c/d/1.cs"",
    ""C:\a\b\c\D\2.cs"": ""http://server/1/a/b/c/D/2.cs"",
    ""C:\a\b\C\d\3.cs"": ""http://server/1/a/b/C/d/3.cs"",
    ""C:\a\b\c\d\x.cs"": ""http://server/1/a/b/c/d/x.cs"",
    ""C:\A\b\c\x.cs"": ""http://server/1/a/b/c/x.cs"",
    ""C:\a\b\x.cs"": ""http://server/1/a/b/x.cs"",
    ""C:\a\B\3.cs"": ""http://server/1/a/B/3.cs"",
    ""C:\a\B\c\4.cs"": ""http://server/1/a/B/c/4.cs"",
    "":6.cs"": ""http://server/4/:6.cs"",
    ""C:\a\b\X.cs"": ""http://server/1/a/b/X.cs"",
    ""C:\a\B\x.cs"": ""http://server/1/a/B/x.cs""
  }
}
", sourceLink.Single());
            }
        }
    }
}
