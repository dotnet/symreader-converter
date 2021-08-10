// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class PdbGuids
    {
        public static class Language
        {
            public static readonly Guid CSharp = new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1");
            public static readonly Guid VisualBasic = new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
            public static readonly Guid FSharp = new Guid("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3");
        }

        public static class HashAlgorithm
        {
            public static readonly Guid SHA1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
            public static readonly Guid SHA256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");
        }

        public static class LanguageVendor
        {
            public static readonly Guid Microsoft = new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1");
        }

        public static class DocumentType
        {
            public static readonly Guid Text = new Guid("{5a869d0b-6611-11d3-bd2a-0000f80849bd}");
        }
    }
}
