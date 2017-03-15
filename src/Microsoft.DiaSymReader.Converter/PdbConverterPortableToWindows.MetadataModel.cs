// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.DiaSymReader.Tools
{
    internal static partial class PdbConverterPortableToWindows<TDocumentWriter>
    {
        private sealed class MetadataModel
        {
            public MetadataReader Reader { get; }

            /// <summary>
            /// Maps standalone signature blobs to a handle. 
            /// </summary>
            private Lazy<Dictionary<byte[], StandaloneSignatureHandle>> _lazyStandaloneSignatureMap;

            public MetadataModel(MetadataReader reader)
            {
                Reader = reader;

                _lazyStandaloneSignatureMap = new Lazy<Dictionary<byte[], StandaloneSignatureHandle>>(BuildStandaloneSignatureMap);
            }

            private Dictionary<byte[], StandaloneSignatureHandle> BuildStandaloneSignatureMap()
            {
                int count = Reader.GetTableRowCount(TableIndex.StandAloneSig);

                var result = new Dictionary<byte[], StandaloneSignatureHandle>(count, ByteSequenceComparer.Instance);

                for (int rowId = 1; rowId < count; rowId++)
                {
                    var handle = MetadataTokens.StandaloneSignatureHandle(rowId);
                    var signature = Reader.GetStandaloneSignature(handle);
                    var bytes = Reader.GetBlobBytes(signature.Signature);
                    result[bytes] = handle;
                }

                return result;
            }

            public bool TryGetStandaloneSignatureHandle(byte[] signature, out StandaloneSignatureHandle handle) =>
                _lazyStandaloneSignatureMap.Value.TryGetValue(signature, out handle);

            internal string GetSerializedTypeName(EntityHandle targetType)
            {
                throw new NotImplementedException();
            }

            public bool TryGetStateMachineMoveNextMethod(MethodDefinitionHandle handle, out MethodDefinitionHandle moveNextHandle)
            {
                var methodDef = Reader.GetMethodDefinition(handle);
                foreach (var caHandle in methodDef.GetCustomAttributes())
                {
                    var ca = Reader.GetCustomAttribute(caHandle);
                    if (TryGetDeclaringTypeQualifiedName(ca.Constructor, out var namespaceHandle, out var nameHandle))
                    {
                        if ((Reader.StringComparer.Equals(nameHandle, "IteratorStateMachineAttribute") || Reader.StringComparer.Equals(nameHandle, "AsyncStateMachineAttribute")) &&
                             Reader.StringComparer.Equals(namespaceHandle, "System.Runtime.CompilerServices"))
                        {
                            // TODO: assumes correct attribute encoding, validate
                            var fixedArgs = ca.DecodeValue(StateMachineAttributeValueDecoder.Instance).FixedArguments;
                            if (fixedArgs.Length != 1)
                            {
                                // TODO: report error
                                moveNextHandle = default(MethodDefinitionHandle);
                                return false;
                            }

                            string serializedName = (string)fixedArgs[0].Value;
                            int nameIndex = serializedName.LastIndexOf('+');
                            if (nameIndex < 0)
                            {
                                // TODO: report error
                                moveNextHandle = default(MethodDefinitionHandle);
                                return false;
                            }

                            string typeName = serializedName.Substring(nameIndex + 1);
                            
                            var declTypeHandle = methodDef.GetDeclaringType();
                            var declTypeDef = Reader.GetTypeDefinition(declTypeHandle);

                            var nestedTypeHandle = FindNestedTypeByName(declTypeDef, typeName);
                            if (!nestedTypeHandle.IsNil)
                            {
                                moveNextHandle = FindMethodByName(Reader.GetTypeDefinition(nestedTypeHandle), "MoveNext");
                                return !moveNextHandle.IsNil;
                            }

                            moveNextHandle = default(MethodDefinitionHandle);
                            return false;
                        }
                    }
                }

                moveNextHandle = default(MethodDefinitionHandle);
                return false;
            }

            private TypeDefinitionHandle FindNestedTypeByName(TypeDefinition typeDef, string name)
            {
                foreach (var nestedTypeHandle in typeDef.GetNestedTypes())
                {
                    var nestedTypeDef = Reader.GetTypeDefinition(nestedTypeHandle);
                    if (Reader.StringComparer.Equals(nestedTypeDef.Name, name))
                    {
                        return nestedTypeHandle;
                    }
                }

                return default(TypeDefinitionHandle);
            }

            private MethodDefinitionHandle FindMethodByName(TypeDefinition typeDef, string name)
            {
                foreach (var methodHandle in typeDef.GetMethods())
                {
                    var methodDef = Reader.GetMethodDefinition(methodHandle);
                    if (Reader.StringComparer.Equals(methodDef.Name, name))
                    {
                        return methodHandle;
                    }
                }

                return default(MethodDefinitionHandle);
            }

            private bool TryGetDeclaringTypeQualifiedName(EntityHandle attributeConstructorHandle, out StringHandle namespaceHandle, out StringHandle nameHandle)
            {
                switch (attributeConstructorHandle.Kind)
                {
                    case HandleKind.MethodDefinition:
                        var methodDef = Reader.GetMethodDefinition((MethodDefinitionHandle)attributeConstructorHandle);
                        var declaringTypeDef = Reader.GetTypeDefinition(methodDef.GetDeclaringType());
                        namespaceHandle = declaringTypeDef.Namespace;
                        nameHandle = declaringTypeDef.Name;
                        return true;

                    case HandleKind.MemberReference:
                        var parent = Reader.GetMemberReference((MemberReferenceHandle)attributeConstructorHandle).Parent;
                        switch (parent.Kind)
                        {
                            case HandleKind.TypeReference:
                                var typeRef = Reader.GetTypeReference((TypeReferenceHandle)parent);
                                namespaceHandle = typeRef.Namespace;
                                nameHandle = typeRef.Name;
                                return true;

                            case HandleKind.TypeDefinition:
                                var typeDef = Reader.GetTypeDefinition((TypeDefinitionHandle)parent);
                                namespaceHandle = typeDef.Namespace;
                                nameHandle = typeDef.Name;
                                return true;
                        }

                        break;
                }

                namespaceHandle = nameHandle = default(StringHandle);
                return false;
            }

            /// <summary>
            /// The only argument of state machine attributes is <see cref="Type"/>, 
            /// so we only need to implement enough to get the serialized type name out.
            /// </summary>
            private sealed class StateMachineAttributeValueDecoder : ICustomAttributeTypeProvider<string>
            {
                public static readonly StateMachineAttributeValueDecoder Instance = new StateMachineAttributeValueDecoder();

                public bool IsSystemType(string type) => true;
                public string GetTypeFromSerializedName(string name) => name;

                public string GetPrimitiveType(PrimitiveTypeCode typeCode) => null;
                public string GetSystemType() => null;
                public string GetSZArrayType(string elementType) => null;
                public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => null;
                public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => null;
                public PrimitiveTypeCode GetUnderlyingEnumType(string type) => default(PrimitiveTypeCode);
            }
        }
    }
}