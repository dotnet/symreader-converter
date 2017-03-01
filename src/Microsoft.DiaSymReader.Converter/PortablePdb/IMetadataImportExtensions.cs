// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace Microsoft.DiaSymReader.PortablePdb
{
    // TODO: Copied from Microsoft.DiaSymReader.PortablePdb. Share.

    internal static class IMetadataImportExtensions
    {
        public static string GetQualifiedTypeName(this IMetadataImport importer, Handle typeDefOrRef)
        {
            string qualifiedName;
            if (typeDefOrRef.Kind == HandleKind.TypeDefinition)
            {
                TypeAttributes attributes;
                int baseType;
                importer.GetTypeDefProps(MetadataTokens.GetToken(typeDefOrRef), out qualifiedName, out attributes, out baseType);
            }
            else if (typeDefOrRef.Kind == HandleKind.TypeReference)
            {
                int resolutionScope;
                importer.GetTypeRefProps(MetadataTokens.GetToken(typeDefOrRef), out resolutionScope, out qualifiedName);
            }
            else
            {
                qualifiedName = null;
            }

            return qualifiedName;
        }

        public static unsafe void GetTypeDefProps(this IMetadataImport importer, int typeDefinition, out string qualifiedName, out TypeAttributes attributes, out int baseType)
        {
            int bufferLength;
            importer.GetTypeDefProps(typeDefinition, null, 0, out bufferLength, out attributes, null);

            var buffer = new StringBuilder(bufferLength);
            int baseTypeValue;
            importer.GetTypeDefProps(typeDefinition, buffer, buffer.Capacity, out bufferLength, out attributes, &baseTypeValue);
            qualifiedName = buffer.ToString();
            baseType = baseTypeValue;
        }

        public static void GetTypeRefProps(this IMetadataImport importer, int typeReference, out int resolutionScope, out string qualifiedName)
        {
            int bufferLength;
            importer.GetTypeRefProps(typeReference, out resolutionScope, null, 0, out bufferLength);

            var buffer = new StringBuilder(bufferLength);
            importer.GetTypeRefProps(typeReference, out resolutionScope, buffer, buffer.Capacity, out bufferLength);
            qualifiedName = buffer.ToString();
        }
    }
}