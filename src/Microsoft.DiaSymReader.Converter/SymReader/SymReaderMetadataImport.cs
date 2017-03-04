// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            _metadataOwnerOpt?.Dispose();

            foreach (var pinnedBuffer in _pinnedBuffers)
            {
                pinnedBuffer.Free();
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

        public int GetSigFromToken(
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

        public int GetTypeDefProps(
            int typeDef,
            [MarshalAs(UnmanagedType.LPWStr), Out]StringBuilder qualifiedName,
            int qualifiedNameBufferLength,
            out int qualifiedNameLength,
            [Out]TypeAttributes* attributes,
            [Out]int* baseType)
        {
            MetadataRequired();

            var handle = (TypeDefinitionHandle)MetadataTokens.Handle(typeDef);
            var typeDefinition = _metadataReaderOpt.GetTypeDefinition(handle);

            if (qualifiedName != null)
            {
                qualifiedName.Clear();

                if (!typeDefinition.Namespace.IsNil)
                {
                    qualifiedName.Append(_metadataReaderOpt.GetString(typeDefinition.Namespace));
                    qualifiedName.Append('.');
                }

                qualifiedName.Append(_metadataReaderOpt.GetString(typeDefinition.Name));
                qualifiedNameLength = qualifiedName.Length;
            }
            else
            {
                qualifiedNameLength =
                    (typeDefinition.Namespace.IsNil ? 0 : _metadataReaderOpt.GetString(typeDefinition.Namespace).Length + 1) +
                    _metadataReaderOpt.GetString(typeDefinition.Name).Length;
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

        public int GetTypeRefProps(
            int typeRef,
            [Out]int* resolutionScope,
            [MarshalAs(UnmanagedType.LPWStr), Out]StringBuilder qualifiedName,
            int qualifiedNameBufferLength,
            out int qualifiedNameLength)
        {
            MetadataRequired();

            var handle = (TypeReferenceHandle)MetadataTokens.Handle(typeRef);
            var typeReference = _metadataReaderOpt.GetTypeReference(handle);

            if (qualifiedName != null)
            {
                qualifiedName.Clear();

                if (!typeReference.Namespace.IsNil)
                {
                    qualifiedName.Append(_metadataReaderOpt.GetString(typeReference.Namespace));
                    qualifiedName.Append('.');
                }

                qualifiedName.Append(_metadataReaderOpt.GetString(typeReference.Name));
                qualifiedNameLength = qualifiedName.Length;
            }
            else
            {
                qualifiedNameLength =
                    (typeReference.Namespace.IsNil ? 0 : _metadataReaderOpt.GetString(typeReference.Namespace).Length + 1) +
                    _metadataReaderOpt.GetString(typeReference.Name).Length;
            }

            if (resolutionScope != null)
            {
                *resolutionScope = MetadataTokens.GetToken(typeReference.ResolutionScope);
            }

            return HResult.S_OK;
        }

        // The only purpose of this method is to get type name of the method and declaring type token (opaque for SymWriter), everything else is ignored by the SymWriter.
        // "mb" is the token passed to OpenMethod. The token is remembered until the corresponding CloseMethod, which passes it to GetMethodProps.
        // It's opaque for SymWriter.
        public int GetMethodProps(
            int methodDef, 
            [Out] int* declaringTypeDef, 
            [Out] char* name, 
            int nameBufferLength, 
            [Out] int* nameLength, 
            [Out] ushort* attributes,
            [Out] byte* signature,
            [Out] int* signatureLength, 
            [Out] int* relativeVirtualAddress,
            [Out] ushort* implAttributes)
        {
            Debug.Assert(name != null);
            Debug.Assert(nameLength != null);
            Debug.Assert(declaringTypeDef != null);
            Debug.Assert(attributes == null);
            Debug.Assert(signature == null);
            Debug.Assert(signatureLength == null);
            Debug.Assert(relativeVirtualAddress == null);
            Debug.Assert(implAttributes == null);

            MetadataRequired();

            var handle = (MethodDefinitionHandle)MetadataTokens.Handle(methodDef);
            var methodDefinition = _metadataReaderOpt.GetMethodDefinition(handle);

            string methodName = _metadataReaderOpt.GetString(methodDefinition.Name);

            // if the buffer is too small to fit the name, truncate the name
            int nameLengthIncludingNull = Math.Min(methodName.Length + 1, nameBufferLength);

            // return the length of the name not including NUL
            *nameLength = nameLengthIncludingNull - 1;
            
#if TRUE // remove when not targeting net45
            for (int i = 0; i < nameLengthIncludingNull - 1; i++)
            {
                name[i] = methodName[i];
            }

            name[nameLengthIncludingNull - 1] = '\0';
#else
            int methodNameByteCount = nameLengthIncludingNull * sizeof(char);
            fixed (char* methodNamePtr = methodName)
            {
                Buffer.MemoryCopy(methodNamePtr, name, methodNameByteCount, methodNameByteCount);
            }
#endif

            *declaringTypeDef = MetadataTokens.GetToken(methodDefinition.GetDeclaringType());

            return HResult.S_OK;
        }

        #region Not Implemented

        public void CloseEnum(int enumHandle)
        {
            throw new NotImplementedException();
        }

        public int CountEnum(int enumHandle, out int count)
        {
            throw new NotImplementedException();
        }

        public int ResetEnum(int enumHandle, int position)
        {
            throw new NotImplementedException();
        }

        public int EnumTypeDefs(ref int enumHandle, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] int[] typeDefs, int bufferLength, out int count)
        {
            throw new NotImplementedException();
        }

        public int EnumInterfaceImpls(ref int enumHandle, int typeDefinition, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] interfaceImpls, int bufferLength, out int count)
        {
            throw new NotImplementedException();
        }

        public int EnumTypeRefs(ref int enumHandle, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] int[] typeRefs, int bufferLength, out int count)
        {
            throw new NotImplementedException();
        }

        public int FindTypeDefByName(string name, int declaringTypeDefOrRef, out int typeDef)
        {
            throw new NotImplementedException();
        }

        public unsafe int GetScopeProps([MarshalAs(UnmanagedType.LPWStr), Out] StringBuilder name, int bufferLength, out int nameLength, [Out] Guid* mvid)
        {
            throw new NotImplementedException();
        }

        public int GetModuleFromScope(out int moduleDef)
        {
            throw new NotImplementedException();
        }

        public unsafe int GetInterfaceImplProps(int interfaceImpl, [Out] int* typeDef, [Out] int* interfaceDefRefSpec)
        {
            throw new NotImplementedException();
        }

        public uint ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIScope)
        {
            throw new NotImplementedException();
        }

        public uint EnumMembers(ref uint handlePointerEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMembers, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMembersWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMembers, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumMethods(ref uint handlePointerEnum, uint cl, uint* arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMethodsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumFields(ref uint handlePointerEnum, uint cl, uint* arrayFields, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumFieldsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayFields, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumParams(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayParams, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMemberRefs(ref uint handlePointerEnum, uint tokenParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMemberRefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMethodImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodBody, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodDecl, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumPermissionSets(ref uint handlePointerEnum, uint tk, uint dwordActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayPermission, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint FindMember(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindMethod(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindField(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindMemberRef(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder stringMember, uint cchMember, out uint pchMember, out byte* ppvSigBlob)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumProperties(ref uint handlePointerEnum, uint td, uint* arrayProperties, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumEvents(ref uint handlePointerEnum, uint td, uint* arrayEvents, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint GetEventProps(uint ev, out uint pointerClass, StringBuilder stringEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)] uint[] rmdOtherMethod, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMethodSemantics(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayEventProp, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint GetMethodSemantics(uint mb, uint tokenEventProp)
        {
            throw new NotImplementedException();
        }

        public uint GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ulong[] arrayFieldOffset, uint countMax, out uint countPointerFieldOffset)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetFieldMarshal(uint tk, out byte* ppvNativeType)
        {
            throw new NotImplementedException();
        }

        public uint GetRVA(uint tk, out uint pulCodeRVA)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission)
        {
            throw new NotImplementedException();
        }

        public uint GetModuleRefProps(uint mur, StringBuilder stringName, uint cchName)
        {
            throw new NotImplementedException();
        }

        public uint EnumModuleRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayModuleRefs, uint cmax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetTypeSpecFromToken(uint typespec, out byte* ppvSig)
        {
            throw new NotImplementedException();
        }

        public uint GetNameFromToken(uint tk)
        {
            throw new NotImplementedException();
        }

        public uint EnumUnresolvedMethods(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint GetUserString(uint stk, StringBuilder stringString, uint cchString)
        {
            throw new NotImplementedException();
        }

        public uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder stringImportName, uint cchImportName, out uint pchImportName)
        {
            throw new NotImplementedException();
        }

        public uint EnumSignatures(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arraySignatures, uint cmax)
        {
            throw new NotImplementedException();
        }

        public uint EnumTypeSpecs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeSpecs, uint cmax)
        {
            throw new NotImplementedException();
        }

        public uint EnumUserStrings(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayStrings, uint cmax)
        {
            throw new NotImplementedException();
        }

        public int GetParamForMethodIndex(uint md, uint ulongParamSeq, out uint pointerParam)
        {
            throw new NotImplementedException();
        }

        public uint EnumCustomAttributes(ref uint handlePointerEnum, uint tk, uint tokenType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayCustomAttributes, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out void* ppBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindTypeRef(uint tokenResolutionScope, string stringName)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetMemberProps(uint mb, out uint pointerClass, StringBuilder stringMember, uint cchMember, out uint pchMember, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetFieldProps(uint mb, out uint pointerClass, StringBuilder stringField, uint cchField, out uint pchField, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetPropertyProps(uint prop, out uint pointerClass, StringBuilder stringProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out byte* ppvSig, out uint bytePointerSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder stringName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetCustomAttributeByName(uint tokenObj, string stringName, out void* ppData)
        {
            throw new NotImplementedException();
        }

        public bool IsValidToken(uint tk)
        {
            throw new NotImplementedException();
        }

        public uint GetNestedClassProps(uint typeDefNestedClass)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetNativeCallConvFromSig(void* voidPointerSig, uint byteCountSig)
        {
            throw new NotImplementedException();
        }

        public int IsGlobal(uint pd)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMetadataEmit

        void IMetadataEmit.__SetModuleProps() => throw new NotImplementedException();
        void IMetadataEmit.__Save() => throw new NotImplementedException();
        void IMetadataEmit.__SaveToStream() => throw new NotImplementedException();
        uint IMetadataEmit.__GetSaveSize() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineTypeDef() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineNestedType() => throw new NotImplementedException();
        void IMetadataEmit.__SetHandler() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineMethod() => throw new NotImplementedException();
        void IMetadataEmit.__DefineMethodImpl() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineTypeRefByName() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineImportType() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineMemberRef() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineImportMember() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineEvent() => throw new NotImplementedException();
        void IMetadataEmit.__SetClassLayout() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteClassLayout() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldMarshal() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteFieldMarshal() => throw new NotImplementedException();
        uint IMetadataEmit.__DefinePermissionSet() => throw new NotImplementedException();
        void IMetadataEmit.__SetRVA() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineModuleRef() => throw new NotImplementedException();
        void IMetadataEmit.__SetParent() => throw new NotImplementedException();
        uint IMetadataEmit.__GetTokenFromTypeSpec() => throw new NotImplementedException();
        void IMetadataEmit.__SaveToMemory() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineUserString() => throw new NotImplementedException();
        void IMetadataEmit.__DeleteToken() => throw new NotImplementedException();
        void IMetadataEmit.__SetMethodProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetTypeDefProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetEventProps() => throw new NotImplementedException();
        uint IMetadataEmit.__SetPermissionSetProps() => throw new NotImplementedException();
        void IMetadataEmit.__DefinePinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__SetPinvokeMap() => throw new NotImplementedException();
        void IMetadataEmit.__DeletePinvokeMap() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineCustomAttribute() => throw new NotImplementedException();
        void IMetadataEmit.__SetCustomAttributeValue() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineField() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineProperty() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineParam() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetPropertyProps() => throw new NotImplementedException();
        void IMetadataEmit.__SetParamProps() => throw new NotImplementedException();
        uint IMetadataEmit.__DefineSecurityAttributeSet() => throw new NotImplementedException();
        void IMetadataEmit.__ApplyEditAndContinue() => throw new NotImplementedException();
        uint IMetadataEmit.__TranslateSigWithScope() => throw new NotImplementedException();
        void IMetadataEmit.__SetMethodImplFlags() => throw new NotImplementedException();
        void IMetadataEmit.__SetFieldRVA() => throw new NotImplementedException();
        void IMetadataEmit.__Merge() => throw new NotImplementedException();
        void IMetadataEmit.__MergeEnd() => throw new NotImplementedException();

        #endregion
    }
}