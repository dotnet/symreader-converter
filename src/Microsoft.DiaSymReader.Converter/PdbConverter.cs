// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.Tools
{
    public sealed class PdbConverter
    {
        /// <summary>
        /// An instance of <see cref="PdbConverter"/> with no diagnostic reporting.
        /// </summary>
        public static PdbConverter Default { get; } = new PdbConverter();

        /// <summary>
        /// Creates PDB converter with an optional callback invoked whenever a diagnostic is to be reported.
        /// </summary>
        private readonly Action<PdbDiagnostic>? _diagnosticReporter;

        public PdbConverter(Action<PdbDiagnostic>? diagnosticReporter = null)
        {
            _diagnosticReporter = diagnosticReporter;
        }

        /// <summary>
        /// Checks whether given PDB stream has Portable format.
        /// </summary>
        /// <param name="pdbStream">Stream.</param>
        /// <returns>Returns true if the given stream starts with a Portable PDB signature.</returns>
        /// <exception cref="ArgumentException"><paramref name="pdbStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pdbStream"/> is null.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading.</exception>
        public static bool IsPortable(Stream pdbStream)
        {
            StreamUtilities.ValidateStream(pdbStream, nameof(pdbStream), readRequired: true, seekRequired: true);
            return SymReaderHelpers.IsPortable(pdbStream);
        }

        /// <summary>
        /// Converts Windows PDB stream to Portable PDB.
        /// </summary>
        /// <param name="peStream">PE image stream (.dll or .exe)</param>
        /// <param name="sourcePdbStream">Source stream of Windows PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Portable PDB data. Must be writable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="peStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading/writing.</exception>
        public void ConvertWindowsToPortable(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            StreamUtilities.ValidateStream(peStream, nameof(peStream), readRequired: true, seekRequired: true);
            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                ConvertWindowsToPortable(peReader, sourcePdbStream, targetPdbStream);
            }
        }

        /// <summary>
        /// Converts Windows PDB stream to Portable PDB.
        /// </summary>
        /// <param name="peReader">PE image stream (.dll or .exe)</param>
        /// <param name="sourcePdbStream">Source stream of Windows PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Portable PDB data. Must be writable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the PDB stream is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading/writing.</exception>
        public void ConvertWindowsToPortable(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream)
            => ConvertWindowsToPortable(peReader, sourcePdbStream, targetPdbStream, options: null);

        /// <summary>
        /// Converts Windows PDB stream to Portable PDB.
        /// </summary>
        /// <param name="peReader">PE image stream (.dll or .exe)</param>
        /// <param name="sourcePdbStream">Source stream of Windows PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Portable PDB data. Must be writable.</param>
        /// <param name="options">Conversion options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the PDB stream is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading/writing.</exception>
        public void ConvertWindowsToPortable(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream, WindowsPdbConversionOptions? options)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);
            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            try
            {
                new PdbConverterWindowsToPortable(_diagnosticReporter).Convert(peReader, sourcePdbStream, targetPdbStream, (options ?? WindowsPdbConversionOptions.Default).ReaderCreationOptions);
            }
            catch (COMException e)
            {
                throw new BadImageFormatException(string.Format(ConverterResources.InvalidPdbFormat, e.Message), e);
            }
        }

        /// <summary>
        /// Converts Portable PDB stream to Windows PDB.
        /// </summary>
        /// <param name="peStream">PE image stream (.dll or .exe)</param>
        /// <param name="sourcePdbStream">Source stream of Portable PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Windows PDB data. Must be writable.</param>
        /// <param name="options">Conversion options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="peStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading/writing.</exception>
        public void ConvertPortableToWindows(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream, PortablePdbConversionOptions? options = null)
        {
            StreamUtilities.ValidateStream(peStream, nameof(peStream), readRequired: true, seekRequired: true);
            using var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen);
            ConvertPortableToWindows(peReader, sourcePdbStream, targetPdbStream, options);
        }

        /// <summary>
        /// Converts Portable PDB stream to Windows PDB.
        /// </summary>
        /// <param name="peReader">PE reader.</param>
        /// <param name="sourcePdbStream">Source stream of Portable PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Windows PDB data. Must be writable.</param>
        /// <param name="options">Conversion options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading/writing.</exception>
        public void ConvertPortableToWindows(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream, PortablePdbConversionOptions? options = null)
        {
            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);

            using var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(sourcePdbStream, MetadataStreamOptions.LeaveOpen);
            ConvertPortableToWindows(peReader, pdbReaderProvider.GetMetadataReader(), targetPdbStream, options);
        }

        /// <summary>
        /// Converts Portable PDB to Windows PDB.
        /// </summary>
        /// <param name="peReader">PE reader.</param>
        /// <param name="pdbReader">Portable PDB reader.</param>
        /// <param name="targetPdbStream">Target stream of Windows PDB data. Must be writable.</param>
        /// <param name="options">Conversion options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/>, <paramref name="pdbReader"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading/writing.</exception>
        public void ConvertPortableToWindows(PEReader peReader, MetadataReader pdbReader, Stream targetPdbStream, PortablePdbConversionOptions? options = null)
        {
            if (pdbReader == null)
            {
                throw new ArgumentNullException(nameof(pdbReader));
            }

            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            using var pdbWriter = SymUnmanagedWriterFactory.CreateWriter(new SymMetadataProvider(peReader.GetMetadataReader()), (options ?? PortablePdbConversionOptions.Default).WriterCreationOptions);
            ConvertPortableToWindows(peReader, pdbReader, pdbWriter, options);
            pdbWriter.WriteTo(targetPdbStream);
        }

        /// <summary>
        /// Converts Portable PDB stream to Windows PDB.
        /// </summary>
        /// <param name="peReader">PE reader.</param>
        /// <param name="pdbReader">Portable PDB reader.</param>
        /// <param name="pdbWriter">PDB writer.</param>
        /// <param name="options">Conversion options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peReader"/>, <paramref name="pdbReader"/>, or <paramref name="pdbWriter"/> is null.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">The PDB doesn't match the CodeView Debug Directory record in the PE image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public void ConvertPortableToWindows(PEReader peReader, MetadataReader pdbReader, SymUnmanagedWriter pdbWriter, PortablePdbConversionOptions? options = null)
        {
            new PdbConverterPortableToWindows(_diagnosticReporter).Convert(
                peReader ?? throw new ArgumentNullException(nameof(peReader)),
                pdbReader ?? throw new ArgumentNullException(nameof(pdbReader)),
                pdbWriter ?? throw new ArgumentNullException(nameof(pdbWriter)),
                options ?? PortablePdbConversionOptions.Default);
        }
    }
}
