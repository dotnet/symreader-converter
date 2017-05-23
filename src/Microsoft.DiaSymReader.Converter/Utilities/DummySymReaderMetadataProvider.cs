// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed class DummySymReaderMetadataProvider : ISymReaderMetadataProvider
    {
        public static readonly ISymReaderMetadataProvider Instance = new DummySymReaderMetadataProvider();

        public unsafe bool TryGetStandaloneSignature(int standaloneSignatureToken, out byte* signature, out int length)
            => throw new NotSupportedException(ConverterResources.MetadataNotAvailable);

        public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes, out int baseTypeToken)
            => throw new NotSupportedException(ConverterResources.MetadataNotAvailable);

        public bool TryGetTypeReferenceInfo(int typeReferenceToken, out string namespaceName, out string typeName, out int resolutionScopeToken)
            => throw new NotSupportedException(ConverterResources.MetadataNotAvailable);
    }
}
