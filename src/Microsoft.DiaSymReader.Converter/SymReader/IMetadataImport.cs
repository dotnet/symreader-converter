// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.DiaSymReader
{
    // TODO: Copied from Roslyn. Share.
    // TODO: unify on char* vs StringBuilder
    // TODO: all should be preserve sig, review methods that aren't

    [ComVisible(false)]
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44")]
    internal unsafe interface IMetadataImport
    {
        void CloseEnum(int enumHandle);

        [PreserveSig]
        int CountEnum(int enumHandle, out int count);

        [PreserveSig]
        int ResetEnum(int enumHandle, int position);

        [PreserveSig]
        int EnumTypeDefs(
            ref int enumHandle,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]int[] typeDefs, 
            int bufferLength,
            out int count);

        [PreserveSig]
        int EnumInterfaceImpls(
            ref int enumHandle,
            int typeDefinition, 
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]int[] interfaceImpls,
            int bufferLength,
            out int count);

        [PreserveSig]
        int EnumTypeRefs(
            ref int enumHandle,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]int[] typeRefs,
            int bufferLength,
            out int count);

        [PreserveSig]
        int FindTypeDefByName(
            string name, 
            int declaringTypeDefOrRef,
            out int typeDef);

        [PreserveSig]
        int GetScopeProps(
            [Out, MarshalAs(UnmanagedType.LPWStr)]StringBuilder name, 
            int bufferLength, 
            out int nameLength, 
            [Out]Guid* mvid);

        [PreserveSig]
        int GetModuleFromScope(out int moduleDef);

        [PreserveSig]
        int GetTypeDefProps(
            int typeDef,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder qualifiedName,
            int qualifiedNameBufferLength,
            out int qualifiedNameLength,
            [Out]TypeAttributes* attributes,
            [Out]int* baseType);

        [PreserveSig]
        int GetInterfaceImplProps(
            int interfaceImpl, 
            [Out]int* typeDef,
            [Out]int* interfaceDefRefSpec);

        [PreserveSig]
        int GetTypeRefProps(
            int typeRef,
            [Out]int* resolutionScope, // ModuleRef or AssemblyRef
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder qualifiedName,
            int qualifiedNameBufferLength,
            out int qualifiedNameLength);

        uint ResolveTypeRef(uint tr, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppIScope);
        uint EnumMembers(ref uint handlePointerEnum, uint cl, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMembers, uint countMax);
        uint EnumMembersWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMembers, uint countMax);
        uint EnumMethods(ref uint handlePointerEnum, uint cl, uint* arrayMethods, uint countMax);
        uint EnumMethodsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethods, uint countMax);
        uint EnumFields(ref uint handlePointerEnum, uint cl, uint* arrayFields, uint countMax);
        uint EnumFieldsWithName(ref uint handlePointerEnum, uint cl, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayFields, uint countMax);
        uint EnumParams(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayParams, uint countMax);
        uint EnumMemberRefs(ref uint handlePointerEnum, uint tokenParent, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayMemberRefs, uint countMax);
        uint EnumMethodImpls(ref uint handlePointerEnum, uint td, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodBody, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayMethodDecl, uint countMax);
        uint EnumPermissionSets(ref uint handlePointerEnum, uint tk, uint dwordActions, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayPermission, uint countMax);
        uint FindMember(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint FindMethod(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint FindField(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);
        uint FindMemberRef(uint td, string stringName, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] byte[] voidPointerSigBlob, uint byteCountSigBlob);

        [PreserveSig]
        int GetMethodProps(
           int methodDef,
           [Out]int* declaringTypeDef,
           [Out]char* name, 
           int nameBufferLength,
           [Out]int* nameLength,
           [Out]ushort* attributes,
           [Out]byte* signature,
           [Out]int* signatureLength,
           [Out]int* relativeVirtualAddress,
           [Out]ushort* implAttributes);

        uint GetMemberRefProps(uint mr, ref uint ptk, StringBuilder stringMember, uint cchMember, out uint pchMember, out byte* ppvSigBlob);
        uint EnumProperties(ref uint handlePointerEnum, uint td, uint* arrayProperties, uint countMax);
        uint EnumEvents(ref uint handlePointerEnum, uint td, uint* arrayEvents, uint countMax);
        uint GetEventProps(uint ev, out uint pointerClass, StringBuilder stringEvent, uint cchEvent, out uint pchEvent, out uint pdwEventFlags, out uint ptkEventType, out uint pmdAddOn, out uint pmdRemoveOn, out uint pmdFire, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)] uint[] rmdOtherMethod, uint countMax);
        uint EnumMethodSemantics(ref uint handlePointerEnum, uint mb, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] uint[] arrayEventProp, uint countMax);
        uint GetMethodSemantics(uint mb, uint tokenEventProp);
        uint GetClassLayout(uint td, out uint pdwPackSize, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ulong[] arrayFieldOffset, uint countMax, out uint countPointerFieldOffset);
        uint GetFieldMarshal(uint tk, out byte* ppvNativeType);
        uint GetRVA(uint tk, out uint pulCodeRVA);
        uint GetPermissionSetProps(uint pm, out uint pdwAction, out void* ppvPermission);

        [PreserveSig]
        int GetSigFromToken(
            int standaloneSignature,
            [Out]byte** signature,
            [Out]int* signatureLength);

        uint GetModuleRefProps(uint mur, StringBuilder stringName, uint cchName);
        uint EnumModuleRefs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayModuleRefs, uint cmax);
        uint GetTypeSpecFromToken(uint typespec, out byte* ppvSig);
        uint GetNameFromToken(uint tk);
        uint EnumUnresolvedMethods(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayMethods, uint countMax);
        uint GetUserString(uint stk, StringBuilder stringString, uint cchString);
        uint GetPinvokeMap(uint tk, out uint pdwMappingFlags, StringBuilder stringImportName, uint cchImportName, out uint pchImportName);
        uint EnumSignatures(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arraySignatures, uint cmax);
        uint EnumTypeSpecs(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayTypeSpecs, uint cmax);
        uint EnumUserStrings(ref uint handlePointerEnum, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] arrayStrings, uint cmax);
        int GetParamForMethodIndex(uint md, uint ulongParamSeq, out uint pointerParam);
        uint EnumCustomAttributes(ref uint handlePointerEnum, uint tk, uint tokenType, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] uint[] arrayCustomAttributes, uint countMax);
        uint GetCustomAttributeProps(uint cv, out uint ptkObj, out uint ptkType, out void* ppBlob);
        uint FindTypeRef(uint tokenResolutionScope, string stringName);
        uint GetMemberProps(uint mb, out uint pointerClass, StringBuilder stringMember, uint cchMember, out uint pchMember, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pulCodeRVA, out uint pdwImplFlags, out uint pdwCPlusTypeFlag, out void* ppValue);
        uint GetFieldProps(uint mb, out uint pointerClass, StringBuilder stringField, uint cchField, out uint pchField, out uint pdwAttr, out byte* ppvSigBlob, out uint pcbSigBlob, out uint pdwCPlusTypeFlag, out void* ppValue);
        uint GetPropertyProps(uint prop, out uint pointerClass, StringBuilder stringProperty, uint cchProperty, out uint pchProperty, out uint pdwPropFlags, out byte* ppvSig, out uint bytePointerSig, out uint pdwCPlusTypeFlag, out void* ppDefaultValue, out uint pcchDefaultValue, out uint pmdSetter, out uint pmdGetter, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 14)] uint[] rmdOtherMethod, uint countMax);
        uint GetParamProps(uint tk, out uint pmd, out uint pulSequence, StringBuilder stringName, uint cchName, out uint pchName, out uint pdwAttr, out uint pdwCPlusTypeFlag, out void* ppValue);
        uint GetCustomAttributeByName(uint tokenObj, string stringName, out void* ppData);
        bool IsValidToken(uint tk);

        [PreserveSig]
        uint GetNestedClassProps(uint typeDefNestedClass);

        uint GetNativeCallConvFromSig(void* voidPointerSig, uint byteCountSig);
        int IsGlobal(uint pd);
    }
}