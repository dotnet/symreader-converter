// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.DiaSymReader.PortablePdb;

namespace Microsoft.DiaSymReader.Tools
{
    internal static partial class PdbConverterPortableToWindows
    {
        private static readonly Guid s_languageVendorMicrosoft = new Guid("{994b45c4-e6e9-11d2-903f-00c04fa302a1}");
        private static readonly Guid s_documentTypeText = new Guid("{5a869d0b-6611-11d3-bd2a-0000f80849bd}");

        private static readonly Guid s_csharpGuid = new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1");
        private static readonly Guid s_visualBasicGuid = new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
        private static readonly Guid s_fsharpGuid = new Guid("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3");
        
        /// <summary>
        /// This is the maximum length of a string in the PDB, assuming it is in UTF-8 format 
        /// and not (yet) null-terminated.
        /// </summary>
        /// <remarks>
        /// Used for import strings, locals, and local constants.
        /// </remarks>
        private const int MaxEntityNameLength = 2046;

        private static Guid GetLanguageVendorGuid(Guid languageGuid)
        {
            return (languageGuid == s_csharpGuid || languageGuid == s_visualBasicGuid || languageGuid == s_fsharpGuid) ?
                s_languageVendorMicrosoft : default(Guid);
        }

        public static void Convert(Stream peStream, Stream sourcePdbStream, Stream targetPdbStream)
        {
            using (var peReader = new PEReader(peStream))
            using (var pdbReaderProvider = MetadataReaderProvider.FromPortablePdbStream(sourcePdbStream))
            using (var pdbWriter = new PdbWriter(peReader.GetMetadataReader()))
            {
                var metadataReader = peReader.GetMetadataReader();
                var metadataModel = new MetadataModel(metadataReader);

                var pdbReader = pdbReaderProvider.GetMetadataReader();

                var documentWriters = new ArrayBuilder<ISymUnmanagedDocumentWriter>(pdbReader.Documents.Count);
                var symSequencePointBuilder = new SequencePointsBuilder(capacity: 64);

                foreach (var documentHandle in pdbReader.Documents)
                {
                    var document = pdbReader.GetDocument(documentHandle);
                    var languageGuid = pdbReader.GetGuid(document.Language);

                    documentWriters.Add(pdbWriter.DefineDocument(
                        name: pdbReader.GetString(document.Name),
                        language: languageGuid,
                        type: s_documentTypeText,
                        vendor: GetLanguageVendorGuid(languageGuid),
                        algorithmId: pdbReader.GetGuid(document.HashAlgorithm),
                        checksum: pdbReader.GetBlobBytes(document.Hash)));
                }

                var localScopeEnumerator = pdbReader.LocalScopes.GetEnumerator();
                LocalScope currentLocalScope = NextLocalScope();

                LocalScope NextLocalScope() => 
                    localScopeEnumerator.MoveNext() ? pdbReader.GetLocalScope(localScopeEnumerator.Current) : default(LocalScope);

                foreach (var methodDebugInfoHandle in pdbReader.MethodDebugInformation)
                {
                    var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodDebugInfoHandle);
                    var methodDefHandle = methodDebugInfoHandle.ToDefinitionHandle();
                    int methodToken = MetadataTokens.GetToken(methodDefHandle);
                    var methodDef = metadataReader.GetMethodDefinition(methodDefHandle);

                    // methods without debug info:
                    if ( methodDebugInfo.Document.IsNil && methodDebugInfo.SequencePointsBlob.IsNil)
                    {
                        continue;
                    }

                    // methods without method body don't currently have any debug information:
                    if (methodDef.RelativeVirtualAddress == 0)
                    {
                        continue;
                    }

                    var methodBody = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);

                    pdbWriter.OpenMethod(methodToken);

                    if (currentLocalScope.Method == methodDefHandle)
                    {
                        foreach (var localConstantHandle in currentLocalScope.GetLocalConstants())
                        {
                            var constant = pdbReader.GetLocalConstant(localConstantHandle);
                            string name = pdbReader.GetString(constant.Name);

                            if (name.Length > MaxEntityNameLength)
                            {
                                // TODO: report warning
                                continue;
                            }

                            var (value, signature) = PortableConstantSignature.GetConstantValueAndSignature(pdbReader, localConstantHandle, pdbWriter.MetadataImport);
                            if (!metadataModel.TryGetStandaloneSignatureHandle(signature, out var constantSignatureHandle))
                            {
                                // TODO: report warning

                                // TODO: 
                                // Currently the EEs require signature to match exactly the type of the value. 
                                // We could relax that and use the type of the value regardless of the signature for primitive types.
                                // Then we could use any signature here.
                                continue;
                            }

                            pdbWriter.DefineLocalConstant(name, value, MetadataTokens.GetToken(constantSignatureHandle));
                        }

                        foreach (var localVariableHandle in currentLocalScope.GetLocalVariables())
                        {
                            var variable = pdbReader.GetLocalVariable(localVariableHandle);
                            string name = pdbReader.GetString(variable.Name);

                            if (name.Length > MaxEntityNameLength)
                            {
                                // TODO: report warning
                                continue;
                            }

                            int localSignatureToken = methodBody.LocalSignature.IsNil ? 0 : MetadataTokens.GetToken(methodBody.LocalSignature);
                            pdbWriter.DefineLocalVariable(variable.Index, name, variable.Attributes, localSignatureToken);
                        }

                        currentLocalScope = NextLocalScope();
                    }

                    // TODO: local scopes
                    // TODO: import scopes

                    WriteSequencePoints(pdbWriter, documentWriters, symSequencePointBuilder, methodDebugInfo.GetSequencePoints());

                    // TODO: async info

                    // TODO: CDI

                    // TODO: Assembly-level namespace imports

                    pdbWriter.CloseMethod(methodBody.GetILReader().Length);
                }

                pdbWriter.WriteTo(targetPdbStream);
            }
        }

        private static void WriteSequencePoints(
            PdbWriter pdbWriter, 
            ArrayBuilder<ISymUnmanagedDocumentWriter> documentWriters, 
            SequencePointsBuilder symSequencePointBuilder, 
            SequencePointCollection sequencePoints)
        {
            int currentDocumentWriterIndex = -1;
            foreach (var sequencePoint in sequencePoints)
            {
                int documentWriterIndex = MetadataTokens.GetRowNumber(sequencePoint.Document) - 1;
                if (documentWriterIndex > documentWriters.Count)
                {
                    // TODO: message
                    throw new BadImageFormatException();
                }

                if (currentDocumentWriterIndex != documentWriterIndex)
                {
                    symSequencePointBuilder.WriteSequencePoints(pdbWriter, documentWriters[currentDocumentWriterIndex]);
                    currentDocumentWriterIndex = documentWriterIndex;
                }

                symSequencePointBuilder.Add(
                    offset: sequencePoint.Offset,
                    startLine: sequencePoint.StartLine,
                    startColumn: sequencePoint.StartColumn,
                    endLine: sequencePoint.EndLine,
                    endColumn: sequencePoint.EndColumn);
            }

            if (currentDocumentWriterIndex > 0)
            {
                symSequencePointBuilder.WriteSequencePoints(pdbWriter, documentWriters[currentDocumentWriterIndex]);
            }
        }
    }
}
