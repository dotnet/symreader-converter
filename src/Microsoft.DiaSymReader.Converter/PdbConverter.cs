// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace Microsoft.DiaSymReader.Tools
{
    public static class PdbConverter
    {
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
        /// Converts Windows PDB stream to Portable PDB and vice versa.
        /// The format is detected automatically.
        /// </summary>
        /// <param name="peStream">PE image stream (.dll or .exe)</param>
        /// <param name="sourcePdbStream">Source stream of Windows PDB data. Must be readable.</param>
        /// <param name="targetPdbStream">Target stream of Portable PDB data. Must be writable.</param>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/>, <paramref name="sourcePdbStream"/>, or <paramref name="targetPdbStream"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="peStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentException"><paramref name="sourcePdbStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetPdbStream"/> does not support writing.</exception>
        /// <exception cref="BadImageFormatException">The format of the PE image or the source PDB image is invalid.</exception>
        /// <exception cref="InvalidDataException">Unexpected data found in the PE image or the source PDB image.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        public static void Convert(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true, seekRequired: true);

            if (SymReaderFactory.IsPortable(sourcePdbStream))
            {
                ConvertPortableToWindows(peStream, sourcePdbStream, targetPdbStream);
            }
            else
            {
                ConvertWindowsToPortable(peStream, sourcePdbStream, targetPdbStream); 
            }
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
        public static void ConvertWindowsToPortable(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            StreamUtilities.ValidateStream(peStream, nameof(peStream), readRequired: true, seekRequired: true);
            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);
            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                try
                {
                    PdbConverterWindowsToPortable.Convert(peReader, sourcePdbStream, targetPdbStream);
                }
                catch (COMException e)
                {
                    throw new BadImageFormatException(string.Format(ConverterResources.InvalidPdbFormat, e.Message), e);
                }
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
        public static void ConvertPortableToWindows(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            StreamUtilities.ValidateStream(peStream, nameof(peStream), readRequired: true, seekRequired: true);
            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            using (var pdbWriter = new SymUnmanagedWriter(peReader.GetMetadataReader()))
            {
                ConvertPortableToWindows(peReader, sourcePdbStream, pdbWriter, PdbConversionOptions.Default);
                pdbWriter.WriteTo(targetPdbStream);
            }
        }

        public static void ConvertPortableToWindows<TDocumentWriter>(PEReader peReader, Stream sourcePdbStream, PdbWriter<TDocumentWriter> pdbWriter, PdbConversionOptions options)
        {
            if (peReader == null)
            {
                throw new ArgumentNullException(nameof(peReader));
            }

            if (pdbWriter == null)
            {
                throw new ArgumentNullException(nameof(pdbWriter));
            }

            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);

            using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(sourcePdbStream, MetadataStreamOptions.LeaveOpen))
            {
                PdbConverterPortableToWindows<TDocumentWriter>.Convert(peReader, pdbReaderProvider.GetMetadataReader(), pdbWriter, options);
            }
        }
    }
}