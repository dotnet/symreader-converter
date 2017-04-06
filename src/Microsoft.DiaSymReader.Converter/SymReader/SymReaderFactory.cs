// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader
{
    internal static class SymReaderFactory
    {
        private const string SymWriterClsid = "0AE2DEB0-F901-478b-BB9F-881EE8066788";

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymWriter")]
        private extern static void CreateSymWriter32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.SafeDirectories)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymWriter")]
        private extern static void CreateSymWriter64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        internal static bool IsPortable(Stream pdbStream)
        {
            pdbStream.Position = 0;

            bool isPortable;
            isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            return isPortable;
        }

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream)
        {
            return CreateWindowsPdbReader(pdbStream, new SymReaderMetadataImport(null, null));
        }

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream, PEReader peReader)
        {
            return CreateWindowsPdbReader(pdbStream, new SymReaderMetadataImport(peReader.GetMetadataReader(), peReader));
        }

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream, MetadataReader metadataReader, IDisposable metadataOwner)
        {
            return CreateWindowsPdbReader(pdbStream, new SymReaderMetadataImport(metadataReader, metadataOwner));
        }

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream, object metadataImporter)
        {
            object symReader = null;

            var guid = default(Guid);
            if (IntPtr.Size == 4)
            {
                CreateSymReader32(ref guid, out symReader);
            }
            else
            {
                CreateSymReader64(ref guid, out symReader);
            }

            var reader = (ISymUnmanagedReader5)symReader;
            reader.Initialize(pdbStream, metadataImporter);
            return reader;
        }

        public static ISymUnmanagedWriter8 CreateWindowsPdbWriter(object pdbStream, object metadataProvider)
        {
            object symWriter = null;
            var guid = new Guid(SymWriterClsid);
            if (IntPtr.Size == 4)
            {
                CreateSymWriter32(ref guid, out symWriter);
            }
            else
            {
                CreateSymWriter64(ref guid, out symWriter);
            }

            var writer = (ISymUnmanagedWriter8)symWriter;
            writer.InitializeDeterministic(metadataProvider, pdbStream);
            return writer;
        }
    }
}
