// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.DiaSymReader.PortablePdb;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    internal static partial class PdbConverterPortableToWindows<TDocumentWriter>
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

        internal static void Convert(PEReader peReader, MetadataReader pdbReader, PdbWriter<TDocumentWriter> pdbWriter)
        {
            var metadataReader = peReader.GetMetadataReader();
            var metadataModel = new MetadataModel(metadataReader);

            var documentWriters = new ArrayBuilder<TDocumentWriter>(pdbReader.Documents.Count);
            var symSequencePointBuilder = new SequencePointsBuilder(capacity: 64);
            var declaredExternAliases = new HashSet<string>();
            var importStringsBuilder = new List<string>();
            var importCountsPerScope = new List<int>();
            var cdiBuilder = new BlobBuilder();
            var dynamicLocals = new List<(string LocalName, byte[] Flags, int Count, int SlotIndex)>();
            var openScopeEndOffsets = new Stack<int>();

            // state for calculating import string forwarding:
            var lastImportScopeHandle = default(ImportScopeHandle);
            var lastImportScopeMethodDefHandle = default(MethodDefinitionHandle);
            var importStringsMap = new Dictionary<ImmutableArray<string>, MethodDefinitionHandle>();

            var aliasedAssemblyRefs = GetAliasedAssemblyRefs(pdbReader);
            // var kickOffMethodToMoveNextMethodMap = GetStateMachineMethodMap(pdbReader);

            string vbDefaultNamespace = MetadataUtilities.GetVisualBasicDefaultNamespace(pdbReader);
            bool vbSemantics = vbDefaultNamespace != null;
            string vbDefaultNamespaceImportString = vbSemantics ? "*" + vbDefaultNamespace : null;

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
            LocalScope? currentLocalScope = NextLocalScope();

            LocalScope? NextLocalScope() => 
                localScopeEnumerator.MoveNext() ? pdbReader.GetLocalScope(localScopeEnumerator.Current) : default(LocalScope?);

            // Handle of the method that is gonna contain list of AssemblyRef aliases.
            // Other methods will forward to it.
            var methodDefHandleWithAssemblyRefAliases = default(MethodDefinitionHandle);

            foreach (var methodDebugInfoHandle in pdbReader.MethodDebugInformation)
            {
                var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodDebugInfoHandle);
                var methodDefHandle = methodDebugInfoHandle.ToDefinitionHandle();
                int methodToken = MetadataTokens.GetToken(methodDefHandle);
                var methodDef = metadataReader.GetMethodDefinition(methodDefHandle);

                void SkipLocalScopes()
                {
                    while (currentLocalScope.HasValue && currentLocalScope.Value.Method == methodDefHandle)
                    {
                        currentLocalScope = NextLocalScope();
                    }
                }

                // methods without method body don't currently have any debug information:
                if (methodDef.RelativeVirtualAddress == 0)
                {
                    SkipLocalScopes();
                    continue;
                }

                bool isKickOffMethod = metadataModel.TryGetStateMachineMoveNextMethod(methodDefHandle, out var moveNextHandle);

                // methods without debug info:
                if (!isKickOffMethod && methodDebugInfo.Document.IsNil && methodDebugInfo.SequencePointsBlob.IsNil)
                {
                    SkipLocalScopes();
                    continue;
                }

                Debug.WriteLine($"Open Method '{metadataReader.GetString(methodDef.Name)}' {methodToken:X8}");
                pdbWriter.OpenMethod(methodToken);

                var methodBody = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                int methodBodyLength = methodBody.GetILReader().Length;

                var forwardImportScopesToMethodDef = default(MethodDefinitionHandle);
                Debug.Assert(dynamicLocals.Count == 0);
                Debug.Assert(openScopeEndOffsets.Count == 0);

                void CloseOpenScopes(int currentScopeStartOffset)
                {
                    // close all open scopes that end before this scope starts:
                    while (openScopeEndOffsets.Count > 0 && currentScopeStartOffset >= openScopeEndOffsets.Peek())
                    {
                        int scopeEnd = openScopeEndOffsets.Pop();
                        Debug.WriteLine($"Close Scope [.., {scopeEnd})");
                        pdbWriter.CloseScope(scopeEnd - (vbSemantics ? 1 : 0));
                    }
                }

                bool isFirstMethodScope = true;
                while (currentLocalScope.HasValue && currentLocalScope.Value.Method == methodDefHandle)
                {
                    var localScope = currentLocalScope.Value;

                    // kickoff methods don't have any scopes emitted to Windows PDBs
                    if (!isKickOffMethod)
                    {
                        CloseOpenScopes(localScope.StartOffset);

                        Debug.WriteLine($"Open Scope [{localScope.StartOffset}, {localScope.EndOffset})");
                        pdbWriter.OpenScope(localScope.StartOffset);
                        openScopeEndOffsets.Push(localScope.EndOffset);

                        if (isFirstMethodScope)
                        {
                            if (lastImportScopeHandle == localScope.ImportScope)
                            {
                                // forward to a method that has the same imports:
                                forwardImportScopesToMethodDef = lastImportScopeMethodDefHandle;
                            }
                            else
                            {
                                Debug.Assert(importStringsBuilder.Count == 0);
                                Debug.Assert(declaredExternAliases.Count == 0);
                                Debug.Assert(importCountsPerScope.Count == 0);

                                AddImportStrings(importStringsBuilder, importCountsPerScope, declaredExternAliases, pdbReader, metadataModel, localScope.ImportScope, aliasedAssemblyRefs, vbDefaultNamespaceImportString);
                                var importStrings = importStringsBuilder.ToImmutableArray();
                                importStringsBuilder.Clear();

                                if (importStringsMap.TryGetValue(importStrings, out forwardImportScopesToMethodDef))
                                {
                                    // forward to a method that has the same imports:
                                    lastImportScopeMethodDefHandle = forwardImportScopesToMethodDef;
                                }
                                else
                                {
                                    // attach import strings to the current method:
                                    WriteImports(pdbWriter, importStrings);
                                    lastImportScopeMethodDefHandle = methodDefHandle;
                                }

                                lastImportScopeHandle = localScope.ImportScope;
                            }

                            if (vbSemantics && !forwardImportScopesToMethodDef.IsNil)
                            {
                                pdbWriter.UsingNamespace("@" + MetadataTokens.GetToken(forwardImportScopesToMethodDef));
                            }

                            // This is the method that's gonna have AssemblyRef aliases attached:
                            if (methodDefHandleWithAssemblyRefAliases.IsNil)
                            {
                                foreach (var (assemblyRefHandle, alias) in aliasedAssemblyRefs)
                                {
                                    var assemblyRef = metadataReader.GetAssemblyReference(assemblyRefHandle);
                                    pdbWriter.UsingNamespace("Z" + alias + " " + AssemblyDisplayNameBuilder.GetAssemblyDisplayName(metadataReader, assemblyRef));
                                }
                            }
                        }

                        foreach (var localConstantHandle in localScope.GetLocalConstants())
                        {
                            var constant = pdbReader.GetLocalConstant(localConstantHandle);
                            string name = pdbReader.GetString(constant.Name);

                            if (name.Length > MaxEntityNameLength)
                            {
                                // TODO: report warning
                                continue;
                            }

                            var (value, signature) = PortableConstantSignature.GetConstantValueAndSignature(pdbReader, localConstantHandle, metadataReader.GetQualifiedTypeName);
                            if (!metadataModel.TryGetStandaloneSignatureHandle(signature, out var constantSignatureHandle))
                            {
                                // Signature will be unspecified. At least we store the name and the value.
                                constantSignatureHandle = default(StandaloneSignatureHandle);
                            }

                            pdbWriter.DefineLocalConstant(name, value, MetadataTokens.GetToken(constantSignatureHandle));

                            var dynamicFlags = MetadataUtilities.ReadDynamicCustomDebugInformation(pdbReader, localConstantHandle);
                            if (TryGetDynamicLocal(name, 0, dynamicFlags, out var dynamicLocal))
                            {
                                dynamicLocals.Add(dynamicLocal);
                            }
                        }

                        foreach (var localVariableHandle in localScope.GetLocalVariables())
                        {
                            var variable = pdbReader.GetLocalVariable(localVariableHandle);
                            string name = pdbReader.GetString(variable.Name);

                            if (name.Length > MaxEntityNameLength)
                            {
                                // TODO: report warning
                                continue;
                            }

                            // TODO: translate dummy VB hosited state machine locals to hosted scopes

                            if (methodBody.LocalSignature.IsNil)
                            {
                                // TODO: report warning
                                continue;
                            }

                            pdbWriter.DefineLocalVariable(variable.Index, name, variable.Attributes, MetadataTokens.GetToken(methodBody.LocalSignature));

                            var dynamicFlags = MetadataUtilities.ReadDynamicCustomDebugInformation(pdbReader, localVariableHandle);
                            if (TryGetDynamicLocal(name, variable.Index, dynamicFlags, out var dynamicLocal))
                            {
                                dynamicLocals.Add(dynamicLocal);
                            }
                        }
                    }

                    currentLocalScope = NextLocalScope();
                    isFirstMethodScope = false;
                }

                CloseOpenScopes(methodBodyLength);
                if (openScopeEndOffsets.Count > 0)
                {
                    // TODO: report warning: scope range exceeds size of the method body
                    openScopeEndOffsets.Clear();
                }

                WriteSequencePoints(pdbWriter, documentWriters, symSequencePointBuilder, methodDebugInfo.GetSequencePoints());

                // async method data:
                var asyncData = MetadataUtilities.ReadAsyncMethodData(pdbReader, methodDebugInfoHandle);
                if (!asyncData.IsNone)
                {
                    pdbWriter.SetAsyncInfo(
                        moveNextMethodToken: methodToken,
                        kickoffMethodToken: MetadataTokens.GetToken(asyncData.KickoffMethod),
                        catchHandlerOffset: asyncData.CatchHandlerOffset,
                        yieldOffsets: asyncData.YieldOffsets.ToArray(),
                        resumeOffsets: asyncData.ResumeOffsets.ToArray());
                }

                // custom debug information:  
                var cdiEncoder = new CustomDebugInfoEncoder(cdiBuilder);
                if (isKickOffMethod)
                {
                    cdiEncoder.AddStateMachineTypeName(GetIteratorTypeName(metadataReader, moveNextHandle));
                }
                else
                {
                    if (!vbSemantics)
                    {
                        if (forwardImportScopesToMethodDef.IsNil)
                        {
                            // record the number of import strings in each scope:
                            cdiEncoder.AddUsingGroups(importCountsPerScope);

                            if (!methodDefHandleWithAssemblyRefAliases.IsNil)
                            {
                                // forward assembly ref aliases to the first method:
                                cdiEncoder.AddForwardModuleInfo(methodDefHandleWithAssemblyRefAliases);
                            }
                        }
                        else
                        {
                            // forward all imports to another method:
                            cdiEncoder.AddForwardMethodInfo(forwardImportScopesToMethodDef);
                        }
                    }

                    var hoistedLocalScopes = GetStateMachineHoistedLocalScopes(pdbReader, methodDefHandle);
                    if (!hoistedLocalScopes.IsDefault)
                    {
                        cdiEncoder.AddStateMachineHoistedLocalScopes(hoistedLocalScopes);
                    }

                    if (dynamicLocals.Count > 0)
                    {
                        cdiEncoder.AddDynamicLocals(dynamicLocals);
                        dynamicLocals.Clear();
                    }
                }

                // the following blobs map 1:1
                CopyCustomDebugInfoRecord(ref cdiEncoder, pdbReader, methodDefHandle, PortableCustomDebugInfoKinds.TupleElementNames, CustomDebugInfoKind.TupleElementNames);
                CopyCustomDebugInfoRecord(ref cdiEncoder, pdbReader, methodDefHandle, PortableCustomDebugInfoKinds.EncLocalSlotMap, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
                CopyCustomDebugInfoRecord(ref cdiEncoder, pdbReader, methodDefHandle, PortableCustomDebugInfoKinds.EncLambdaAndClosureMap, CustomDebugInfoKind.EditAndContinueLambdaMap);

                if (cdiEncoder.RecordCount > 0)
                {
                    pdbWriter.DefineCustomMetadata(cdiEncoder.ToArray());
                }

                cdiBuilder.Clear();

                if (!isKickOffMethod && methodDefHandleWithAssemblyRefAliases.IsNil)
                {
                    methodDefHandleWithAssemblyRefAliases = methodDefHandle;
                }

                Debug.WriteLine($"Close Method {methodToken:X8}");
                pdbWriter.CloseMethod();
            }
        }

        private static bool TryGetDynamicLocal(string name, int slotIndex, ImmutableArray<bool> flagsOpt, out (string LocalName, byte[] Flags, int Count, int SlotIndex) result)
        {
            if (flagsOpt.IsDefaultOrEmpty ||
                flagsOpt.Length > CustomDebugInfoEncoder.DynamicAttributeSize &&
                name.Length >= CustomDebugInfoEncoder.IdentifierSize)
            {
                result = default((string, byte[], int, int));
                return false;
            }

            var bytes = new byte[CustomDebugInfoEncoder.DynamicAttributeSize];
            for (int k = 0; k < flagsOpt.Length; k++)
            {
                if (flagsOpt[k])
                {
                    bytes[k] = 1;
                }
            }

            result = (name, bytes, flagsOpt.Length, slotIndex);
            return true;
        }

        private static void CopyCustomDebugInfoRecord(
            ref CustomDebugInfoEncoder cdiEncoder, 
            MetadataReader pdbReader,
            MethodDefinitionHandle methodDefHandle,
            Guid portableKind,
            CustomDebugInfoKind windowsKind)
        {
            var cdiHandle = pdbReader.GetCustomDebugInformation(methodDefHandle, portableKind);
            if (!cdiHandle.IsNil)
            {
                var bytes = pdbReader.GetBlobBytes(cdiHandle);
                cdiEncoder.AddRecord(
                    windowsKind,
                    bytes,
                    (b, builder) => builder.WriteBytes(b));
            }
        }

        private static ImmutableArray<StateMachineHoistedLocalScope> GetStateMachineHoistedLocalScopes(MetadataReader pdbReader, MethodDefinitionHandle methodDefHandle)
        {
            var cdiHandle = pdbReader.GetCustomDebugInformation(methodDefHandle, PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes);
            if (cdiHandle.IsNil)
            {
                return default(ImmutableArray<StateMachineHoistedLocalScope>);
            }

            return MetadataUtilities.DecodeHoistedLocalScopes(pdbReader.GetBlobReader(cdiHandle));
        }

        private static string GetIteratorTypeName(MetadataReader metadataReader, MethodDefinitionHandle moveNextHandle)
        {
            // TODO: errors
            var moveNextDef = metadataReader.GetMethodDefinition(moveNextHandle);
            var iteratorType = moveNextDef.GetDeclaringType();
            return metadataReader.GetString(metadataReader.GetTypeDefinition(iteratorType).Name);
        }

        private static IReadOnlyDictionary<MethodDefinitionHandle, MethodDefinitionHandle> GetStateMachineMethodMap(MetadataReader pdbReader)
        {
            // TODO: SRM doesn't have better APIs.

            return 
               (from methodHandle in pdbReader.MethodDebugInformation
                let method = pdbReader.GetMethodDebugInformation(methodHandle)
                let kickOffHandle = method.GetStateMachineKickoffMethod()
                where !kickOffHandle.IsNil
                select (key: kickOffHandle, value: methodHandle.ToDefinitionHandle())).ToDictionary(pair => pair.key, pair => pair.value);
        }

        private static void WriteImports(PdbWriter<TDocumentWriter> pdbWriter, ImmutableArray<string> importStrings)
        {
            foreach (var importString in importStrings)
            {
                pdbWriter.UsingNamespace(importString);
            }
        }

        private static void AddImportStrings(
            List<string> importStrings,
            List<int> importCountsPerScope,
            HashSet<string> declaredExternAliases,
            MetadataReader pdbReader,
            MetadataModel metadataModel,
            ImportScopeHandle importScopeHandle,
            ImmutableArray<(AssemblyReferenceHandle, string)> aliasedAssemblyRefs,
            string vbDefaultNamespaceImportStringOpt)
        {
            Debug.Assert(declaredExternAliases.Count == 0);
            AddExternAliases(declaredExternAliases, pdbReader, importScopeHandle);

            bool vbSemantics = vbDefaultNamespaceImportStringOpt != null;

            while (!importScopeHandle.IsNil)
            {
                var importScope = pdbReader.GetImportScope(importScopeHandle);
                bool isProjectLevel = importScope.Parent.IsNil;

                if (isProjectLevel && vbDefaultNamespaceImportStringOpt != null)
                {
                    importStrings.Add(vbDefaultNamespaceImportStringOpt);
                }

                int importStringCount = 0;
                foreach (var import in importScope.GetImports())
                {
                    var importString = TryEncodeImport(pdbReader, metadataModel, import, declaredExternAliases, aliasedAssemblyRefs, isProjectLevel, vbSemantics);
                    if (importString == null)
                    {
                        // diagnostic already reported if applicable
                        continue;
                    }

                    if (importString.Length > MaxEntityNameLength)
                    {
                        // TODO: warning
                        continue;
                    }

                    importStrings.Add(importString);
                    importStringCount++;
                }

                // Skip C# project-level scope if it doesn't include namespaces.
                // Currently regular (non-scripting) C# doesn't support project-level namespace imports.
                if (vbSemantics || !isProjectLevel || importStringCount > 0)
                {
                    importCountsPerScope.Add(importStringCount);
                }

                importScopeHandle = importScope.Parent;
            }

            declaredExternAliases.Clear();
            aliasedAssemblyRefs.Clear();
        }

        private static void AddExternAliases(HashSet<string> externAliases, MetadataReader pdbReader, ImportScopeHandle importScopeHandle)
        {
            while (!importScopeHandle.IsNil)
            {
                var importScope = pdbReader.GetImportScope(importScopeHandle);

                foreach (var import in importScope.GetImports())
                {
                    if (import.Kind == ImportDefinitionKind.ImportAssemblyReferenceAlias)
                    {
                        externAliases.Add(pdbReader.GetStringUTF8(import.Alias));
                    }
                }

                importScopeHandle = importScope.Parent;
            }
        }

        private static ImmutableArray<(AssemblyReferenceHandle, string)> GetAliasedAssemblyRefs( MetadataReader pdbReader)
        {
            // C# serialized aliased assembly refs to the first import scope.
            // In Windows PDBs they are attached as CDIs to any method in the assembly and the other methods 
            // have CDI that forwards to it.
            return (from import in pdbReader.GetImportScope(MetadataTokens.ImportScopeHandle(1)).GetImports()
                    where import.Kind == ImportDefinitionKind.AliasAssemblyReference
                    select (import.TargetAssembly, pdbReader.GetStringUTF8(import.Alias))).ToImmutableArray();
        }

        private static string TryEncodeImport(
            MetadataReader pdbReader, 
            MetadataModel metadataModel, 
            ImportDefinition import,
            HashSet<string> declaredExternAliases,
            ImmutableArray<(AssemblyReferenceHandle, string)> aliasedAssemblyRefs,
            bool isProjectLevel,
            bool vbSemantics)
        {
            string typeName, namespaceName;

            // See Roslyn implementation: PdbWriter.TryEncodeImport, MetadataWriter.SerializeImport
            switch (import.Kind)
            {
                case ImportDefinitionKind.AliasType:
                case ImportDefinitionKind.ImportType:
                    // C#, VB

                    // VB doesn't support generics in imports:
                    if (vbSemantics && import.TargetType.Kind == HandleKind.TypeSpecification)
                    {
                        return null;
                    }

                    typeName = metadataModel.GetSerializedTypeName(import.TargetType);

                    if (vbSemantics)
                    {
                        if (import.Kind == ImportDefinitionKind.AliasType)
                        {
                            return (isProjectLevel ? "@PA:" : "@FA:") + pdbReader.GetStringUTF8(import.Alias) + "=" + typeName;
                        }
                        else
                        {
                            return (isProjectLevel ? "@PT:" : "@FT:") + typeName;
                        }
                    }
                    else
                    {
                        if (import.Kind == ImportDefinitionKind.AliasType)
                        {
                            return "A" + pdbReader.GetStringUTF8(import.Alias) + " T" + typeName;
                        }
                        else
                        {
                            return "T" + typeName;
                        }
                    }

                case ImportDefinitionKind.AliasNamespace:
                    // C#, VB
                    namespaceName = pdbReader.GetStringUTF8(import.TargetNamespace);
                    if (vbSemantics)
                    {
                        return (isProjectLevel ? "@P:" : "@F:") + namespaceName;
                    }
                    else
                    {
                        return "A" + pdbReader.GetStringUTF8(import.Alias) + " U" + namespaceName;
                    }

                case ImportDefinitionKind.ImportNamespace:
                    // C#, VB
                    namespaceName = pdbReader.GetStringUTF8(import.TargetNamespace);
                    if (vbSemantics)
                    {
                        return (isProjectLevel ? "@PA:" : "@FA:") + pdbReader.GetStringUTF8(import.Alias) + "=" + namespaceName;
                    }
                    else
                    {
                        return "U" + namespaceName;
                    }

                case ImportDefinitionKind.ImportXmlNamespace:
                    // VB
                    return (isProjectLevel ? "@PX:" : "@FX:") + pdbReader.GetStringUTF8(import.Alias) + "=" + pdbReader.GetStringUTF8(import.TargetNamespace);

                case ImportDefinitionKind.ImportAssemblyReferenceAlias:
                    // C#
                    return "X" + pdbReader.GetStringUTF8(import.Alias);

                case ImportDefinitionKind.AliasAssemblyNamespace:
                case ImportDefinitionKind.ImportAssemblyNamespace:
                    // C#
                    string assemblyRefAlias = TryGetAssemblyReferenceAlias(import.TargetAssembly, declaredExternAliases, aliasedAssemblyRefs);
                    if (assemblyRefAlias == null)
                    {
                        // TODO: report error
                        return null;
                    }

                    namespaceName = pdbReader.GetStringUTF8(import.TargetNamespace);

                    if (import.Kind == ImportDefinitionKind.AliasAssemblyNamespace)
                    {
                        return "A" + pdbReader.GetStringUTF8(import.Alias) + " " + "E" + namespaceName + " " + assemblyRefAlias;
                    }
                    else
                    {
                        return "E" + namespaceName + " " + assemblyRefAlias;
                    }

                case ImportDefinitionKind.AliasAssemblyReference:
                    // C#: aliased assembly references collected upfront and encoded separately:
                    return null;

                default:
                    // TODO: report error
                    return null;
            }
        }

        private static string TryGetAssemblyReferenceAlias(
            AssemblyReferenceHandle targetAssembly, 
            HashSet<string> declaredExternAliases, 
            ImmutableArray<(AssemblyReferenceHandle, string)> aliasedAssemblyRefs)
        {
            // See Roslyn PdbWriter.GetAssemblyReferenceAlias:
            // Multiple aliases may be given to an assembly reference.
            // We find one that is in scope (was imported via extern alias directive).
            // If multiple are in scope then use the first one.

            foreach (var (assemblyRefHandle, alias) in aliasedAssemblyRefs)
            {
                if (targetAssembly == assemblyRefHandle && declaredExternAliases.Contains(alias))
                {
                    return alias;
                }
            }

            return null;
        }

        private static void WriteSequencePoints(
            PdbWriter<TDocumentWriter> pdbWriter, 
            ArrayBuilder<TDocumentWriter> documentWriters, 
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
                    if (currentDocumentWriterIndex >= 0)
                    {
                        symSequencePointBuilder.WriteSequencePoints(pdbWriter, documentWriters[currentDocumentWriterIndex]);
                    }

                    currentDocumentWriterIndex = documentWriterIndex;
                }

                symSequencePointBuilder.Add(
                    offset: sequencePoint.Offset,
                    startLine: sequencePoint.StartLine,
                    startColumn: sequencePoint.StartColumn,
                    endLine: sequencePoint.EndLine,
                    endColumn: sequencePoint.EndColumn);
            }

            if (currentDocumentWriterIndex >= 0)
            {
                symSequencePointBuilder.WriteSequencePoints(pdbWriter, documentWriters[currentDocumentWriterIndex]);
            }
        }
    }
}
