// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed partial class MetadataModel
    {
        private readonly struct Name
        {
            public readonly PooledStringBuilder? PooledBuilder;
            public readonly AssemblyReferenceHandle AssemblyReferenceOpt;

            public Name(PooledStringBuilder? pooledBuilder, AssemblyReferenceHandle assemblyReferenceOpt)
            {
                PooledBuilder = pooledBuilder;
                AssemblyReferenceOpt = assemblyReferenceOpt;
            }

            public StringBuilder? Builder => PooledBuilder?.Builder;
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

            public string? GetSerializedTypeName(EntityHandle typeHandle)
            {
                AssemblyReferenceHandle assemblyQualifierOpt;
                PooledStringBuilder? pooled;
                switch (typeHandle.Kind)
                {
                    case HandleKind.TypeDefinition:
                        pooled = PooledStringBuilder.GetInstance();
                        BuildQualifiedName(pooled.Builder, _model.Reader, (TypeDefinitionHandle)typeHandle, _nestedNameSeparator);
                        assemblyQualifierOpt = default;
                        break;

                    case HandleKind.TypeReference:
                        pooled = PooledStringBuilder.GetInstance();
                        BuildQualifiedName(pooled.Builder, _model.Reader, (TypeReferenceHandle)typeHandle, _nestedNameSeparator, out assemblyQualifierOpt);

                        if (!_useAssemblyQualification)
                        {
                            assemblyQualifierOpt = default;
                        }

                        break;

                    case HandleKind.TypeSpecification:
                        var typeSpec = _model.Reader.GetTypeSpecification((TypeSpecificationHandle)typeHandle);
                        var name = typeSpec.DecodeSignature(this, genericContext: null!);
                        pooled = name.PooledBuilder;
                        if (pooled == null)
                        {
                            return null;
                        }

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
                return new Name(pooled, _useAssemblyQualification ? _model._lazyCorlibAssemblyRef.Value : default);
            }

            public Name GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var pooled = PooledStringBuilder.GetInstance();
                BuildQualifiedName(pooled, reader, handle, _nestedNameSeparator);
                return new Name(pooled, default);
            }

            public Name GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var pooled = PooledStringBuilder.GetInstance();
                BuildQualifiedName(pooled.Builder, reader, handle, _nestedNameSeparator, out var assemblyReferenceHandle);
                return new Name(pooled, _useAssemblyQualification ? assemblyReferenceHandle : default);
            }

            public Name GetSZArrayType(Name elementType)
            {
                elementType.Builder?.Append("[]");
                return elementType;
            }

            public Name GetArrayType(Name elementType, ArrayShape shape)
            {
                var sb = elementType.Builder;
                if (sb == null)
                {
                    return default;
                }

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
                elementType.Builder?.Append('*');
                return elementType;
            }

            public Name GetGenericInstantiation(Name genericType, ImmutableArray<Name> typeArguments)
            {
                var sb = genericType.Builder;

                if (sb == null)
                {
                    return default;
                }

                sb.Append('[');

                bool first = true;
                foreach (Name typeArgument in typeArguments)
                {
                    if (typeArgument.PooledBuilder == null)
                    {
                        return default;
                    }

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
                default;

            public Name GetFunctionPointerType(MethodSignature<Name> signature) =>
                default;

            public Name GetModifiedType(Name modifier, Name unmodifiedType, bool isRequired) =>
                default;

            public Name GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
                default;

            public Name GetPinnedType(Name elementType) =>
                default;

            public Name GetGenericMethodParameter(object genericContext, int index) =>
                default;

            public Name GetGenericTypeParameter(object genericContext, int index) =>
                default;
        }
    }
}