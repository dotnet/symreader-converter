# Microsoft.DiaSymReader.Converter

Converts between Windows PDB and [Portable PDB](https://github.com/dotnet/core/blob/master/Documentation/diagnostics/portable_pdb.md) formats.

Pre-release builds are available on MyGet gallery: https://dotnet.myget.org/Gallery/symreader-converter.

## Usage

The converter is available as a command line tool as well as a library. Both are distributed as NuGet packages.

### [Pdb2Pdb](https://dotnet.myget.org/feed/symreader-converter/package/nuget/Pdb2Pdb)

`Pdb2Pdb.exe <dll/exe path> [/pdb <path>] [/out <path>] [/extract]`

| Switch        | Description                                             |
|:--------------|:--------------------------------------------------------|
| `/pdb <path>` | Path to the PDB to convert. If not specified explicitly, the PDB referenced by or embedded in the DLL/EXE is used. |
| `/out <path>` | Output PDB path. |
| `/extract`    | Extract PDB embedded in the DLL/EXE. |
| `/sourcelink` | Only include SourceLink in the converted Windows PDB without converting it to to the legacy `srcsrv` format. By default both SourceLink and `srcsrv` are included in Windows PDB. |
| `/verbose`    | Print detailed diagnostics. |
| `/srcsvrvar <name>=<value>` | Add specified variable to srcsvr stream. Only applicable when converting to Windows PDB and `/sourcelink` is not specified. |

`/extract` and `/pdb` are mutually exclusive.

Example: Create and build .NET Core Standard library and convert its Portable PDB to Windows PDB to be published to Symbol Store.

```
> dotnet new classlib
> dotnet build
> cd bin\Debug\netstandard2.0
> mkdir SymStore
> Pdb2Pdb MyProject.dll /out SymStore\MyProject.pdb
```

### [Microsoft.DiaSymReader.Converter](https://dotnet.myget.org/feed/symreader-converter/package/nuget/Microsoft.DiaSymReader.Converter)

The package provides the following public APIs:

```C#

namespace Microsoft.DiaSymReader.Tools
{
    public class PdbConverter
    {
        /// <summary>
        /// An instance of <see cref="PdbConverter"/> with no diagnostic reporting.
        /// </summary>
        public static readonly PdbConverter Default;
        
        /// <summary>
        /// Creates PDB converter with an optional callback invoked whenever a diagnostic is to be reported.
        /// </summary>
        public PdbConverter(Action<PdbDiagnostic> diagnosticReporter = null);

        /// <summary>
        /// Checks whether given PDB stream has Portable format.
        /// </summary>
        /// <param name="pdbStream">Stream.</param>
        /// <returns>Returns true if the given stream starts with a Portable PDB signature.</returns>
        /// <exception cref="ArgumentException"><paramref name="pdbStream"/> does not support read and seek operations.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="pdbStream"/> is null.</exception>
        /// <exception cref="IOException">IO error while reading from or writing to a stream.</exception>
        /// <exception cref="ObjectDisposedException">Stream has been disposed while reading.</exception>
        public static bool IsPortable(Stream pdbStream);

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
        public void ConvertWindowsToPortable(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream);

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
        public void ConvertWindowsToPortable(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream);

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
        public void ConvertPortableToWindows(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream, PortablePdbConversionOptions options = null);

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
        public void ConvertPortableToWindows(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream, PortablePdbConversionOptions options = null);

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
        public void ConvertPortableToWindows(PEReader peReader, MetadataReader pdbReader, Stream targetPdbStream, PortablePdbConversionOptions options = null);

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
        public void ConvertPortableToWindows(PEReader peReader, MetadataReader pdbReader, SymUnmanagedWriter pdbWriter, PortablePdbConversionOptions options = null);
    }
}

```

## Repository status

[//]: # (Begin current test results)

|          |Windows Debug|Windows Release|
|:--------:|:-----------:|:-------------:|
|**master**|[![Build Status](https://ci2.dot.net/job/dotnet_symreader-converter/job/master/job/windows_debug/badge/icon)](https://ci2.dot.net/job/dotnet_symreader-converter/job/master/job/windows_debug/)|[![Build Status](https://ci2.dot.net/job/dotnet_symreader-converter/job/master/job/windows_release/badge/icon)](https://ci2.dot.net/job/dotnet_symreader-converter/job/master/job/windows_release/)|

[//]: # (End current test results)

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).  For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

