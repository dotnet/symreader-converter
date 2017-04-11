// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.Tools
{
    public class PdbConverter
    {
        public static readonly PdbConverter Default = new PdbConverter();

        public PdbConverter()
        {
        }

        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ObjectDisposedException"/>
        public static bool IsPortable(Stream pdbStream)
        {
            StreamUtilities.ValidateStream(pdbStream, nameof(pdbStream), readRequired: true, seekRequired: true);
            return SymReaderFactory.IsPortable(pdbStream);
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
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
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
        /// <exception cref="BadImageFormatException">The format of the PE image is invalid.</exception>
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public void ConvertWindowsToPortable(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);
            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            try
            {
                new PdbConverterWindowsToPortable().Convert(peReader, sourcePdbStream, targetPdbStream);
            }
            catch (COMException e)
            {
                throw new InvalidDataException(string.Format(ConverterResources.InvalidPdbFormat, e.Message), e);
            }
        }
        
        /// <summary>
        /// Converts Portable PDB stream to Windows PDB.
        /// </summary>
        /// <param name="peStream">PE image stream (.dll or .exe)</param>
        /// <param name="sourcePdbStream">Source stream of Portable PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Windows PDB data. Must be writable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="peStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support reading.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public void ConvertPortableToWindows(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            StreamUtilities.ValidateStream(peStream, nameof(peStream), readRequired: true, seekRequired: true);
            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                ConvertPortableToWindows(peReader, sourcePdbStream, targetPdbStream);
            }
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
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public void ConvertPortableToWindows(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream, PdbConversionOptions options = default(PdbConversionOptions))
        {
            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);

            using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(sourcePdbStream, MetadataStreamOptions.LeaveOpen))
            {
                ConvertPortableToWindows(peReader, pdbReaderProvider.GetMetadataReader(), targetPdbStream, options);
            }
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
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public void ConvertPortableToWindows(PEReader peReader, MetadataReader pdbReader, Stream targetPdbStream, PdbConversionOptions options = default(PdbConversionOptions))
        {
            if (pdbReader == null)
            {
                throw new ArgumentNullException(nameof(pdbReader));
            }

            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            using (var pdbWriter = new SymUnmanagedWriter(peReader.GetMetadataReader()))
            {
                ConvertPortableToWindows(peReader, pdbReader, pdbWriter, options);
                pdbWriter.WriteTo(targetPdbStream);
            }
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
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public void ConvertPortableToWindows<TDocumentWriter>(PEReader peReader, MetadataReader pdbReader, PdbWriter<TDocumentWriter> pdbWriter, PdbConversionOptions options)
        {
            new PdbConverterPortableToWindows<TDocumentWriter>().Convert(
                peReader ?? throw new ArgumentNullException(nameof(peReader)), 
                pdbReader ?? throw new ArgumentNullException(nameof(pdbReader)),
                pdbWriter ?? throw new ArgumentNullException(nameof(pdbWriter)), 
                options);
        }
    }
}