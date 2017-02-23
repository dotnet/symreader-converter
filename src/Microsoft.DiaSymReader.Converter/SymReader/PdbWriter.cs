// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader
{
    // TODO: Similar to PdbWriter from Roslyn. Share some bits.

    internal sealed class PdbWriter : IDisposable
    {
        private static object s_zeroInt32 = 0;

        private SymReaderMetadataImport _metadataImport;
        private ISymUnmanagedWriter7 _symWriter;
        private ComMemoryStream _pdbStream;

        public PdbWriter(MetadataReader metadataReader)
        {
            _pdbStream = new ComMemoryStream();
            _metadataImport = new SymReaderMetadataImport(metadataReader, metadataOwnerOpt: null);
            _symWriter = SymReaderFactory.CreateWindowsPdbWriter(_pdbStream, _metadataImport);
        }

        public SymReaderMetadataImport MetadataImport => _metadataImport;

        public void WriteTo(Stream stream)
        {
            Debug.Assert(_pdbStream != null);
            Debug.Assert(_symWriter != null);

            try
            {
                // SymWriter flushes data to the native stream on close:
                _symWriter.Close();
                _symWriter = null;
                _pdbStream.CopyTo(stream);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Close();
        }

        ~PdbWriter()
        {
            Close();
        }

        private void Close()
        {
            try
            {
                _symWriter?.Close();
                _metadataImport?.Dispose();
                _metadataImport = null;
                _symWriter = null;
                _pdbStream = null;
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public ISymUnmanagedDocumentWriter DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, byte[] checksum)
        {
            ISymUnmanagedDocumentWriter writer;

            try
            {
                writer = _symWriter.DefineDocument(name, ref language, ref vendor, ref type);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }

            if (algorithmId != default(Guid) && checksum.Length > 0)
            {
                try
                {
                    writer.SetCheckSum(algorithmId, (uint)checksum.Length, checksum);
                }
                catch (Exception ex)
                {
                    throw new PdbWritingException(ex);
                }
            }

            return writer;
        }

        public void DefineSequencePoints(ISymUnmanagedDocumentWriter symDocument, int count, int[] offsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns)
        {
            try
            {
                _symWriter.DefineSequencePoints(
                    symDocument,
                    count,
                    offsets,
                    startLines,
                    startColumns,
                    endLines,
                    endColumns);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void OpenMethod(int methodToken)
        {
            try
            {
                _symWriter.OpenMethod((uint)methodToken);

                // open outermost scope:
                _symWriter.OpenScope(startOffset: 0);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void CloseMethod(int ilLength)
        {
            try
            {
                // close the root scope:
                CloseScope(endOffset: ilLength);

                _symWriter.CloseMethod();
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void OpenScope(int offset)
        {
            try
            {
                _symWriter.OpenScope((uint)offset);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void CloseScope(int endOffset)
        {
            try
            {
                _symWriter.CloseScope((uint)endOffset);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void DefineLocalVariable(int index, string name, LocalVariableAttributes attributes, int localSignatureToken)
        {
            const uint ADDR_IL_OFFSET = 1;
            try
            {
                _symWriter.DefineLocalVariable2(name, (uint)attributes, localSignatureToken, ADDR_IL_OFFSET, index, 0, 0, 0, 0);
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }

        public void DefineLocalConstant(string name, object value, int constantSignatureToken)
        {
            switch (value)
            {
                case string str:
                    DefineLocalStringConstant(name, str, constantSignatureToken);
                    break;

                case DateTime dateTime:
                    // Note: Do not use DefineConstant as it doesn't set the local signature token, which is required in order to avoid callbacks to IMetadataEmit.

                    // Marshal.GetNativeVariantForObject would create a variant with type VT_DATE and value equal to the
                    // number of days since 1899/12/30.  However, ConstantValue::VariantFromConstant in the native VB
                    // compiler actually created a variant with type VT_DATE and value equal to the tick count.
                    // http://blogs.msdn.com/b/ericlippert/archive/2003/09/16/eric-s-complete-guide-to-vt-date.aspx
                    _symWriter.DefineConstant2(name, new VariantStructure(dateTime), constantSignatureToken);
                    break;

                default:
                    try
                    {
                        // ISymUnmanagedWriter2.DefineConstant2 throws an ArgumentException
                        // if you pass in null - Dev10 appears to use 0 instead.
                        // (See EMITTER::VariantFromConstVal)
                        DefineLocalConstantImpl(name, value ?? s_zeroInt32, constantSignatureToken);
                    }
                    catch (Exception ex)
                    {
                        throw new PdbWritingException(ex);
                    }
                    break;
            }
        }

        private unsafe void DefineLocalConstantImpl(string name, object value, int constantSignatureToken)
        {
            VariantStructure variant = new VariantStructure();
#pragma warning disable CS0618 // Type or member is obsolete
            Marshal.GetNativeVariantForObject(value, new IntPtr(&variant));
#pragma warning restore CS0618 // Type or member is obsolete
            _symWriter.DefineConstant2(name, variant, constantSignatureToken);
        }

        private void DefineLocalStringConstant(string name, string value, int constantSignatureToken)
        {
            Debug.Assert(value != null);

            int encodedLength;

            // ISymUnmanagedWriter2 doesn't handle unicode strings with unmatched unicode surrogates.
            // We use the .NET UTF8 encoder to replace unmatched unicode surrogates with unicode replacement character.

            if (!MetadataHelpers.IsValidUnicodeString(value))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                encodedLength = bytes.Length;
                value = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            }
            else
            {
                encodedLength = Encoding.UTF8.GetByteCount(value);
            }

            // +1 for terminating NUL character
            encodedLength++;

            // If defining a string constant and it is too long (length limit is not documented by the API), DefineConstant2 throws an ArgumentException.
            // However, diasymreader doesn't calculate the length correctly in presence of NUL characters in the string.
            // Until that's fixed we need to check the limit ourselves. See http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/178988
            if (encodedLength > 2032)
            {
                return;
            }

            try
            {
                DefineLocalConstantImpl(name, value, constantSignatureToken);
            }
            catch (ArgumentException)
            {
                // writing the constant value into the PDB failed because the string value was most probably too long.
                // We will report a warning for this issue and continue writing the PDB. 
                // The effect on the debug experience is that the symbol for the constant will not be shown in the local
                // window of the debugger. Nor will the user be able to bind to it in expressions in the EE.

                //The triage team has deemed this new warning undesirable. The effects are not significant. The warning
                //is showing up in the DevDiv build more often than expected. We never warned on it before and nobody cared.
                //The proposed warning is not actionable with no source location.
            }
            catch (Exception ex)
            {
                throw new PdbWritingException(ex);
            }
        }
    }
}
