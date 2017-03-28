// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed partial class MetadataModel
    {
        private struct Name
        {
            public readonly PooledStringBuilder PooledBuilderOpt;
            public readonly AssemblyReferenceHandle AssemblyReferenceOpt;

            public Name(PooledStringBuilder pooledBuilder, AssemblyReferenceHandle assemblyReference)
            {
                PooledBuilderOpt = pooledBuilder;
                AssemblyReferenceOpt = assemblyReference;
            }

            public StringBuilder BuilderOpt => PooledBuilderOpt?.Builder;
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
                        pooled = name.PooledBuilderOpt;
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
                elementType.BuilderOpt?.Append("[]");
                return elementType;
            }

            public Name GetArrayType(Name elementType, ArrayShape shape)
            {
                var sb = elementType.BuilderOpt;
                if (sb == null)
                {
                    return default(Name);
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
                elementType.BuilderOpt?.Append('*');
                return elementType;
            }

            public Name GetGenericInstantiation(Name genericType, ImmutableArray<Name> typeArguments)
            {
                var sb = genericType.BuilderOpt;

                if (sb == null)
                {
                    return default(Name);
                }

                sb.Append('[');

                bool first = true;
                foreach (Name typeArgument in typeArguments)
                {
                    if (typeArgument.PooledBuilderOpt == null)
                    {
                        return default(Name);
                    }

                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    string serializedArgName = typeArgument.PooledBuilderOpt.ToStringAndFree();
                    
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
                default(Name);

            public Name GetFunctionPointerType(MethodSignature<Name> signature) =>
                default(Name);

            public Name GetModifiedType(Name modifier, Name unmodifiedType, bool isRequired) =>
                default(Name);

            public Name GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
                default(Name);

            public Name GetPinnedType(Name elementType) =>
                default(Name);

            public Name GetGenericMethodParameter(object genericContext, int index) =>
                default(Name);

            public Name GetGenericTypeParameter(object genericContext, int index) =>
                default(Name);
        }
    }
}