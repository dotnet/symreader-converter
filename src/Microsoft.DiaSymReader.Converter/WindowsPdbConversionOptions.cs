// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.DiaSymReader.Tools
{
    /// <summary>
    /// Windows to Portable PDB conversion options.
    /// </summary>
    public sealed class WindowsPdbConversionOptions
    {
        public static readonly WindowsPdbConversionOptions Default = new();

        /// <summary>
        /// Customizes creation of the Windows PDB reader.
        /// </summary>
        public SymUnmanagedReaderCreationOptions ReaderCreationOptions { get; }

        public WindowsPdbConversionOptions(
            SymUnmanagedReaderCreationOptions readerCreationOptions = SymUnmanagedReaderCreationOptions.Default)
        {
            ReaderCreationOptions = readerCreationOptions;
        }
    }
}
