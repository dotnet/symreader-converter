// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed class SymMetadataProvider : ISymWriterMetadataProvider, ISymReaderMetadataProvider
    {
        private readonly MetadataReader _reader;

        internal SymMetadataProvider(MetadataReader reader)
        {
            _reader = reader;
        }

        public unsafe bool TryGetStandaloneSignature(int standaloneSignatureToken, out byte* signature, out int length)
        {
            var sigHandle = (StandaloneSignatureHandle)MetadataTokens.Handle(standaloneSignatureToken);
            if (sigHandle.IsNil)
            {
                signature = null;
                length = 0;
                return false;
            }

            var sig = _reader.GetStandaloneSignature(sigHandle);
            var blobReader = _reader.GetBlobReader(sig.Signature);

            signature = blobReader.StartPointer;
            length = blobReader.Length;
            return true;
        }

        public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, [NotNullWhen(true)] out string? namespaceName, [NotNullWhen(true)] out string? typeName, out TypeAttributes attributes)
        {
            var handle = (TypeDefinitionHandle)MetadataTokens.Handle(typeDefinitionToken);
            if (handle.IsNil)
            {
                namespaceName = null;
                typeName = null;
                attributes = 0;
                return false;
            }

            var typeDefinition = _reader.GetTypeDefinition(handle);
            namespaceName = _reader.GetString(typeDefinition.Namespace);
            typeName = _reader.GetString(typeDefinition.Name);
            attributes = typeDefinition.Attributes;
            return true;
        }

        public bool TryGetTypeReferenceInfo(int typeReferenceToken, [NotNullWhen(true)] out string? namespaceName, [NotNullWhen(true)] out string? typeName)
        {
            var handle = (TypeReferenceHandle)MetadataTokens.Handle(typeReferenceToken);
            if (handle.IsNil)
            {
                namespaceName = null;
                typeName = null;
                return false;
            }

            var typeReference = _reader.GetTypeReference(handle);
            namespaceName = _reader.GetString(typeReference.Namespace);
            typeName = _reader.GetString(typeReference.Name);
            return true;
        }

        public bool TryGetEnclosingType(int nestedTypeToken, out int enclosingTypeToken)
        {
            var nestedTypeDef = _reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(nestedTypeToken));
            var declaringTypeHandle = nestedTypeDef.GetDeclaringType();

            if (declaringTypeHandle.IsNil)
            {
                enclosingTypeToken = 0;
                return false;
            }
            else
            {
                enclosingTypeToken = MetadataTokens.GetToken(declaringTypeHandle);
                return true;
            }
        }

        public bool TryGetMethodInfo(int methodDefinitionToken, [NotNullWhen(true)] out string? methodName, out int declaringTypeToken)
        {
            var handle = (MethodDefinitionHandle)MetadataTokens.Handle(methodDefinitionToken);
            if (handle.IsNil)
            {
                methodName = null;
                declaringTypeToken = 0;
                return false;
            }

            var methodDefinition = _reader.GetMethodDefinition(handle);
            methodName = _reader.GetString(methodDefinition.Name);
            declaringTypeToken = MetadataTokens.GetToken(methodDefinition.GetDeclaringType());
            return true;
        }
    }
}
