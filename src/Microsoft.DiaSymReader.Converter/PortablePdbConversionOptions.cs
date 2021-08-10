// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.DiaSymReader.Tools
{
    /// <summary>
    /// Portable to Windows PDB conversion options.
    /// </summary>
    public sealed class PortablePdbConversionOptions
    {
        public static readonly PortablePdbConversionOptions Default = new PortablePdbConversionOptions();

        /// <summary>
        /// When converting to Windows PDB do not convert Source Link to srcsrv.
        /// </summary>
        public bool SuppressSourceLinkConversion { get; }

        /// <summary>
        /// Additional variable definitions to add to variable section of the srcsvr stream.
        /// </summary>
        public ImmutableArray<KeyValuePair<string, string>> SrcSvrVariables { get; }

        public PortablePdbConversionOptions(
            bool suppressSourceLinkConversion = false,
            IEnumerable<KeyValuePair<string, string>>? srcSvrVariables = null)
        {
            var variables = srcSvrVariables?.ToImmutableArray() ?? ImmutableArray<KeyValuePair<string, string>>.Empty;
            PdbConverterPortableToWindows.ValidateSrcSvrVariables(variables, nameof(srcSvrVariables));

            SuppressSourceLinkConversion = suppressSourceLinkConversion;
            SrcSvrVariables = variables;
        }
    }
}
