// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal static class MetadataUtilities
    {
        public const int MaxCompressedIntegerValue = 0x1fffffff;
        public const SignatureTypeCode SignatureTypeCode_ValueType = (SignatureTypeCode)0x11;
        public const SignatureTypeCode SignatureTypeCode_Class = (SignatureTypeCode)0x12;
        public static int MethodDefToken(int rowId) => 0x06000000 | rowId;
        public static int GetRowId(int token) => token & 0xffffff;
        public static bool IsMethodToken(int token) => unchecked((uint)token) >> 24 == 0x06;

        // Custom Attribute kinds:
        public static readonly Guid MethodSteppingInformationBlobId = new Guid("54FD2AC5-E925-401A-9C2A-F94F171072F8");
        public static readonly Guid VbDefaultNamespaceId = new Guid("58b2eab6-209f-4e4e-a22c-b2d0f910c782");
        public static readonly Guid EmbeddedSourceId = new Guid("0E8A571B-6926-466E-B4AD-8AB04611F5FE");
        public static readonly Guid SourceLinkId = new Guid("CC110556-A091-4D38-9FEC-25AB9A351A6A");

        internal static int GetTypeDefOrRefOrSpecCodedIndex(EntityHandle typeHandle)
        {
            int tag = 0;
            switch (typeHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    tag = 0;
                    break;

                case HandleKind.TypeReference:
                    tag = 1;
                    break;

                case HandleKind.TypeSpecification:
                    tag = 2;
                    break;
            }

            return (MetadataTokens.GetRowNumber(typeHandle) << 2) | tag;
        }
        
        // Doesn't handle nested types.
        public static string? GetQualifiedTypeName(this MetadataReader reader, EntityHandle typeDefOrRef)
        {
            string? qualifiedName;
            if (typeDefOrRef.Kind == HandleKind.TypeDefinition)
            {
                var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)typeDefOrRef);
                if (typeDef.Namespace.IsNil)
                {
                    return reader.GetString(typeDef.Name);
                }
                else
                {
                    return reader.GetString(typeDef.Namespace) + "." + reader.GetString(typeDef.Name);
                }

            }
            else if (typeDefOrRef.Kind == HandleKind.TypeReference)
            {
                var typeRef = reader.GetTypeReference((TypeReferenceHandle)typeDefOrRef);
                if (typeRef.Namespace.IsNil)
                {
                    return reader.GetString(typeRef.Name);
                }
                else
                {
                    return reader.GetString(typeRef.Namespace) + "." + reader.GetString(typeRef.Name);
                }
            }
            else
            {
                qualifiedName = null;
            }

            return qualifiedName;
        }

        internal static BlobHandle GetCustomDebugInformation(this MetadataReader reader, EntityHandle parent, Guid kind)
        {
            foreach (var cdiHandle in reader.GetCustomDebugInformation(parent))
            {
                var cdi = reader.GetCustomDebugInformation(cdiHandle);
                if (reader.GetGuid(cdi.Kind) == kind)
                {
                    // return the first record
                    return cdi.Value;
                }
            }

            return default;
        }

        internal static string? GetVisualBasicDefaultNamespace(MetadataReader reader)
        {
            foreach (var cdiHandle in reader.GetCustomDebugInformation(Handle.ModuleDefinition))
            {
                var cdi = reader.GetCustomDebugInformation(cdiHandle);
                if (reader.GetGuid(cdi.Kind) == VbDefaultNamespaceId)
                {
                    return reader.GetStringUTF8(cdi.Value);
                }
            }

            return null;
        }

        internal static string GetStringUTF8(this MetadataReader reader, BlobHandle handle)
        {
            var bytes = reader.GetBlobBytes(handle);
            return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
        }

        // Copied from Roslyn EE. Share Portable PDB blob decoders.

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        public static ImmutableArray<StateMachineHoistedLocalScope> DecodeHoistedLocalScopes(BlobReader reader)
        {
            var result = ArrayBuilder<StateMachineHoistedLocalScope>.GetInstance();

            do
            {
                int startOffset = reader.ReadInt32();
                int length = reader.ReadInt32();

                result.Add(new StateMachineHoistedLocalScope(startOffset, startOffset + length));
            }
            while (reader.RemainingBytes > 0);

            return result.ToImmutableAndFree();
        }

        // Copied from Roslyn EE. Share Portable PDB blob decoders.

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        public static ImmutableArray<bool> ReadDynamicCustomDebugInformation(MetadataReader reader, EntityHandle variableOrConstantHandle)
        {
            var blobHandle = GetCustomDebugInformation(reader, variableOrConstantHandle, PortableCustomDebugInfoKinds.DynamicLocalVariables);
            if (blobHandle.IsNil)
            {
                return default;
            }

            return DecodeDynamicFlags(reader.GetBlobReader(blobHandle));
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<bool> DecodeDynamicFlags(BlobReader reader)
        {
            var builder = ImmutableArray.CreateBuilder<bool>(reader.Length * 8);

            while (reader.RemainingBytes > 0)
            {
                int b = reader.ReadByte();
                for (int i = 1; i < 0x100; i <<= 1)
                {
                    builder.Add((b & i) != 0);
                }
            }

            return builder.MoveToImmutable();
        }

        // Copied from Roslyn EE. Share Portable PDB blob decoders.
       
        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        public static ImmutableArray<string?> ReadTupleCustomDebugInformation(MetadataReader reader, EntityHandle variableOrConstantHandle)
        {
            var blobHandle = GetCustomDebugInformation(reader, variableOrConstantHandle, PortableCustomDebugInfoKinds.TupleElementNames);
            if (blobHandle.IsNil)
            {
                return default;
            }

            return DecodeTupleElementNames(reader.GetBlobReader(blobHandle));
        }

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private static ImmutableArray<string?> DecodeTupleElementNames(BlobReader reader)
        {
            var builder = ArrayBuilder<string?>.GetInstance();
            while (reader.RemainingBytes > 0)
            {
                int byteCount = reader.IndexOf(0);
                string value = reader.ReadUTF8(byteCount);
                byte terminator = reader.ReadByte();
                Debug.Assert(terminator == 0);
                builder.Add(value.Length == 0 ? null : value);
            }

            return builder.ToImmutableAndFree();
        }

        // TODO: Copied from PortablePdb. Share.

        public static AsyncMethodData ReadAsyncMethodData(MetadataReader metadataReader, MethodDebugInformationHandle debugHandle)
        {
            var reader = metadataReader;
            var body = reader.GetMethodDebugInformation(debugHandle);
            var kickoffMethod = body.GetStateMachineKickoffMethod();

            if (kickoffMethod.IsNil)
            {
                return AsyncMethodData.None;
            }

            var value = reader.GetCustomDebugInformation(debugHandle.ToDefinitionHandle(), MethodSteppingInformationBlobId);
            if (value.IsNil)
            {
                return AsyncMethodData.None;
            }

            var blobReader = reader.GetBlobReader(value);

            long catchHandlerOffset = blobReader.ReadUInt32();
            if (catchHandlerOffset > (uint)int.MaxValue + 1)
            {
                throw new BadImageFormatException();
            }

            var yieldOffsets = ImmutableArray.CreateBuilder<int>();
            var resultOffsets = ImmutableArray.CreateBuilder<int>();
            var resumeMethods = ImmutableArray.CreateBuilder<int>();

            while (blobReader.RemainingBytes > 0)
            {
                uint yieldOffset = blobReader.ReadUInt32();
                if (yieldOffset > int.MaxValue)
                {
                    throw new BadImageFormatException();
                }

                uint resultOffset = blobReader.ReadUInt32();
                if (resultOffset > int.MaxValue)
                {
                    throw new BadImageFormatException();
                }

                yieldOffsets.Add((int)yieldOffset);
                resultOffsets.Add((int)resultOffset);
                resumeMethods.Add(MethodDefToken(blobReader.ReadCompressedInteger()));
            }

            return new AsyncMethodData(
                kickoffMethod,
                (int)(catchHandlerOffset - 1),
                yieldOffsets.ToImmutable(),
                resultOffsets.ToImmutable(),
                resumeMethods.ToImmutable());
        }

        // Copied from Roslyn: MetadataWriter.SerializeBitVector
        internal static void SerializeBitVector(BlobBuilder builder, ImmutableArray<bool> flags)
        {
            int c = flags.Length - 1;
            while (!flags[c])
            {
                c--;
            }

            int b = 0;
            int shift = 0;
            for (int i = 0; i <= c; i++)
            {
                if (flags[i])
                {
                    b |= 1 << shift;
                }

                if (shift == 7)
                {
                    builder.WriteByte((byte)b);
                    b = 0;
                    shift = 0;
                }
                else
                {
                    shift++;
                }
            }

            if (b != 0)
            {
                builder.WriteByte((byte)b);
            }
        }

        // Copied from Roslyn: MetadataWriter.SerializeTupleElementNames
        internal static void SerializeTupleElementNames(BlobBuilder builder, ImmutableArray<string> names)
        {
            foreach (var name in names)
            {
                if (name != null)
                {
                    builder.WriteUTF8(name);
                }

                builder.WriteByte(0);
            }
        }
    }
}