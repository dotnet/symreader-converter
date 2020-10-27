 // Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

 using Microsoft.DiaSymReader.Tools.UnitTests;
using System;
using System.IO;

namespace TestResources
{
    public static class Documents
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Documents) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Documents) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(Documents) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(Documents) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class Scopes
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Scopes) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Scopes) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(Scopes) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(Scopes) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class LanguageOnlyTypes
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(LanguageOnlyTypes) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(LanguageOnlyTypes) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(LanguageOnlyTypes) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(LanguageOnlyTypes) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class VB
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(VB) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(VB) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(VB) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(VB) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class Async
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Async) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Async) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(Async) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(Async) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class Iterator
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Iterator) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Iterator) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(Iterator) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(Iterator) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class Imports
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Imports) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Imports) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(Imports) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(Imports) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class MethodBoundaries
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(MethodBoundaries) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(MethodBoundaries) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(MethodBoundaries) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(MethodBoundaries) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class EmbeddedSource
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(EmbeddedSource) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(EmbeddedSource) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(EmbeddedSource) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(EmbeddedSource) + ".pdb");

        private static byte[]? s_cs;
        public static byte[] CS => ResourceLoader.GetOrCreateResource(ref s_cs, nameof(EmbeddedSource) + ".cs");

        private static byte[]? s_csSmall;
        public static byte[] CSSmall => ResourceLoader.GetOrCreateResource(ref s_csSmall, nameof(EmbeddedSource) + "Small.cs");

        private static byte[]? s_csNoCode;
        public static byte[] CSNoCode => ResourceLoader.GetOrCreateResource(ref s_csNoCode, nameof(EmbeddedSource) + "NoCode.cs");

        private static byte[]? s_csNoSequencePoints;
        public static byte[] CSNoSequencePoints => ResourceLoader.GetOrCreateResource(ref s_csNoSequencePoints, nameof(EmbeddedSource) + "NoSequencePoints.cs");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class SourceLink
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(SourceLink) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(SourceLink) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(SourceLink) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(SourceLink) + ".pdb");

        private static byte[]? s_embeddedDll;
        public static byte[] EmbeddedDll => ResourceLoader.GetOrCreateResource(ref s_embeddedDll, nameof(SourceLink) + ".Embedded.dll");

        private static byte[]? s_sourceLinkJson;
        public static byte[] SourceLinkJson => ResourceLoader.GetOrCreateResource(ref s_sourceLinkJson, nameof(SourceLink) + ".json");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class SourceData
    {
        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(SourceData) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(SourceData) + ".pdb");

        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
    }

    public static class Misc
    {
        private static byte[]? s_portableDll;
        public static byte[] PortableDll => ResourceLoader.GetOrCreateResource(ref s_portableDll, nameof(Misc) + ".dllx");

        private static byte[]? s_portablePdb;
        public static byte[] PortablePdb => ResourceLoader.GetOrCreateResource(ref s_portablePdb, nameof(Misc) + ".pdbx");

        private static byte[]? s_windowsDll;
        public static byte[] WindowsDll => ResourceLoader.GetOrCreateResource(ref s_windowsDll, nameof(Misc) + ".dll");

        private static byte[]? s_windowsPdb;
        public static byte[] WindowsPdb => ResourceLoader.GetOrCreateResource(ref s_windowsPdb, nameof(Misc) + ".pdb");

        public static TestResource PortableDllAndPdb => new TestResource(PortableDll, PortablePdb);
        public static TestResource WindowsDllAndPdb => new TestResource(WindowsDll, WindowsPdb);
        public static TestResource DllAndPdb(bool portable) => portable ? PortableDllAndPdb : WindowsDllAndPdb;
    }

    public static class CrossGen
    {
        public static Stream PortableDll => ResourceLoader.GetResourceStream(nameof(CrossGen) + ".dllx");

        public static Guid PortableDllDebugDirectoryEntryGuid => new Guid("3FE3F61A-BC73-4489-A785-F180214B76D1");

        public static Stream WindowsDll => ResourceLoader.GetResourceStream(nameof(CrossGen) + ".dll");

        public static Guid WindowsDllDebugDirectoryEntryGuid => new Guid("4448E80E-2F97-40AF-BBCE-86A3D66A1A20");
    }
}
