// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.DiaSymReader.Tools
{
    public enum PdbConversionOptions
    {
        Default = 0,

        /// <summary>
        /// When converting to Windows PDB include Source Link data as is, 
        /// without converting to srcsrv.
        /// </summary>
        SuppressSourceLinkConversion = 1,
    }
}
