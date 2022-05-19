// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.CodeAnalysis.Debugging;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class SymReaderHelpers
    {
        internal static readonly Guid EncStateMachineSuspensionPoints = new("8B78CD68-2EDE-420B-980B-E15884B8AAA3");
        internal static readonly Guid VisualBasicLanguageGuid = new("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
        internal const CustomDebugInfoKind CustomDebugInfoKind_EncStateMachineSuspensionPoints = (CustomDebugInfoKind)9;

        internal static bool IsPortable(Stream pdbStream)
        {
            pdbStream.Position = 0;

            bool isPortable;
            isPortable = pdbStream.ReadByte() == 'B' && pdbStream.ReadByte() == 'S' && pdbStream.ReadByte() == 'J' && pdbStream.ReadByte() == 'B';
            pdbStream.Position = 0;

            return isPortable;
        }

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream, SymUnmanagedReaderCreationOptions options)
            => SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, DummySymReaderMetadataProvider.Instance, options);

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream, PEReader peReader, SymUnmanagedReaderCreationOptions options)
            => CreateWindowsPdbReader(pdbStream, peReader.GetMetadataReader(), options);

        public static ISymUnmanagedReader5 CreateWindowsPdbReader(Stream pdbStream, MetadataReader metadataReader, SymUnmanagedReaderCreationOptions options)
            => SymUnmanagedReaderFactory.CreateReader<ISymUnmanagedReader5>(pdbStream, new SymMetadataProvider(metadataReader), options);

        public static ImmutableArray<string> GetImportStrings(ISymUnmanagedReader reader, int methodToken, int methodVersion)
        {
            var method = reader.GetMethodByVersion(methodToken, methodVersion);
            if (method == null)
            {
                // In rare circumstances (only bad PDBs?) GetMethodByVersion can return null.
                // If there's no debug info for the method, then no import strings are available.
                return ImmutableArray<string>.Empty;
            }

            ISymUnmanagedScope rootScope = method.GetRootScope();
            if (rootScope == null)
            {
                // TODO: report warning?
                return ImmutableArray<string>.Empty;
            }

            var childScopes = rootScope.GetChildren();
            if (childScopes.Length == 0)
            {
                // It seems like there should always be at least one child scope, but we've
                // seen PDBs where that is not the case.
                return ImmutableArray<string>.Empty;
            }

            // As in NamespaceListWrapper::Init, we only consider namespaces in the first
            // child of the root scope.
            ISymUnmanagedScope firstChildScope = childScopes[0];

            var namespaces = firstChildScope.GetNamespaces();
            if (namespaces.Length == 0)
            {
                // It seems like there should always be at least one namespace (i.e. the global
                // namespace), but we've seen PDBs where that is not the case.
                return ImmutableArray<string>.Empty;
            }

            return ImmutableArray.CreateRange(namespaces.Select(n => n.GetName()));
        }

        public static bool TryReadPdbId(PEReader peReader, out BlobContentId id, out int age)
        {
            var codeViewEntry = peReader.ReadDebugDirectory().LastOrDefault(entry => entry.Type == DebugDirectoryEntryType.CodeView);
            if (codeViewEntry.DataSize == 0)
            {
                id = default;
                age = 0;
                return false;
            }

            var codeViewData = peReader.ReadCodeViewDebugDirectoryData(codeViewEntry);

            id = new BlobContentId(codeViewData.Guid, codeViewEntry.Stamp);
            age = codeViewData.Age;
            return true;
        }

        public static void GetWindowsPdbSignature(ImmutableArray<byte> bytes, out Guid guid, out uint timestamp, out int age)
        {
            var guidBytes = new byte[16];
            bytes.CopyTo(0, guidBytes, 0, guidBytes.Length);
            guid = new Guid(guidBytes);

            int n = guidBytes.Length;
            timestamp = ((uint)bytes[n + 3] << 24) | ((uint)bytes[n + 2] << 16) | ((uint)bytes[n + 1] << 8) | bytes[n];
            age = 1;
        }

        private unsafe static byte[] GetBytes(byte* data, int size)
        {
            var buffer = new byte[size];
            Marshal.Copy((IntPtr)data, buffer, 0, buffer.Length);
            return buffer;
        }

        private unsafe static string GetString(byte* data, int size) =>
#if NET45
            new string((sbyte*)data, 0, size, Encoding.UTF8);
#else
            Encoding.UTF8.GetString(data, size);
#endif

        public unsafe static string? GetSourceLinkData(this ISymUnmanagedReader5 reader) => 
            TryGetSourceLinkData(reader, out byte* data, out int size) ? GetString(data, size) : null;

        public unsafe static byte[]? GetRawSourceLinkData(this ISymUnmanagedReader5 reader) =>
            TryGetSourceLinkData(reader, out byte* data, out int size) ? GetBytes(data, size) : null;

        private unsafe static bool TryGetSourceLinkData(ISymUnmanagedReader5 reader, out byte* data, out int size)
        {
            int hr = reader.GetSourceServerData(out data, out size);
            Marshal.ThrowExceptionForHR(hr);
            return hr != HResult.S_FALSE;
        }

        public unsafe static byte[]? GetRawSourceServerData(this ISymUnmanagedReader reader)
        {
            if (!(reader is ISymUnmanagedSourceServerModule srcsrv))
            {
                return null;
            }

            int size;
            byte* data = null;
            try
            {
                return (srcsrv.GetSourceServerData(out size, out data) == HResult.S_OK) ? 
                    GetBytes(data, size) : null;
            }
            finally
            {
                if (data != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)data);
                }
            }
        }

        public unsafe static string? GetSourceServerData(this ISymUnmanagedReader reader)
        {
            if (!(reader is ISymUnmanagedSourceServerModule srcsrv))
            {
                return null;
            }

            int size;
            byte* data = null;
            try
            {
                return (srcsrv.GetSourceServerData(out size, out data) == HResult.S_OK) ? 
                    GetString(data, size) : null;
            }
            finally
            {
                if (data != null)
                {
                    Marshal.FreeCoTaskMem((IntPtr)data);
                }
            }
        }

        public static byte[]? GetRawEmbeddedSource(this ISymUnmanagedDocument document)
        {
            Marshal.ThrowExceptionForHR(document.GetSourceLength(out int length));
            if (length == 0)
            {
                return null;
            }

            if (length < sizeof(int))
            {
                throw new InvalidDataException();
            }

            var sourceBlob = new byte[length];
            Marshal.ThrowExceptionForHR(document.GetSourceRange(0, 0, int.MaxValue, int.MaxValue, length, out int bytesRead, sourceBlob));
            if (bytesRead < sizeof(int) || bytesRead > sourceBlob.Length)
            {
                throw new InvalidDataException();
            }

            return sourceBlob;
        }
    }
}
