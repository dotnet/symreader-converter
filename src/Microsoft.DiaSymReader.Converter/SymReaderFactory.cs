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

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymReader")]
        private extern static void CreateSymReader64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symReader);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.x86.dll", EntryPoint = "CreateSymWriter")]
        private extern static void CreateSymWriter32(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        [DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory)]
        [DllImport("Microsoft.DiaSymReader.Native.amd64.dll", EntryPoint = "CreateSymWriter")]
        private extern static void CreateSymWriter64(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)]out object symWriter);

        public static ISymUnmanagedReader3 CreateWindowsPdbReader(Stream pdbStream, PEReader peReader)
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

            var reader = (ISymUnmanagedReader3)symReader;
            reader.Initialize(pdbStream, new SymReaderMetadataImport(peReader.GetMetadataReader(), peReader));
            return reader;
        }

        public static ISymUnmanagedWriter7 CreateWindowsPdbWriter(object pdbStream, object metadataProvider)
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

            var writer = (ISymUnmanagedWriter7)symWriter;
            writer.InitializeDeterministic(metadataProvider, pdbStream);
            return writer;
        }
    }
}
