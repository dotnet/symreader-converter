﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal sealed class SymReaderMetadataImport : IMetadataImport, IMetadataEmit, IDisposable
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

        [PreserveSig]
        public unsafe int GetSigFromToken(
            int tkSignature,    // Signature token.
            out byte* ppvSig,   // return pointer to signature blob
            out int pcbSig)     // return size of signature
        {
            MetadataRequired();

            var sig = _metadataReaderOpt.GetStandaloneSignature((StandaloneSignatureHandle)MetadataTokens.Handle(tkSignature));
            var signature = _metadataReaderOpt.GetBlobBytes(sig.Signature);

            GCHandle pinnedBuffer = GCHandle.Alloc(signature, GCHandleType.Pinned);
            ppvSig = (byte*)pinnedBuffer.AddrOfPinnedObject();
            pcbSig = signature.Length;

            _pinnedBuffers.Add(pinnedBuffer);
            return 0;
        }

        public unsafe void GetTypeDefProps(
            int typeDefinition,
            [MarshalAs(UnmanagedType.LPWStr), Out]StringBuilder qualifiedName,
            int qualifiedNameBufferLength,
            out int qualifiedNameLength,
            [MarshalAs(UnmanagedType.U4)]out TypeAttributes attributes,
            [Out]int* baseType)
        {
            MetadataRequired();

            var handle = (TypeDefinitionHandle)MetadataTokens.Handle(typeDefinition);
            var typeDef = _metadataReaderOpt.GetTypeDefinition(handle);

            if (qualifiedName != null)
            {
                qualifiedName.Clear();

                if (!typeDef.Namespace.IsNil)
                {
                    qualifiedName.Append(_metadataReaderOpt.GetString(typeDef.Namespace));
                    qualifiedName.Append('.');
                }

                qualifiedName.Append(_metadataReaderOpt.GetString(typeDef.Name));
                qualifiedNameLength = qualifiedName.Length;
            }
            else
            {
                qualifiedNameLength =
                    (typeDef.Namespace.IsNil ? 0 : _metadataReaderOpt.GetString(typeDef.Namespace).Length + 1) +
                    _metadataReaderOpt.GetString(typeDef.Name).Length;
            }

            if (baseType != null)
            {
                *baseType = MetadataTokens.GetToken(typeDef.BaseType);
            }

            attributes = typeDef.Attributes;
        }

        public void GetTypeRefProps(
            int typeReference,
            out int resolutionScope,
            [MarshalAs(UnmanagedType.LPWStr), Out]StringBuilder qualifiedName,
            int qualifiedNameBufferLength,
            out int qualifiedNameLength)
        {
            MetadataRequired();

            var handle = (TypeReferenceHandle)MetadataTokens.Handle(typeReference);
            var typeRef = _metadataReaderOpt.GetTypeReference(handle);

            if (qualifiedName != null)
            {
                qualifiedName.Clear();

                if (!typeRef.Namespace.IsNil)
                {
                    qualifiedName.Append(_metadataReaderOpt.GetString(typeRef.Namespace));
                    qualifiedName.Append('.');
                }

                qualifiedName.Append(_metadataReaderOpt.GetString(typeRef.Name));
                qualifiedNameLength = qualifiedName.Length;
            }
            else
            {
                qualifiedNameLength =
                    (typeRef.Namespace.IsNil ? 0 : _metadataReaderOpt.GetString(typeRef.Namespace).Length + 1) +
                    _metadataReaderOpt.GetString(typeRef.Name).Length;
            }

            resolutionScope = MetadataTokens.GetToken(typeRef.ResolutionScope);
        }

        // The only purpose of this method is to get type name of the method and declaring type token (opaque for SymWriter), everything else is ignored by the SymWriter.
        // "mb" is the token passed to OpenMethod. The token is remembered until the corresponding CloseMethod, which passes it to GetMethodProps.
        // It's opaque for SymWriter.
        public unsafe int GetMethodProps(
            int methodDefinition, 
            out int declaringTypeDefinition,
            [Out]char* nameBuffer, 
            int nameBufferLength, 
            out int nameLength,
            [Out]ushort* attributes,
            [Out]byte* signature,
            [Out]int* signatureLength, 
            [Out]int* relativeVirtualAddress,
            [Out]ushort* implAttributes)
        {
            Debug.Assert(nameBuffer != null);
            Debug.Assert(attributes == null);
            Debug.Assert(signature == null);
            Debug.Assert(signatureLength == null);
            Debug.Assert(relativeVirtualAddress == null);
            Debug.Assert(implAttributes == null);

            MetadataRequired();

            var handle = (MethodDefinitionHandle)MetadataTokens.Handle(methodDefinition);
            var methodDef = _metadataReaderOpt.GetMethodDefinition(handle);

            string methodName = _metadataReaderOpt.GetString(methodDef.Name);

            // if the buffer is too small to fit the name, truncate the name
            int nameLengthIncludingNull = Math.Min(methodName.Length + 1, nameBufferLength);

            // return the length of the name not including NUL
            nameLength = nameLengthIncludingNull - 1;

            int methodNameByteCount = nameLengthIncludingNull * sizeof(char);
            fixed (char* methodNamePtr = methodName)
            {
                Buffer.MemoryCopy(methodNamePtr, nameBuffer, methodNameByteCount, methodNameByteCount);
            }

            declaringTypeDefinition = MetadataTokens.GetToken(methodDef.GetDeclaringType());
            return 0;
        }

        #region Not Implemented

        public void CloseEnum(uint handleEnum)
        {
            throw new NotImplementedException();
        }

        public uint CountEnum(uint handleEnum)
        {
            throw new NotImplementedException();
        }

        public uint EnumCustomAttributes(ref uint handlePointerEnum, uint tk, uint tokenType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayCustomAttributes, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumEvents(ref uint handlePointerEnum, uint td, uint* arrayEvents, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumFields(ref uint handlePointerEnum, uint cl, uint* arrayFields, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumFieldsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayFields, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumInterfaceImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] arrayImpls, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMemberRefs(ref uint handlePointerEnum, uint tokenParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] arrayMemberRefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMembers(ref uint handlePointerEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] arrayMembers, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMembersWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayMembers, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMethodImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayMethodBody, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayMethodDecl, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumMethods(ref uint handlePointerEnum, uint cl, uint* arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMethodSemantics(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] arrayEventProp, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumMethodsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumModuleRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arrayModuleRefs, uint cmax)
        {
            throw new NotImplementedException();
        }

        public uint EnumParams(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]uint[] arrayParams, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumPermissionSets(ref uint handlePointerEnum, uint tk, uint dwordActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]uint[] arrayPermission, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint EnumProperties(ref uint handlePointerEnum, uint td, uint* arrayProperties, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumSignatures(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arraySignatures, uint cmax)
        {
            throw new NotImplementedException();
        }

        public uint EnumTypeDefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arrayTypeDefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumTypeRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arrayTypeRefs, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumTypeSpecs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arrayTypeSpecs, uint cmax)
        {
            throw new NotImplementedException();
        }

        public uint EnumUnresolvedMethods(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arrayMethods, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint EnumUserStrings(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]uint[] arrayStrings, uint cmax)
        {
            throw new NotImplementedException();
        }

        public uint FindField(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindMember(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindMemberRef(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindMethod(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]byte[] voidPointerSigBlob, uint byteCountSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint FindTypeDefByName(string stringTypeDef, uint tokenEnclosingClass)
        {
            throw new NotImplementedException();
        }

        public uint FindTypeRef(uint tokenResolutionScope, string stringName)
        {
            throw new NotImplementedException();
        }

        public uint GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]ulong[] arrayFieldOffset, uint countMax, out uint countPointerFieldOffset)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetCustomAttributeByName(uint tokenObj, string stringName, out void* ppData)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out void* ppBlob)
        {
            throw new NotImplementedException();
        }

        public uint GetEventProps(uint ev, out uint pointerClass, StringBuilder stringEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)]uint[] rmdOtherMethod, uint countMax)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetFieldMarshal(uint tk, out byte* ppvNativeType)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetFieldProps(uint mb, out uint pointerClass, StringBuilder stringField, uint cchField, out uint pchField, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        public uint GetInterfaceImplProps(uint impl, out uint pointerClass)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetMemberProps(uint mb, out uint pointerClass, StringBuilder stringMember, uint cchMember, out uint pchMember, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder stringMember, uint cchMember, out uint pchMember, out byte* ppvSigBlob)
        {
            throw new NotImplementedException();
        }

        public uint GetMethodSemantics(uint mb, uint tokenEventProp)
        {
            throw new NotImplementedException();
        }

        public uint GetModuleFromScope()
        {
            throw new NotImplementedException();
        }

        public uint GetModuleRefProps(uint mur, StringBuilder stringName, uint cchName)
        {
            throw new NotImplementedException();
        }

        public uint GetNameFromToken(uint tk)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetNativeCallConvFromSig(void* voidPointerSig, uint byteCountSig)
        {
            throw new NotImplementedException();
        }

        public uint GetNestedClassProps(uint typeDefNestedClass)
        {
            throw new NotImplementedException();
        }

        public int GetParamForMethodIndex(uint md, uint ulongParamSeq, out uint pointerParam)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder stringName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission)
        {
            throw new NotImplementedException();
        }

        public uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder stringImportName, uint cchImportName, out uint pchImportName)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetPropertyProps(uint prop, out uint pointerClass, StringBuilder stringProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out byte* ppvSig, out uint bytePointerSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)]uint[] rmdOtherMethod, uint countMax)
        {
            throw new NotImplementedException();
        }

        public uint GetRVA(uint tk, out uint pulCodeRVA)
        {
            throw new NotImplementedException();
        }

        public Guid GetScopeProps(StringBuilder stringName, uint cchName, out uint pchName)
        {
            throw new NotImplementedException();
        }

        public unsafe uint GetTypeSpecFromToken(uint typespec, out byte* ppvSig)
        {
            throw new NotImplementedException();
        }

        public uint GetUserString(uint stk, StringBuilder stringString, uint cchString)
        {
            throw new NotImplementedException();
        }

        public int IsGlobal(uint pd)
        {
            throw new NotImplementedException();
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        public bool IsValidToken(uint tk)
        {
            throw new NotImplementedException();
        }

        public void ResetEnum(uint handleEnum, uint ulongPos)
        {
            throw new NotImplementedException();
        }

        public uint ResolveTypeRef(uint tr, [In]ref Guid riid, [MarshalAs(UnmanagedType.Interface)]out object ppIScope)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}