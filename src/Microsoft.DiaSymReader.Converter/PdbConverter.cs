// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System;

namespace Microsoft.DiaSymReader.Tools
{
    public static class PdbConverter
    {
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ObjectDisposedException"/>
        public static bool IsPortable(Stream pdbStream) => SymReaderFactory.IsPortable(pdbStream);

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
            StreamUtilities.ValidateStream(peStream, nameof(peStream), readRequired: true, seekRequired: true);
            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true, seekRequired: true);
            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            if (SymReaderFactory.IsPortable(sourcePdbStream))
            {
                PdbConverterPortableToWindows.Convert(peStream, sourcePdbStream, targetPdbStream);
            }
            else
            {
                PdbConverterWindowsToPortable.Convert(peStream, sourcePdbStream, targetPdbStream);
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

            PdbConverterWindowsToPortable.Convert(peStream, sourcePdbStream, targetPdbStream);
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
            StreamUtilities.ValidateStream(sourcePdbStream, nameof(sourcePdbStream), readRequired: true);
            StreamUtilities.ValidateStream(targetPdbStream, nameof(targetPdbStream), writeRequired: true);

            PdbConverterPortableToWindows.Convert(peStream, sourcePdbStream, targetPdbStream);
        }
    }
}