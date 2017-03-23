// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed class MetadataModel
    {
        public MetadataReader Reader { get; }

        /// <summary>
        /// Maps standalone signature blobs to a handle. 
        /// </summary>
        private readonly Lazy<Dictionary<byte[], StandaloneSignatureHandle>> _lazyStandaloneSignatureMap;

        private readonly Lazy<ImmutableArray<string>> _lazyAssemblyRefDisplayNames;
        private readonly Lazy<IReadOnlyDictionary<string, AssemblyReferenceHandle>> _lazyAssemblyRefDisplayNameMap;
        private readonly SerializedTypeNameSignatureDecoder _serializedTypeNameDecoder;
        private readonly Lazy<AssemblyReferenceHandle> _lazyCorlibAssemblyRef;

        private readonly Lazy<IReadOnlyDictionary<byte[], TypeSpecificationHandle>> _lazyTypeSpecificationMap;
        private readonly Lazy<IReadOnlyDictionary<string, EntityHandle>> _lazySerializedTypeNameMap;

        public MetadataModel(MetadataReader reader, bool vbSemantics)
        {
            Reader = reader;
            _lazyCorlibAssemblyRef = new Lazy<AssemblyReferenceHandle>(FindCorlibAssemblyRef);
            _lazyStandaloneSignatureMap = new Lazy<Dictionary<byte[], StandaloneSignatureHandle>>(BuildStandaloneSignatureMap);
            _lazyAssemblyRefDisplayNames = new Lazy<ImmutableArray<string>>(BuildAssemblyRefDisplayNames);
            _lazyAssemblyRefDisplayNameMap = new Lazy<IReadOnlyDictionary<string, AssemblyReferenceHandle>>(BuildAssemblyRefDisplayNameMap);
            _serializedTypeNameDecoder = new SerializedTypeNameSignatureDecoder(this, useAssemblyQualification: !vbSemantics, nestedNameSeparator: vbSemantics ? '.' : '+');
            _lazyTypeSpecificationMap = new Lazy<IReadOnlyDictionary<byte[], TypeSpecificationHandle>>(BuildTypeSpecificationMap);
            _lazySerializedTypeNameMap = new Lazy<IReadOnlyDictionary<string, EntityHandle>>(BuildSerializedTypeNameMap);
        }

        public ImmutableArray<int> GetRowCounts()
        {
            var builder = ImmutableArray.CreateBuilder<int>(MetadataTokens.TableCount);
            for (int i = 0; i < MetadataTokens.TableCount; i++)
            {
                builder.Add(Reader.GetTableRowCount((TableIndex)i));
            }

            return builder.MoveToImmutable();
        }

        private static readonly (string, string)[] s_corTypes = new(string, string)[]
        {
               ("System", "Enum"),
               ("System", "ValueType"),
               ("System", "Delegate"),
               ("System", "MulticastDelegate"),
               ("System", "Decimal"),
               ("System", "Array"),
               ("System", "DateTime"),
               ("System", "Nullable`1"),
               ("System", "IDisposable"),
               ("System", "IAsyncResult"),
               ("System", "AsyncCallback"),
               ("System.Collections.Generic", "IEnumerable`1"),
               ("System.Collections.Generic", "IList`1"),
               ("System.Collections.Generic", "ICollection`1"),
               ("System.Collections.Generic", "IEnumerator`1"),
               ("System.Collections.Generic", "IReadOnlyList`1"),
               ("System.Collections.Generic", "IReadOnlyCollection`1"),
               ("System.Collections", "IEnumerable"),
               ("System.Collections", "IEnumerator"),
               ("System.Runtime.CompilerServices", "IsVolatile"),
               ("System", "Object"),
               ("System", "Char"),
               ("System", "Boolean"),
               ("System", "SByte"),
               ("System", "Byte"),
               ("System", "Int16"),
               ("System", "UInt16"),
               ("System", "Int32"),
               ("System", "UInt32"),
               ("System", "Int64"),
               ("System", "UInt64"),
               ("System", "Single"),
               ("System", "Double"),
               ("System", "String"),
               ("System", "IntPtr"),
               ("System", "UIntPtr"),
               ("System", "TypedReference"),
               ("System", "Void"),
        };

        private AssemblyReferenceHandle FindCorlibAssemblyRef()
        {
            var comparer = Reader.StringComparer;
            foreach (var (ns, name) in s_corTypes)
            {
                foreach (var typeRefHandle in Reader.TypeReferences)
                {
                    var typeRef = Reader.GetTypeReference(typeRefHandle);
                    if (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference &&
                        comparer.Equals(typeRef.Name, name) &&
                        comparer.Equals(typeRef.Namespace, ns))
                    {
                        return (AssemblyReferenceHandle)typeRef.ResolutionScope;
                    }
                }
            }

            // TODO: report warning
            return default(AssemblyReferenceHandle);
        }

        private ImmutableArray<string> BuildAssemblyRefDisplayNames()
        {
            var result = ArrayBuilder<string>.GetInstance(Reader.AssemblyReferences.Count);

            foreach (var assemblyRefHandle in Reader.AssemblyReferences)
            {
                var assemblyRef = Reader.GetAssemblyReference(assemblyRefHandle);
                result.Add(AssemblyDisplayNameBuilder.GetAssemblyDisplayName(Reader, assemblyRef));
            }

            return result.ToImmutableAndFree();
        }

        private IReadOnlyDictionary<string, AssemblyReferenceHandle> BuildAssemblyRefDisplayNameMap()
        {
            var map = new Dictionary<string, AssemblyReferenceHandle>(StringComparer.OrdinalIgnoreCase);

            var displayNames = _lazyAssemblyRefDisplayNames.Value;
            for (int i = 0; i < displayNames.Length; i++)
            {
                map.Add(displayNames[i], MetadataTokens.AssemblyReferenceHandle(i + 1));
            }

            return map;
        }

        private string GetDisplayName(AssemblyReferenceHandle handle) =>
            _lazyAssemblyRefDisplayNames.Value[MetadataTokens.GetRowNumber(handle) - 1];

        public bool TryResolveAssemblyReference(string displayName, out AssemblyReferenceHandle handle) =>
            _lazyAssemblyRefDisplayNameMap.Value.TryGetValue(displayName, out handle);
        
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

        private IReadOnlyDictionary<byte[], TypeSpecificationHandle> BuildTypeSpecificationMap()
        {
            var result = new Dictionary<byte[], TypeSpecificationHandle>(ByteSequenceComparer.Instance);

            for (int rowId = 1; rowId <= Reader.GetTableRowCount(TableIndex.TypeSpec); rowId++)
            {
                var handle = MetadataTokens.TypeSpecificationHandle(rowId);
                var typeSpec = Reader.GetTypeSpecification(handle);

                result[Reader.GetBlobBytes(typeSpec.Signature)] = handle;
            }

            return result;
        }

        public bool TryResolveTypeSpecification(byte[] spec, out TypeSpecificationHandle typeSpec) =>
                _lazyTypeSpecificationMap.Value.TryGetValue(spec, out typeSpec);

        public string GetSerializedTypeName(EntityHandle typeHandle) =>
            _serializedTypeNameDecoder.GetSerializedTypeName(typeHandle);

        internal static void BuildQualifiedName(StringBuilder builder, MetadataReader reader, TypeDefinitionHandle typeHandle, char nestedNameSeparator)
        {
            const TypeAttributes IsNestedMask = (TypeAttributes)0x00000006;

            var typeDef = reader.GetTypeDefinition(typeHandle);

            if ((typeDef.Attributes & IsNestedMask) != 0)
            {
                var names = ArrayBuilder<StringHandle>.GetInstance();
                while (true)
                {
                    var declaringTypeHandle = typeDef.GetDeclaringType();
                    if (declaringTypeHandle.IsNil)
                    {
                        break;
                    }

                    names.Add(typeDef.Name);
                    typeDef = reader.GetTypeDefinition(declaringTypeHandle);
                }

                BuildQualifiedName(builder, reader, typeDef.Namespace, typeDef.Name, names, nestedNameSeparator);
                names.Free();
            }
            else
            {
                BuildQualifiedName(builder, reader, typeDef.Namespace, typeDef.Name);
            }
        }

        internal static void BuildQualifiedName(StringBuilder builder, MetadataReader reader, TypeReferenceHandle typeHandle, char nestedNameSeparator, out AssemblyReferenceHandle assemblyRefHandle)
        {
            var typeRef = reader.GetTypeReference(typeHandle);
            switch (typeRef.ResolutionScope.Kind)
            {
                case HandleKind.ModuleDefinition:
                case HandleKind.ModuleReference:
                case HandleKind.AssemblyReference:
                    BuildQualifiedName(builder, reader, typeRef.Namespace, typeRef.Name);
                    break;

                case HandleKind.TypeReference:
                    var names = ArrayBuilder<StringHandle>.GetInstance();
                    while (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
                    {
                        names.Add(typeRef.Name);
                        typeRef = reader.GetTypeReference((TypeReferenceHandle)typeRef.ResolutionScope);
                    }

                    BuildQualifiedName(builder, reader, typeRef.Namespace, typeRef.Name, names, nestedNameSeparator);
                    names.Free();
                    break;

                default:
                    throw new BadImageFormatException();
            }

            assemblyRefHandle = (typeRef.ResolutionScope.Kind == HandleKind.AssemblyReference) ?
                (AssemblyReferenceHandle)typeRef.ResolutionScope : default(AssemblyReferenceHandle);
        }

        private static void BuildQualifiedName(StringBuilder builder, MetadataReader reader, StringHandle namespaceHandle, StringHandle nameHandle, IReadOnlyList<StringHandle> nestedNames, char nestedNameSeparator)
        {
            BuildQualifiedName(builder, reader, namespaceHandle, nameHandle);

            for (int i = nestedNames.Count - 1; i >= 0; i--)
            {
                builder.Append(nestedNameSeparator);
                BuildName(builder, reader, nestedNames[i]);
            }
        }

        private static void BuildQualifiedName(StringBuilder builder, MetadataReader reader, StringHandle namespaceHandle, StringHandle nameHandle)
        {
            if (!namespaceHandle.IsNil)
            {
                builder.Append(reader.GetString(namespaceHandle));
                builder.Append('.');
            }

            BuildName(builder, reader, nameHandle);
        }

        private static void BuildName(StringBuilder builder, MetadataReader reader, StringHandle nameHandle)
        {
            const string needsEscaping = "\\[]*.+,& ";
            foreach (char c in reader.GetString(nameHandle))
            {
                if (needsEscaping.IndexOf(c) >= 0)
                {
                    builder.Append('\\');
                }

                builder.Append(c);
            }
        }

        private struct Name
        {
            public readonly PooledStringBuilder PooledBuilder;
            public readonly AssemblyReferenceHandle AssemblyReferenceOpt;

            public Name(PooledStringBuilder pooledBuilder, AssemblyReferenceHandle assemblyReference)
            {
                PooledBuilder = pooledBuilder;
                AssemblyReferenceOpt = assemblyReference;
            }

            public StringBuilder Builder => PooledBuilder.Builder;
        }

        private sealed class SerializedTypeNameSignatureDecoder : ISignatureTypeProvider<Name, object>
        {
            private readonly MetadataModel _model;
            private readonly bool _useAssemblyQualification;
            private readonly char _nestedNameSeparator;

            public SerializedTypeNameSignatureDecoder(MetadataModel model, bool useAssemblyQualification, char nestedNameSeparator)
            {
                _model = model;
                _useAssemblyQualification = useAssemblyQualification;
                _nestedNameSeparator = nestedNameSeparator;
            }

            public string GetSerializedTypeName(EntityHandle typeHandle)
            {
                AssemblyReferenceHandle assemblyQualifierOpt;
                PooledStringBuilder pooled;
                switch (typeHandle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        pooled = PooledStringBuilder.GetInstance();
                        BuildQualifiedName(pooled.Builder, _model.Reader, (TypeDefinitionHandle)typeHandle, _nestedNameSeparator);
                        assemblyQualifierOpt = default(AssemblyReferenceHandle);
                        break;

                    case HandleKind.TypeReference:
                        pooled = PooledStringBuilder.GetInstance();
                        BuildQualifiedName(pooled.Builder, _model.Reader, (TypeReferenceHandle)typeHandle, _nestedNameSeparator, out assemblyQualifierOpt);

                        if (!_useAssemblyQualification)
                        {
                            assemblyQualifierOpt = default(AssemblyReferenceHandle);
                        }

                        break;

                    case HandleKind.TypeSpecification:
                        var typeSpec = _model.Reader.GetTypeSpecification((TypeSpecificationHandle)typeHandle);
                        var name = typeSpec.DecodeSignature(this, genericContext: null);
                        pooled = name.PooledBuilder;
                        assemblyQualifierOpt = name.AssemblyReferenceOpt;
                        break;

                    default:
                        throw new BadImageFormatException();
                }

                string result = pooled.ToStringAndFree();
                return assemblyQualifierOpt.IsNil ? result : result + ", " + _model.GetDisplayName(assemblyQualifierOpt);
            }

            public Name GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                var pooled = PooledStringBuilder.GetInstance();
                pooled.Builder.Append(GetPrimitiveTypeQualifiedName(typeCode));
                return new Name(pooled, _useAssemblyQualification ? _model._lazyCorlibAssemblyRef.Value : default(AssemblyReferenceHandle));
            }

            public Name GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var pooled = PooledStringBuilder.GetInstance();
                BuildQualifiedName(pooled, reader, handle, _nestedNameSeparator);
                return new Name(pooled, default(AssemblyReferenceHandle));
            }

            public Name GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var pooled = PooledStringBuilder.GetInstance();
                BuildQualifiedName(pooled.Builder, reader, handle, _nestedNameSeparator, out var assemblyReferenceHandle);
                return new Name(pooled, _useAssemblyQualification ? assemblyReferenceHandle : default(AssemblyReferenceHandle));
            }

            public Name GetSZArrayType(Name elementType)
            {
                elementType.Builder.Append("[]");
                return elementType;
            }

            public Name GetArrayType(Name elementType, ArrayShape shape)
            {
                var sb = elementType.Builder;

                sb.Append('[');

                if (shape.Rank == 1)
                {
                    sb.Append('*');
                }

                sb.Append(',', shape.Rank - 1);

                sb.Append(']');
                return elementType;
            }

            public Name GetPointerType(Name elementType)
            {
                elementType.Builder.Append('*');
                return elementType;
            }

            public Name GetGenericInstantiation(Name genericType, ImmutableArray<Name> typeArguments)
            {
                var sb = genericType.Builder;
                sb.Append('[');

                bool first = true;
                foreach (Name typeArgument in typeArguments)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    string serializedArgName = typeArgument.PooledBuilder.ToStringAndFree();
                    if (!typeArgument.AssemblyReferenceOpt.IsNil)
                    {
                        sb.Append('[');
                        sb.Append(serializedArgName);
                        sb.Append(", ");
                        sb.Append(_model.GetDisplayName(typeArgument.AssemblyReferenceOpt));
                        sb.Append(']');
                    }
                    else
                    {
                        sb.Append(serializedArgName);
                    }
                }

                sb.Append(']');
                return genericType;
            }

            public Name GetByReferenceType(Name elementType) =>
                throw new BadImageFormatException();

            public Name GetFunctionPointerType(MethodSignature<Name> signature) =>
                throw new BadImageFormatException();

            public Name GetModifiedType(Name modifier, Name unmodifiedType, bool isRequired) =>
                throw new BadImageFormatException();

            public Name GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
                throw new BadImageFormatException();

            public Name GetPinnedType(Name elementType) =>
                throw new BadImageFormatException();

            public Name GetGenericMethodParameter(object genericContext, int index) =>
                throw new BadImageFormatException();

            public Name GetGenericTypeParameter(object genericContext, int index) =>
                throw new BadImageFormatException();
        }

        private static string GetPrimitiveTypeQualifiedName(PrimitiveTypeCode typeCode)
        {
            switch (typeCode)
            {
                case PrimitiveTypeCode.Boolean: return nameof(System) + "." + nameof(Boolean);
                case PrimitiveTypeCode.Byte: return nameof(System) + "." + nameof(Byte);
                case PrimitiveTypeCode.SByte: return nameof(System) + "." + nameof(SByte);
                case PrimitiveTypeCode.Int16: return nameof(System) + "." + nameof(Int16);
                case PrimitiveTypeCode.UInt16: return nameof(System) + "." + nameof(UInt16);
                case PrimitiveTypeCode.Char: return nameof(System) + "." + nameof(Char);
                case PrimitiveTypeCode.Int32: return nameof(System) + "." + nameof(Int32);
                case PrimitiveTypeCode.UInt32: return nameof(System) + "." + nameof(UInt32);
                case PrimitiveTypeCode.Int64: return nameof(System) + "." + nameof(Int64);
                case PrimitiveTypeCode.UInt64: return nameof(System) + "." + nameof(UInt64);
                case PrimitiveTypeCode.IntPtr: return nameof(System) + "." + nameof(IntPtr);
                case PrimitiveTypeCode.UIntPtr: return nameof(System) + "." + nameof(UIntPtr);
                case PrimitiveTypeCode.Single: return nameof(System) + "." + nameof(Single);
                case PrimitiveTypeCode.Double: return nameof(System) + "." + nameof(Double);
                case PrimitiveTypeCode.Object: return nameof(System) + "." + nameof(Object);
                case PrimitiveTypeCode.String: return nameof(System) + "." + nameof(String);
                case PrimitiveTypeCode.TypedReference: return "System.TypedReference";
                default:
                    throw new BadImageFormatException();
            }
        }

        private IReadOnlyDictionary<string, EntityHandle> BuildSerializedTypeNameMap()
        {
            var map = new Dictionary<string, EntityHandle>();

            foreach (var handle in Reader.TypeDefinitions)
            {
                map[GetSerializedTypeName(handle)] = handle;
            }

            foreach (var handle in Reader.TypeReferences)
            {
                map[GetSerializedTypeName(handle)] = handle;
            }

            for (int rowId = 1; rowId <= Reader.GetTableRowCount(TableIndex.TypeSpec); rowId++)
            {
                var handle = MetadataTokens.TypeSpecificationHandle(rowId);
                map[GetSerializedTypeName(handle)] = handle;
            }

            return map;
        }

        internal bool TryResolveType(string serializedTypeName, out EntityHandle type) =>
            _lazySerializedTypeNameMap.Value.TryGetValue(serializedTypeName, out type);

        public MethodDefinitionHandle FindStateMachineMoveNextMethod(MethodDefinitionHandle methodHandle, bool vbSemantics)
        {
            var methodDef = Reader.GetMethodDefinition(methodHandle);
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
                            return default(MethodDefinitionHandle);
                        }

                        string serializedName = (string)fixedArgs[0].Value;
                        int nameIndex = serializedName.LastIndexOf('+');
                        if (nameIndex < 0)
                        {
                            // TODO: report error
                            return default(MethodDefinitionHandle);
                        }

                        string typeName = serializedName.Substring(nameIndex + 1);
                        return FindStateMachineMoveNextMethod(methodDef, typeName, isGenericSuffixIncluded: false);
                    }
                }
            }

            // Attributes are applied only when available in the target framework or in source.
            // Fallback to well-known generated names.
            var stateMachineTypeHandle = FindNestedStateMachineTypeByNamePattern(methodDef, vbSemantics);
            if (!stateMachineTypeHandle.IsNil)
            {
                return FindMethodByName(Reader.GetTypeDefinition(stateMachineTypeHandle), "MoveNext");
            }

            return default(MethodDefinitionHandle);
        }

        public MethodDefinitionHandle FindStateMachineMoveNextMethod(MethodDefinition kickoffMethodDef, string stateMachineTypeName, bool isGenericSuffixIncluded)
        {
            var declTypeHandle = kickoffMethodDef.GetDeclaringType();
            var declTypeDef = Reader.GetTypeDefinition(declTypeHandle);

            var nestedTypeHandle = isGenericSuffixIncluded ?
                FindNestedTypeByName(declTypeDef, stateMachineTypeName) :
                FindSingleNestedTypeByNamePrefixAndSuffix(declTypeDef, stateMachineTypeName);

            if (!nestedTypeHandle.IsNil)
            {
                return FindMethodByName(Reader.GetTypeDefinition(nestedTypeHandle), "MoveNext");
            }

            return default(MethodDefinitionHandle);
        }

        private TypeDefinitionHandle FindNestedStateMachineTypeByNamePattern(MethodDefinition kickoffMethodDef, bool vbSemantics)
        {
            var declaringTypeDef = Reader.GetTypeDefinition(kickoffMethodDef.GetDeclaringType());
            string escapedMethodName = Reader.GetString(kickoffMethodDef.Name).Replace(".", "-");

            if (vbSemantics)
            {
                // VB state machine type name pattern:
                // "VB$StateMachine_*_{method name with . replaced by _}" 
                return FindSingleNestedTypeByNamePrefixAndSuffix(declaringTypeDef, prefix: "VB$StateMachine_", suffix: "_" + escapedMethodName);
            }
            else
            {
                // C# state machine type name pattern:
                // <{method name with . replaced by _}>d*
                return FindSingleNestedTypeByNamePrefixAndSuffix(declaringTypeDef, prefix: "<" + escapedMethodName + ">d");
            }
        }

        private TypeDefinitionHandle FindNestedTypeByName(TypeDefinition typeDef, string name)
        {
            // note: the reader caches the nested type map
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

        private TypeDefinitionHandle FindSingleNestedTypeByNamePrefixAndSuffix(TypeDefinition typeDef, string prefix, string suffix = null)
        {
            var candidate = default(TypeDefinitionHandle);
            foreach (var typeHandle in typeDef.GetNestedTypes())
            {
                var nestedTypeDef = Reader.GetTypeDefinition(typeHandle);
                if (Reader.StringComparer.StartsWith(nestedTypeDef.Name, prefix) &&
                    (suffix == null || Reader.GetString(nestedTypeDef.Name).EndsWith(suffix)))
                {
                    if (candidate.IsNil)
                    {
                        candidate = typeHandle;
                    }
                    else
                    {
                        // multiple candidates
                        return default(TypeDefinitionHandle);
                    }
                }
            }

            return candidate;
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