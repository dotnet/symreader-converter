// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable 436 // SuppressUnmanagedCodeSecurityAttribute defined in source and mscorlib 

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.DiaSymReader
{
    // TODO: Copied from Roslyn. Share.

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("BA3FEE4C-ECB9-4e41-83B7-183FA41CD859")]
    [SuppressUnmanagedCodeSecurity]
    internal unsafe interface IMetadataEmit
    {
        // SymWriter doesn't use any methods from this interface, unless DefineLocalVariable and DefineConstant are used in which case it calls GetTokenFromSig.
        // If DefineLocalVariable2 and DefineConstant2 are used instead (as we do) they set the signature and GetTokenFromSig is not called.
    }
}
