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
        public static readonly PortablePdbConversionOptions Default = new();

        /// <summary>
        /// When converting to Windows PDB do not convert Source Link to srcsrv.
        /// </summary>
        public bool SuppressSourceLinkConversion { get; }

        /// <summary>
        /// Additional variable definitions to add to variable section of the srcsvr stream.
        /// </summary>
        public ImmutableArray<KeyValuePair<string, string>> SrcSvrVariables { get; }

        /// <summary>
        /// Customizes creation of the Windows PDB writer.
        /// </summary>
        public SymUnmanagedWriterCreationOptions WriterCreationOptions { get; }

        // backwards compat overload
        public PortablePdbConversionOptions(bool suppressSourceLinkConversion, IEnumerable<KeyValuePair<string, string>>? srcSvrVariables)
            : this(suppressSourceLinkConversion, srcSvrVariables, SymUnmanagedWriterCreationOptions.Deterministic)
        {
        }

        public PortablePdbConversionOptions(
            bool suppressSourceLinkConversion = false,
            IEnumerable<KeyValuePair<string, string>>? srcSvrVariables = null,
            SymUnmanagedWriterCreationOptions writerCreationOptions = SymUnmanagedWriterCreationOptions.Deterministic)
        {
            var variables = srcSvrVariables?.ToImmutableArray() ?? ImmutableArray<KeyValuePair<string, string>>.Empty;
            PdbConverterPortableToWindows.ValidateSrcSvrVariables(variables, nameof(srcSvrVariables));

            SuppressSourceLinkConversion = suppressSourceLinkConversion;
            SrcSvrVariables = variables;
            WriterCreationOptions = writerCreationOptions;
        }
    }
}
