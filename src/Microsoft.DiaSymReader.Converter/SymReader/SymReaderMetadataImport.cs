// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection;
using Microsoft.DiaSymReader.Tools;
using System.Diagnostics;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// Minimal implementation of IMetadataImport that implements APIs used by SymReader and SymWriter.
    /// </summary>
    internal unsafe sealed class SymReaderMetadataImport : IMetadataImport, IMetadataEmit, IDisposable
    {
        private readonly MetadataReader _metadataReaderOpt;
        private readonly IDisposable _metadataOwnerOpt;
        private readonly List<GCHandle> _pinnedBuffers;

        public SymReaderMetadataImport(MetadataReader metadataReaderOpt, IDisposable metadataOwnerOpt)
        {
            _metadataReaderOpt = metadataReaderOpt;
            _pinnedBuffers = new List<GCHandle>();
            _metadataOwnerOpt = metadataOwnerOpt;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            _metadataOwnerOpt?.Dispose();

            var pinnedBuffers = _pinnedBuffers;
            if (pinnedBuffers != null)
            {
                foreach (var pinnedBuffer in pinnedBuffers)
                {
                    pinnedBuffer.Free();
                }
            }
        }

        ~SymReaderMetadataImport()
        {
            Dispose(false);
        }

        private void MetadataRequired()
        {
            if (_metadataReaderOpt == null)
            {
                throw new NotSupportedException(ConverterResources.MetadataNotAvailable);
            }
        }

        int IMetadataEmit.GetTokenFromSig(byte* voidPointerSig, int byteCountSig)
        {
            // Only used when building constant signature. 
            // We trick SymWriter into embedding NIL token into the PDB if 
            // we don't have a real signature token matching the constant type.
            return MetadataTokens.GetToken(default(StandaloneSignatureHandle));
        }

        int IMetadataImport.GetSigFromToken(
            int standaloneSignature, 
            [Out]byte** signature,
            [Out]int* signatureLength)
        {
            MetadataRequired();

            var sigHandle = (StandaloneSignatureHandle)MetadataTokens.Handle(standaloneSignature);

            // happens when a constant doesn't have a signature:
            if (sigHandle.IsNil)
            {
                if (signature != null)
                {
                    *signature = null;
                }

                if (signatureLength != null)
                {
                    *signatureLength = 0;
                }

                // The caller expect the signature to have at least one byte on success, 
                // so we need to fail here. Otherwise AV happens.
                return HResult.E_INVALIDARG;
            }

            var sig = _metadataReaderOpt.GetStandaloneSignature(sigHandle);
            var bytes = _metadataReaderOpt.GetBlobBytes(sig.Signature);

            var pinnedBuffer = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            if (signature != null)
            {
                *signature = (byte*)pinnedBuffer.AddrOfPinnedObject();
            }

            if (signatureLength != null)
            {
                *signatureLength = bytes.Length;
            }

            _pinnedBuffers.Add(pinnedBuffer);
            return HResult.S_OK;
        }

        int IMetadataImport.GetTypeDefProps(
            int typeDef,
            [Out]char* qualifiedName,
            int qualifiedNameBufferLength,
            [Out]int* qualifiedNameLength,
            [Out]TypeAttributes* attributes,
            [Out]int* baseType)
        {
            MetadataRequired();

            var handle = (TypeDefinitionHandle)MetadataTokens.Handle(typeDef);
            var typeDefinition = _metadataReaderOpt.GetTypeDefinition(handle);

            if (qualifiedNameLength != null || qualifiedName != null)
            {
                InteropUtilities.CopyQualifiedTypeName(
                    qualifiedName,
                    qualifiedNameLength,
                    _metadataReaderOpt.GetString(typeDefinition.Namespace),
                    _metadataReaderOpt.GetString(typeDefinition.Name));
            }

            if (attributes != null)
            {
                *attributes = typeDefinition.Attributes;
            }

            if (baseType != null)
            {
                *baseType = MetadataTokens.GetToken(typeDefinition.BaseType);
            }

            return HResult.S_OK;
        }

        int IMetadataImport.GetTypeRefProps(
            int typeRef,
            [Out]int* resolutionScope, // ModuleRef or AssemblyRef
            [Out]char* qualifiedName,
            int qualifiedNameBufferLength,
            [Out]int* qualifiedNameLength)
        {
            MetadataRequired();

            var handle = (TypeReferenceHandle)MetadataTokens.Handle(typeRef);
            var typeReference = _metadataReaderOpt.GetTypeReference(handle);

            if (qualifiedNameLength != null || qualifiedName != null)
            {
                InteropUtilities.CopyQualifiedTypeName(
                    qualifiedName,
                    qualifiedNameLength,
                    _metadataReaderOpt.GetString(typeReference.Namespace),
                    _metadataReaderOpt.GetString(typeReference.Name));
            }

            if (resolutionScope != null)
            {
                *resolutionScope = MetadataTokens.GetToken(typeReference.ResolutionScope);
            }

            return HResult.S_OK;
        }

        int IMetadataImport.GetNestedClassProps(int nestedClass, out int enclosingClass)
        {
            MetadataRequired();

            var nestedTypeDef = _metadataReaderOpt.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(nestedClass));
            var declaringTypeHandle = nestedTypeDef.GetDeclaringType();

            if (declaringTypeHandle.IsNil)
            {
                enclosingClass = 0;
                return HResult.E_FAIL;
            }
            else
            {
                enclosingClass = MetadataTokens.GetToken(declaringTypeHandle);
                return HResult.S_OK;
            }
        }

        // The only purpose of this method is to get type name of the method and declaring type token (opaque for SymWriter), everything else is ignored by the SymWriter.
        // "mb" is the token passed to OpenMethod. The token is remembered until the corresponding CloseMethod, which passes it to GetMethodProps.
        // It's opaque for SymWriter.
        int IMetadataImport.GetMethodProps(
            int methodDef, 
            [Out] int* declaringTypeDef, 
            [Out] char* name, 
            int nameBufferLength, 
            [Out] int* nameLength, 
            [Out] MethodAttributes* attributes,
            [Out] byte** signature,
            [Out] int* signatureLength, 
            [Out] int* relativeVirtualAddress,
            [Out] MethodImplAttributes* implAttributes)
        {
            MetadataRequired();

            Debug.Assert(attributes == null);
            Debug.Assert(signature == null);
            Debug.Assert(signatureLength == null);
            Debug.Assert(relativeVirtualAddress == null);
            Debug.Assert(implAttributes == null);

            var handle = (MethodDefinitionHandle)MetadataTokens.Handle(methodDef);
            var methodDefinition = _metadataReaderOpt.GetMethodDefinition(handle);

            if (name != null || nameLength != null)
            {
                string nameStr = _metadataReaderOpt.GetString(methodDefinition.Name);

                // if the buffer is too small to fit the name, truncate the name.
                // -1 to account for a NUL terminator.
                int adjustedLength = Math.Min(nameStr.Length, nameBufferLength - 1);

                // return the length of the name not including NUL
                if (nameLength != null)
                {
                    *nameLength = adjustedLength;
                }

                if (name != null)
                {
                    InteropUtilities.StringCopy(name, nameStr, adjustedLength);
                }
            }

            if (declaringTypeDef != null)
            {
                *declaringTypeDef = MetadataTokens.GetToken(methodDefinition.GetDeclaringType());
            }

            return HResult.S_OK;
        }

        #region Not implemented

        void IMetadataEmit.__SetModuleProps() => throw new NotImplementedException();
        void IMetadataEmit.__Save() => throw new NotImplementedException();
        void IMetadataEmit.__SaveToStream() => throw new NotImplementedException();
        void IMetadataEmit.__GetSaveSize() => throw new NotImplementedException();
        void IMetadataEmit.__DefineTypeDef() => throw new NotImplementedException();
        void IMetadataEmit.__DefineNestedType() => throw new NotImplementedException();
        void IMetadataEmit.__SetHandler() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMethod() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMethodImpl() => throw new NotImplementedException();
        void IMetadataEmit.__DefineTypeRefByName() => throw new NotImplementedException();
        void IMetadataEmit.__DefineImportType() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMemberRef() => throw new NotImplementedException();
        void IMetadataEmit.__DefineImportMember() => throw new NotImplementedException();
        void IMetadataEmit.__DefineEvent() => throw new NotImplementedException();
        void IMetadataEmit.__SetClassLayout() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteClassLayout() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldMarshal() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteFieldMarshal() => throw new NotImplementedException();
        void IMetadataEmit.__DefinePermissionSet() => throw new NotImplementedException();
        void IMetadataEmit.__SetRVA() => throw new NotImplementedException();
        void IMetadataEmit.__DefineModuleRef() => throw new NotImplementedException();
        void IMetadataEmit.__SetParent() => throw new NotImplementedException();
        void IMetadataEmit.__GetTokenFromTypeSpec() => throw new NotImplementedException();
        void IMetadataEmit.__SaveToMemory() => throw new NotImplementedException();
        void IMetadataEmit.__DefineUserString() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteToken() => throw new NotImplementedException();
        void IMetadataEmit.__SetMethodProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetTypeDefProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetEventProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetPermissionSetProps() => throw new NotImplementedException();
        void IMetadataEmit.__DefinePinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__SetPinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__DeletePinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__DefineCustomAttribute() => throw new NotImplementedException();
        void IMetadataEmit.__SetCustomAttributeValue() => throw new NotImplementedException();
        void IMetadataEmit.__DefineField() => throw new NotImplementedException();
        void IMetadataEmit.__DefineProperty() => throw new NotImplementedException();
        void IMetadataEmit.__DefineParam() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetPropertyProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetParamProps() => throw new NotImplementedException();
        void IMetadataEmit.__DefineSecurityAttributeSet() => throw new NotImplementedException();
        void IMetadataEmit.__ApplyEditAndContinue() => throw new NotImplementedException();
        void IMetadataEmit.__TranslateSigWithScope() => throw new NotImplementedException();
        void IMetadataEmit.__SetMethodImplFlags() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldRVA() => throw new NotImplementedException();
        void IMetadataEmit.__Merge() => throw new NotImplementedException();
        void IMetadataEmit.__MergeEnd() => throw new NotImplementedException();

        void IMetadataImport.CloseEnum(int enumHandle) => throw new NotImplementedException();
        int IMetadataImport.CountEnum(int enumHandle, out int count) => throw new NotImplementedException();
        int IMetadataImport.ResetEnum(int enumHandle, int position) => throw new NotImplementedException();
        int IMetadataImport.EnumTypeDefs(ref int enumHandle, int* typeDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumInterfaceImpls(ref int enumHandle, int typeDef, int* interfaceImpls, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumTypeRefs(ref int enumHandle, int* typeRefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.FindTypeDefByName(string name, int enclosingClass, out int typeDef) => throw new NotImplementedException();
        int IMetadataImport.GetScopeProps(char* name, int bufferLength, int* nameLength, Guid* mvid) => throw new NotImplementedException();
        int IMetadataImport.GetModuleFromScope(out int moduleDef) => throw new NotImplementedException();
        int IMetadataImport.GetInterfaceImplProps(int interfaceImpl, int* typeDef, int* interfaceDefRefSpec) => throw new NotImplementedException();
        int IMetadataImport.ResolveTypeRef(int typeRef, ref Guid scopeInterfaceId, out object scope, out int typeDef) => throw new NotImplementedException();
        int IMetadataImport.EnumMembers(ref int enumHandle, int typeDef, int* memberDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMembersWithName(ref int enumHandle, int typeDef, string name, int* memberDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMethods(ref int enumHandle, int typeDef, int* methodDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMethodsWithName(ref int enumHandle, int typeDef, string name, int* methodDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumFields(ref int enumHandle, int typeDef, int* fieldDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumFieldsWithName(ref int enumHandle, int typeDef, string name, int* fieldDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumParams(ref int enumHandle, int methodDef, int* paramDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMemberRefs(ref int enumHandle, int parentToken, int* memberRefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumMethodImpls(ref int enumHandle, int typeDef, int* implementationTokens, int* declarationTokens, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumPermissionSets(ref int enumHandle, int token, uint action, int* declSecurityTokens, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.FindMember(int typeDef, string name, byte* signature, int signatureLength, out int memberDef) => throw new NotImplementedException();
        int IMetadataImport.FindMethod(int typeDef, string name, byte* signature, int signatureLength, out int methodDef) => throw new NotImplementedException();
        int IMetadataImport.FindField(int typeDef, string name, byte* signature, int signatureLength, out int fieldDef) => throw new NotImplementedException();
        int IMetadataImport.FindMemberRef(int typeDef, string name, byte* signature, int signatureLength, out int memberRef) => throw new NotImplementedException();
        int IMetadataImport.GetMemberRefProps(int memberRef, int* declaringType, char* name, int nameBufferLength, int* nameLength, byte** signature, int* signatureLength) => throw new NotImplementedException();
        int IMetadataImport.EnumProperties(ref int enumHandle, int typeDef, int* properties, int bufferLength, int* count) => throw new NotImplementedException();
        uint IMetadataImport.EnumEvents(ref int enumHandle, int typeDef, int* events, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetEventProps(int @event, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, int* eventType, int* adderMethodDef, int* removerMethodDef, int* raiserMethodDef, int* otherMethodDefs, int otherMethodDefBufferLength, int* methodMethodDefsLength) => throw new NotImplementedException();
        int IMetadataImport.EnumMethodSemantics(ref int enumHandle, int methodDef, int* eventsAndProperties, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetMethodSemantics(int methodDef, int eventOrProperty, int* semantics) => throw new NotImplementedException();
        int IMetadataImport.GetClassLayout(int typeDef, int* packSize, MetadataImportFieldOffset* fieldOffsets, int bufferLength, int* count, int* typeSize) => throw new NotImplementedException();
        int IMetadataImport.GetFieldMarshal(int fieldDef, byte** nativeTypeSignature, int* nativeTypeSignatureLengvth) => throw new NotImplementedException();
        int IMetadataImport.GetRVA(int methodDef, int* relativeVirtualAddress, int* implAttributes) => throw new NotImplementedException();
        int IMetadataImport.GetPermissionSetProps(int declSecurity, uint* action, byte** permissionBlob, int* permissionBlobLength) => throw new NotImplementedException();
        int IMetadataImport.GetModuleRefProps(int moduleRef, char* name, int nameBufferLength, int* nameLength) => throw new NotImplementedException();
        int IMetadataImport.EnumModuleRefs(ref int enumHandle, int* moduleRefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetTypeSpecFromToken(int typeSpec, byte** signature, int* signatureLength) => throw new NotImplementedException();
        int IMetadataImport.GetNameFromToken(int token, byte* nameUTF8) => throw new NotImplementedException();
        int IMetadataImport.EnumUnresolvedMethods(ref int enumHandle, int* methodDefs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetUserString(int userStringToken, char* buffer, int bufferLength, int* length) => throw new NotImplementedException();
        int IMetadataImport.GetPinvokeMap(int memberDef, int* attributes, char* importName, int importNameBufferLength, int* importNameLength, int* moduleRef) => throw new NotImplementedException();
        int IMetadataImport.EnumSignatures(ref int enumHandle, int* signatureTokens, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumTypeSpecs(ref int enumHandle, int* typeSpecs, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.EnumUserStrings(ref int enumHandle, int* userStrings, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetParamForMethodIndex(int methodDef, int sequenceNumber, out int parameterToken) => throw new NotImplementedException();
        int IMetadataImport.EnumCustomAttributes(ref int enumHandle, int parent, int attributeType, int* customAttributes, int bufferLength, int* count) => throw new NotImplementedException();
        int IMetadataImport.GetCustomAttributeProps(int customAttribute, int* parent, int* constructor, byte** value, int* valueLength) => throw new NotImplementedException();
        int IMetadataImport.FindTypeRef(int resolutionScope, string name, out int typeRef) => throw new NotImplementedException();
        int IMetadataImport.GetMemberProps(int member, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, byte** signature, int* signatureLength, int* relativeVirtualAddress, int* implAttributes, int* constantType, byte** constantValue, int* constantValueLength) => throw new NotImplementedException();
        int IMetadataImport.GetFieldProps(int fieldDef, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, byte** signature, int* signatureLength, int* constantType, byte** constantValue, int* constantValueLength) => throw new NotImplementedException();
        int IMetadataImport.GetPropertyProps(int propertyDef, int* declaringTypeDef, char* name, int nameBufferLength, int* nameLength, int* attributes, byte** signature, int* signatureLength, int* constantType, byte** constantValue, int* constantValueLength, int* setterMethodDef, int* getterMethodDef, int* outerMethodDefs, int outerMethodDefsBufferLength, int* otherMethodDefCount) => throw new NotImplementedException();
        int IMetadataImport.GetParamProps(int parameter, int* declaringMethodDef, int* sequenceNumber, char* name, int nameBufferLength, int* nameLength, int* attributes, int* constantType, byte** constantValue, int* constantValueLength) => throw new NotImplementedException();
        int IMetadataImport.GetCustomAttributeByName(int parent, string name, byte** value, int* valueLength) => throw new NotImplementedException();
        bool IMetadataImport.IsValidToken(int token) => throw new NotImplementedException();
        int IMetadataImport.GetNativeCallConvFromSig(byte* signature, int signatureLength, int* callingConvention) => throw new NotImplementedException();
        int IMetadataImport.IsGlobal(int token, bool value) => throw new NotImplementedException();

        #endregion
    }
}