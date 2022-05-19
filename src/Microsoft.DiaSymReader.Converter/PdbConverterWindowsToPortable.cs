// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader.PortablePdb;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed partial class PdbConverterWindowsToPortable
    {
        private readonly Action<PdbDiagnostic>? _diagnosticReporter;

        public PdbConverterWindowsToPortable(Action<PdbDiagnostic>? diagnosticReporter)
        {
            _diagnosticReporter = diagnosticReporter;
        }

        private void ReportDiagnostic(PdbDiagnosticId id, int token, params object[] args)
        {
            _diagnosticReporter?.Invoke(new PdbDiagnostic(id, token, args));
        }

        /// <exception cref="COMException">Invalid PDB format.</exception>
        /// <exception cref="InvalidDataException">The content of <paramref name="sourcePdbStream"/> doesn't match the CodeView Debug Directory record in the PE image.</exception>
        public void Convert(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream, SymUnmanagedReaderCreationOptions readerCreationOptions)
        {
            if (!SymReaderHelpers.TryReadPdbId(peReader, out var pdbId, out int age))
            {
                throw new InvalidDataException(ConverterResources.SpecifiedPEFileHasNoAssociatedPdb);
            }

            ISymUnmanagedReader5? symReader = null;
            try
            {
                symReader = SymReaderHelpers.CreateWindowsPdbReader(sourcePdbStream, peReader, readerCreationOptions);

                Marshal.ThrowExceptionForHR(symReader.MatchesModule(pdbId.Guid, pdbId.Stamp, age, out bool isMatching));
                if (!isMatching)
                {
                    throw new InvalidDataException(ConverterResources.PdbNotMatchingDebugDirectory);
                }

                Convert(symReader, peReader, targetPdbStream, pdbId);
            }
            finally
            {
                _ = (((ISymUnmanagedDispose?)symReader)?.Destroy());
            }
        }

        private void Convert(ISymUnmanagedReader5 symReader, PEReader peReader, Stream targetPdbStream, BlobContentId pdbId)
        {
            var metadataBuilder = new MetadataBuilder();
            var documents = symReader.GetDocuments();
            bool vbSemantics = documents.Any(d => d.GetLanguage() == SymReaderHelpers.VisualBasicLanguageGuid);

            var metadataReader = peReader.GetMetadataReader();
            var metadataModel = new MetadataModel(metadataReader, vbSemantics);

            var typeSystemRowCounts = metadataModel.GetRowCounts();
            var debugEntryPointToken = ReadEntryPointHandle(symReader);

            // documents:
            var documentIndex = new Dictionary<string, DocumentHandle>(StringComparer.Ordinal);
            metadataBuilder.SetCapacity(TableIndex.Document, documents.Length);

            foreach (var document in documents)
            {
                DefineDocument(metadataBuilder, document, documentIndex);
            }

            var lastLocalVariableHandle = default(LocalVariableHandle);
            var lastLocalConstantHandle = default(LocalConstantHandle);

            var importStringsByMethod = new Dictionary<int, ImmutableArray<string>>();
            var importScopesByMethod = new List<ImportScopeHandle>();

            // Maps import scope content to import scope handles
            var importScopeIndex = new Dictionary<ImportScopeInfo, ImportScopeHandle>();

            // import scopes to be emitted to the Portable PDB:
            var importScopes = new List<ImportScopeInfo>();

            // reserve slot for module import scope:
            importScopes.Add(default);

            var externAliasImports = new List<ImportInfo>();
            var externAliasStringSet = new HashSet<string>(StringComparer.Ordinal);

            string? vbDefaultNamespace = null;
            var vbProjectLevelImports = new List<ImportInfo>();

            // first pass:
            foreach (var methodHandle in metadataReader.MethodDefinitions)
            {
                int methodToken = MetadataTokens.GetToken(methodHandle);
                ImmutableArray<ImmutableArray<ImportInfo>> importGroups;

                if (vbSemantics)
                {
                    var importStrings = CustomDebugInfoReader.GetVisualBasicImportStrings(
                        methodToken,
                        symReader,
                        getMethodImportStrings: (token, sr) => GetImportStrings(token, importStringsByMethod, sr));

                    if (importStrings.IsEmpty)
                    {
                        importGroups = default;
                    }
                    else
                    {
                        bool projectLevelImportsDefined = vbProjectLevelImports.Count > 0;
                        var vbFileLevelImports = ArrayBuilder<ImportInfo>.GetInstance();
                        foreach (var importString in importStrings)
                        {
                            if (TryParseImportString(importString, out var import, vbSemantics: true))
                            {
                                // already processed by GetVisualBasicImportStrings
                                Debug.Assert(import.Kind != ImportTargetKind.MethodToken);

                                if (import.Kind == ImportTargetKind.DefaultNamespace)
                                {
                                    vbDefaultNamespace = import.Target;
                                }
                                else if (import.Scope == VBImportScopeKind.Project)
                                {
                                    // All methods that define project level imports should be defining the same imports.
                                    // Use the first set and ignore the rest.
                                    if (!projectLevelImportsDefined)
                                    {
                                        vbProjectLevelImports.Add(import);
                                    }
                                }
                                else if (import.Kind != ImportTargetKind.Defunct && import.Kind != ImportTargetKind.CurrentNamespace)
                                {
                                    vbFileLevelImports.Add(import);
                                }
                            }
                            else
                            {
                                ReportDiagnostic(PdbDiagnosticId.InvalidImportStringFormat, methodToken, importString);
                            }
                        }

                        importGroups = ImmutableArray.Create(vbFileLevelImports.ToImmutableAndFree());
                    }
                }
                else
                {
                    var importStringGroups = CustomDebugInfoReader.GetCSharpGroupedImportStrings(
                        methodToken,
                        symReader,
                        getMethodCustomDebugInfo: (token, sr) => sr.GetCustomDebugInfo(token, methodVersion: 1),
                        getMethodImportStrings: (token, sr) => GetImportStrings(token, importStringsByMethod, sr),
                        externAliasStrings: out var localExternAliasStrings);

                    if (!localExternAliasStrings.IsDefault)
                    {
                        foreach (var externAlias in localExternAliasStrings)
                        {
                            if (externAliasStringSet.Add(externAlias) &&
                                TryParseImportString(externAlias, out var import, vbSemantics: false))
                            {
                                externAliasImports.Add(import);
                            }
                        }
                    }

                    if (importStringGroups.IsDefault)
                    {
                        importGroups = default;
                    }
                    else
                    {
                        importGroups = ImmutableArray.CreateRange(importStringGroups.Select(g => ParseCSharpImportStrings(g, methodToken)));
                    }
                }

                if (importGroups.IsDefault)
                {
                    importScopesByMethod.Add(default);
                }
                else
                {
                    importScopesByMethod.Add(DefineImportScope(importGroups, importScopeIndex, importScopes));
                }
            }

            // always emit VB default namespace, even if it is not specified in the Windows PDB
            if (vbSemantics && vbDefaultNamespace == null)
            {
                vbDefaultNamespace = string.Empty;
            }

            // import scopes:
            metadataBuilder.AddImportScope(
                parentScope: default,
                imports: SerializeModuleImportScope(metadataBuilder, externAliasImports, vbProjectLevelImports, vbDefaultNamespace, metadataModel));

            for (int i = 1; i < importScopes.Count; i++)
            {
                metadataBuilder.AddImportScope(
                    parentScope: importScopes[i].Parent,
                    imports: SerializeImportsBlob(metadataBuilder, importScopes[i].Imports, metadataModel));
            }

            var dynamicVariables = new Dictionary<int, DynamicLocalInfo>();
            var dynamicConstants = new Dictionary<string, List<DynamicLocalInfo>>();
            var tupleVariables = new Dictionary<int, TupleElementNamesInfo>();
            var tupleConstants = new Dictionary<(string name, int scopeStart, int scopeEnd), TupleElementNamesInfo>();
            var scopes = new List<(int Start, int End, ISymUnmanagedVariable[] Variables, ISymUnmanagedConstant[] Constants)>();
            var vbHoistedLocalScopes = new List<StateMachineHoistedLocalScope>();

            // maps MoveNext method definitions to the corresponding kickoff definitions:
            var stateMachineMethods = new Dictionary<MethodDefinitionHandle, MethodDefinitionHandle>();

            // methods:
            metadataBuilder.SetCapacity(TableIndex.MethodDebugInformation, metadataReader.MethodDefinitions.Count);
            foreach (var methodHandle in metadataReader.MethodDefinitions)
            {
                var methodDef = metadataReader.GetMethodDefinition(methodHandle);
                int methodToken = MetadataTokens.GetToken(methodHandle);
                int methodRowId = MetadataTokens.GetRowNumber(methodHandle);

                var symMethod = symReader.GetMethod(methodToken);
                if (symMethod == null)
                {
                    metadataBuilder.AddMethodDebugInformation(default, sequencePoints: default);
                    continue;
                }

                // method debug info:
                MethodBodyBlock? methodBody;
                int localSignatureRowId;
                if (methodDef.RelativeVirtualAddress != 0)
                {
                    methodBody = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                    localSignatureRowId = methodBody.LocalSignature.IsNil ? 0 : MetadataTokens.GetRowNumber(methodBody.LocalSignature);
                }
                else
                {
                    methodBody = null;
                    localSignatureRowId = 0;
                }

                var symSequencePoints = symMethod.GetSequencePoints().ToImmutableArray();

                // add a dummy document:
                if (documentIndex.Count == 0 && symSequencePoints.Length > 0)
                {
                    documentIndex.Add(string.Empty, metadataBuilder.AddDocument(
                        name: metadataBuilder.GetOrAddDocumentName(string.Empty),
                        hashAlgorithm: default,
                        hash: default,
                        language: default));
                }

                BlobHandle sequencePointsBlob = SerializeSequencePoints(metadataBuilder, localSignatureRowId, symSequencePoints, documentIndex, methodToken, out var singleDocumentHandle);

                metadataBuilder.AddMethodDebugInformation(
                    document: singleDocumentHandle,
                    sequencePoints: sequencePointsBlob);

                // state machine and async info:
                var symAsyncMethod = symMethod.AsAsyncMethod();
                if (symAsyncMethod != null)
                {
                    var kickoffToken = symAsyncMethod.GetKickoffMethod();
                    var kickoffHandle = (MethodDefinitionHandle)MetadataTokens.Handle(kickoffToken);

                    if (!stateMachineMethods.TryGetValue(methodHandle, out var existingKickoff))
                    {
                        stateMachineMethods[methodHandle] = kickoffHandle;
                    }
                    else if (kickoffHandle != existingKickoff)
                    {
                        ReportDiagnostic(PdbDiagnosticId.InconsistentStateMachineMethodMapping, methodToken,  kickoffToken, MetadataTokens.GetToken(existingKickoff));
                    }

                    metadataBuilder.AddCustomDebugInformation(
                        parent: methodHandle,
                        kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.AsyncMethodSteppingInformationBlob),
                        value: SerializeAsyncMethodSteppingInfo(metadataBuilder, symAsyncMethod, MetadataTokens.GetRowNumber(methodHandle)));
                }

                // custom debug information:
                var importScope = importScopesByMethod[methodRowId - 1];
                var dynamicLocals = ImmutableArray<DynamicLocalInfo>.Empty;
                var tupleLocals = ImmutableArray<TupleElementNamesInfo>.Empty;

                byte[] customDebugInfoBytes = symReader.GetCustomDebugInfo(methodToken, methodVersion: 1);
                if (customDebugInfoBytes != null)
                {
                    foreach (var record in CustomDebugInfoReader.GetCustomDebugInfoRecords(customDebugInfoBytes))
                    {
                        switch (record.Kind)
                        {
                            case CustomDebugInfoKind.DynamicLocals:
                                dynamicLocals = CustomDebugInfoReader.DecodeDynamicLocalsRecord(record.Data);
                                break;

                            case CustomDebugInfoKind.TupleElementNames:
                                tupleLocals = CustomDebugInfoReader.DecodeTupleElementNamesRecord(record.Data);
                                break;

                            case CustomDebugInfoKind.ForwardMethodInfo:
                                // already processed by GetCSharpGroupedImportStrings
                                break;

                            case CustomDebugInfoKind.StateMachineTypeName:
                                if (importScope.IsNil)
                                {
                                    string nonGenericName = CustomDebugInfoReader.DecodeForwardIteratorRecord(record.Data);
                                    var moveNextHandle = metadataModel.FindStateMachineMoveNextMethod(methodDef, nonGenericName, isGenericSuffixIncluded: false);
                                    if (!moveNextHandle.IsNil)
                                    {
                                        importScope = importScopesByMethod[MetadataTokens.GetRowNumber(moveNextHandle) - 1];

                                        if (!stateMachineMethods.TryGetValue(moveNextHandle, out var existingKickoff))
                                        {
                                            stateMachineMethods.Add(moveNextHandle, methodHandle);
                                        }
                                        else if (existingKickoff != methodHandle)
                                        {
                                            ReportDiagnostic(
                                                PdbDiagnosticId.InconsistentStateMachineMethodMapping, 
                                                MetadataTokens.GetToken(moveNextHandle),
                                                methodToken,
                                                MetadataTokens.GetToken(existingKickoff));
                                        }
                                    }
                                    else
                                    {
                                        ReportDiagnostic(PdbDiagnosticId.InvalidStateMachineTypeName, methodToken, nonGenericName);
                                    }
                                }
                                else
                                {
                                    ReportDiagnostic(PdbDiagnosticId.BothStateMachineTypeNameAndImportsSpecified, methodToken);
                                }

                                break;

                            case CustomDebugInfoKind.StateMachineHoistedLocalScopes:
                                metadataBuilder.AddCustomDebugInformation(
                                    parent: methodHandle,
                                    kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes),
                                    value: SerializeStateMachineHoistedLocalsBlob(metadataBuilder, CustomDebugInfoReader.DecodeStateMachineHoistedLocalScopesRecord(record.Data)));
                                break;

                            case CustomDebugInfoKind.EditAndContinueLocalSlotMap:
                                metadataBuilder.AddCustomDebugInformation(
                                    parent: methodHandle,
                                    kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.EncLocalSlotMap),
                                    value: metadataBuilder.GetOrAddBlob(record.Data));
                                break;

                            case CustomDebugInfoKind.EditAndContinueLambdaMap:
                                metadataBuilder.AddCustomDebugInformation(
                                    parent: methodHandle,
                                    kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.EncLambdaAndClosureMap),
                                    value: metadataBuilder.GetOrAddBlob(record.Data));
                                break;

                            case SymReaderHelpers.CustomDebugInfoKind_EncStateMachineSuspensionPoints:
                                metadataBuilder.AddCustomDebugInformation(
                                    parent: methodHandle,
                                    kind: metadataBuilder.GetOrAddGuid(SymReaderHelpers.EncStateMachineSuspensionPoints),
                                    value: metadataBuilder.GetOrAddBlob(record.Data));
                                break;
                        }
                    }
                }

                var rootScope = symMethod.GetRootScope();
                ISymUnmanagedScope[] childScopes;
                if (rootScope.GetNamespaces().Length != 0 || rootScope.GetLocals().Length != 0 || rootScope.GetConstants().Length != 0)
                {
                    // C#/VB only produce empty root scopes, but Managed C++ doesn't.
                    // Pretend the root scope is a single child.
                    childScopes = new ISymUnmanagedScope[] { rootScope };
                }
                else
                {
                    childScopes = rootScope.GetChildren(); 
                }

                if (childScopes.Length > 0)
                {
                    BuildDynamicLocalMaps(dynamicVariables, dynamicConstants, dynamicLocals, methodToken);
                    BuildTupleLocalMaps(tupleVariables, tupleConstants, tupleLocals, methodToken);

                    Debug.Assert(scopes.Count == 0);
                    Debug.Assert(vbHoistedLocalScopes.Count == 0);
                    foreach (var child in childScopes)
                    {
                        AddScopesRecursive(scopes, vbHoistedLocalScopes, child, methodToken, vbSemantics, isTopScope: true);
                    }

                    foreach (var scope in scopes)
                    {
                        SerializeScope(
                            metadataBuilder,
                            metadataModel,
                            methodHandle,
                            importScope,
                            scope.Start,
                            scope.End,
                            scope.Variables,
                            scope.Constants,
                            tupleVariables,
                            tupleConstants,
                            dynamicVariables,
                            dynamicConstants,
                            lastLocalVariableHandle: ref lastLocalVariableHandle,
                            lastLocalConstantHandle: ref lastLocalConstantHandle);
                    }

                    if (vbHoistedLocalScopes.Count > 0)
                    {
                        metadataBuilder.AddCustomDebugInformation(
                            parent: methodHandle,
                            kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes),
                            value: SerializeStateMachineHoistedLocalsBlob(metadataBuilder, vbHoistedLocalScopes.ToImmutableArray()));
                    }

                    dynamicConstants.Clear();
                    dynamicVariables.Clear();
                    tupleConstants.Clear();
                    tupleVariables.Clear();
                    scopes.Clear();
                    vbHoistedLocalScopes.Clear();
                }
                else if (methodBody != null)
                {
                    metadataBuilder.AddLocalScope(
                        method: methodHandle,
                        importScope: importScope,
                        variableList: NextHandle(lastLocalVariableHandle),
                        constantList: NextHandle(lastLocalConstantHandle),
                        startOffset: 0,
                        length: methodBody.GetILReader().Length);
                }
            }

            foreach (var entry in stateMachineMethods.OrderBy(kvp => MetadataTokens.GetToken(kvp.Key)))
            {
                metadataBuilder.AddStateMachineMethod(entry.Key, entry.Value);
            }

            // If the PDB has SourceLink take it as is.
            // Otherwise if it has srcsvr data convert them to SourceLink.

            byte[]? sourceLinkData;
            try
            {
                sourceLinkData = symReader.GetRawSourceLinkData();
            }
            catch (Exception)
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidSourceLinkData, 0);
                sourceLinkData = null;
            }

            if (sourceLinkData == null)
            {
                byte[]? sourceServerData;
                try
                {
                    sourceServerData = symReader.GetRawSourceServerData();
                }
                catch (Exception)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidSourceLinkData, 0);
                    sourceServerData = null;
                }

                if (sourceServerData != null)
                {
                    sourceLinkData = ConvertSourceServerToSourceLinkData(sourceServerData);
                }
            }

            if (sourceLinkData != null)
            {
                SerializeSourceLinkData(metadataBuilder, sourceLinkData);
            }

            var serializer = new PortablePdbBuilder(metadataBuilder, typeSystemRowCounts, debugEntryPointToken, idProvider: _ => pdbId);
            BlobBuilder blobBuilder = new BlobBuilder();
            serializer.Serialize(blobBuilder);
            blobBuilder.WriteContentTo(targetPdbStream);
        }

        private void DefineDocument(MetadataBuilder metadataBuilder, ISymUnmanagedDocument document, Dictionary<string, DocumentHandle> documentIndex)
        {
            string name = document.GetName();
            Guid language = document.GetLanguage();
            var hashAlgorithm = document.GetHashAlgorithm();
            var checksumHandle = (hashAlgorithm != default) ? metadataBuilder.GetOrAddBlob(document.GetChecksum()) : default;

            var documentHandle = metadataBuilder.AddDocument(
                name: metadataBuilder.GetOrAddDocumentName(name),
                hashAlgorithm: metadataBuilder.GetOrAddGuid(hashAlgorithm),
                hash: checksumHandle,
                language: metadataBuilder.GetOrAddGuid(language));

            byte[]? sourceBlob;

            try
            {
                sourceBlob = document.GetRawEmbeddedSource();
            }
            catch
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidEmbeddedSource, 0, name);
                sourceBlob = null;
            }

            if (sourceBlob != null)
            {
                metadataBuilder.AddCustomDebugInformation(
                    documentHandle,
                    metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.EmbeddedSource),
                    metadataBuilder.GetOrAddBlob(sourceBlob));
            }

            documentIndex.Add(name, documentHandle);
        }

        private void BuildDynamicLocalMaps(
            Dictionary<int, DynamicLocalInfo> variables, 
            Dictionary<string, List<DynamicLocalInfo>> constants, 
            ImmutableArray<DynamicLocalInfo> infos,
            int methodToken)
        {
            Debug.Assert(variables.Count == 0);
            Debug.Assert(constants.Count == 0);

            foreach (var info in infos)
            {
                if (info.SlotId == 0)
                {
                    // All dynamic constants have slot id == 0, but a variable can also have slot id == 0.
                    // Put the info into the constants list and we'll fetch it from there for variables as well.
                    if (constants.TryGetValue(info.LocalName, out var existingInfos))
                    {
                        existingInfos.Add(info);
                    }
                    else
                    {
                        constants.Add(info.LocalName, new List<DynamicLocalInfo> { info });
                    }
                }
                else if (variables.ContainsKey(info.SlotId))
                {
                    ReportDiagnostic(PdbDiagnosticId.DuplicateDynamicLocals, methodToken, info.SlotId);
                }
                else
                {
                    variables.Add(info.SlotId, info);
                }
            }
        }

        private void BuildTupleLocalMaps(
            Dictionary<int, TupleElementNamesInfo> variables,
            Dictionary<(string name, int scopeStart, int scopeEnd), TupleElementNamesInfo> constants,
            ImmutableArray<TupleElementNamesInfo> infos,
            int methodToken)
        {
            Debug.Assert(variables.Count == 0);
            Debug.Assert(constants.Count == 0);
            foreach (var info in infos)
            {
                if (info.SlotIndex >= 0)
                {
                    if (variables.ContainsKey(info.SlotIndex))
                    {
                        ReportDiagnostic(PdbDiagnosticId.DuplicateTupleElementNamesForSlot, methodToken, info.SlotIndex);
                    }
                    else
                    {
                        variables.Add(info.SlotIndex, info);
                    }
                }
                else
                {
                    var key = (info.LocalName, info.ScopeStart, info.ScopeEnd);
                    if (constants.ContainsKey(key))
                    {
                        ReportDiagnostic(PdbDiagnosticId.DuplicateTupleElementNamesForConstant, methodToken, info.LocalName, info.ScopeStart, info.ScopeEnd);
                    }
                    else
                    {
                        constants.Add(key, info);
                    }
                }
            }
        }

        private static BlobHandle SerializeStateMachineHoistedLocalsBlob(MetadataBuilder metadataBuilder, ImmutableArray<StateMachineHoistedLocalScope> scopes)
        {
            var builder = new BlobBuilder();

            foreach (var scope in scopes)
            {
                builder.WriteInt32(scope.StartOffset);
                builder.WriteInt32(scope.EndOffset - scope.StartOffset);
            }

            return metadataBuilder.GetOrAddBlob(builder);
        }

        private static BlobHandle SerializeAsyncMethodSteppingInfo(MetadataBuilder metadataBuilder, ISymUnmanagedAsyncMethod symAsyncMethod, int moveNextMethodRowId)
        {
            var builder = new BlobBuilder();

            builder.WriteUInt32((uint)((long)symAsyncMethod.GetCatchHandlerILOffset() + 1));

            foreach (var stepInfo in symAsyncMethod.GetAsyncStepInfos())
            {
                builder.WriteInt32(stepInfo.YieldOffset);
                builder.WriteInt32(stepInfo.ResumeOffset);
                builder.WriteCompressedInteger(moveNextMethodRowId);
            }

            return metadataBuilder.GetOrAddBlob(builder);
        }

        private static bool TryParseImportString(string importString, out ImportInfo import, bool vbSemantics)
        {
            ImportTargetKind kind;
            string target;
            string alias;
            string? externAlias = null;
            var scope = VBImportScopeKind.Unspecified;

            if (vbSemantics ? 
                CustomDebugInfoReader.TryParseVisualBasicImportString(importString, out alias, out target, out kind, out scope) :
                CustomDebugInfoReader.TryParseCSharpImportString(importString, out alias, out externAlias, out target, out kind))
            {
                import = new ImportInfo(kind, target, alias, externAlias, scope);
                return true;
            }

            import = default;
            return false;
        }

        private static ImmutableArray<string> GetImportStrings(int token, Dictionary<int, ImmutableArray<string>> cache, ISymUnmanagedReader3 reader)
        {
            ImmutableArray<string> result;
            if (!cache.TryGetValue(token, out result))
            {
                result = SymReaderHelpers.GetImportStrings(reader, token, methodVersion: 1);
                cache.Add(token, result);
            }

            return result;
        }

        private MethodDefinitionHandle ReadEntryPointHandle(ISymUnmanagedReader symReader)
        {
            var handle = MetadataTokens.EntityHandle(symReader.GetUserEntryPoint());
            if (handle.IsNil)
            {
                return default;
            }

            if (handle.Kind != HandleKind.MethodDefinition)
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidEntryPointToken, 0, MetadataTokens.GetToken(handle));
                return default;
            }

            return (MethodDefinitionHandle)handle;
        }

        private static readonly ImportScopeHandle ModuleImportScopeHandle = MetadataTokens.ImportScopeHandle(1);

        private struct ImportScopeInfo : IEquatable<ImportScopeInfo>
        {
            public readonly ImportScopeHandle Parent;
            public readonly ImmutableArray<ImportInfo> Imports;

            public ImportScopeInfo(ImmutableArray<ImportInfo> imports, ImportScopeHandle parent)
            {
                Parent = parent;
                Imports = imports;
            }

            public override bool Equals(object? obj) => obj is ImportScopeInfo info && Equals(info);
            public bool Equals(ImportScopeInfo other) => Parent == other.Parent && Imports.SequenceEqual(other.Imports);
            public override int GetHashCode() => Hash.Combine(Parent.GetHashCode(), Hash.CombineValues(Imports));
        }

        private static ImportScopeHandle DefineImportScope(
            ImmutableArray<ImmutableArray<ImportInfo>> importGroups,
            Dictionary<ImportScopeInfo, ImportScopeHandle> importScopeIndex,
            List<ImportScopeInfo> importScopes)
        {
            ImportScopeHandle parentHandle = ModuleImportScopeHandle;
            for (int i = importGroups.Length - 1; i >= 0; i--)
            {
                var info = new ImportScopeInfo(importGroups[i], parentHandle);

                if (importScopeIndex.TryGetValue(info, out var existingScopeHandle))
                {
                    parentHandle = existingScopeHandle;
                }
                else
                {
                    importScopes.Add(info);
                    importScopeIndex.Add(info, parentHandle = MetadataTokens.ImportScopeHandle(importScopes.Count));
                }
            }

            return parentHandle;
        }

        private ImmutableArray<ImportInfo> ParseCSharpImportStrings(ImmutableArray<string> importStrings, int methodToken)
        {
            var builder = ArrayBuilder<ImportInfo>.GetInstance();
            foreach (var importString in importStrings)
            {
                if (TryParseImportString(importString, out var import, vbSemantics: false))
                {
                    builder.Add(import);
                }
                else
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidImportStringFormat, methodToken, importString);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private static BlobHandle SerializeModuleImportScope(
            MetadataBuilder metadataBuilder,
            IEnumerable<ImportInfo> csExternAliasImports,
            IEnumerable<ImportInfo> vbProjectLevelImports,
            string? vbDefaultNamespace,
            MetadataModel metadataModel)
        {
            // module-level import scope:
            var builder = new BlobBuilder();
            var encoder = new ImportDefinitionEncoder(metadataBuilder, builder);

            if (vbDefaultNamespace != null)
            {
                SerializeModuleDefaultNamespace(metadataBuilder, vbDefaultNamespace);
            }

            foreach (var import in csExternAliasImports)
            {
                SerializeImport(encoder, import, metadataModel);
            }

            foreach (var import in vbProjectLevelImports)
            {
                SerializeImport(encoder, import, metadataModel);
            }

            return metadataBuilder.GetOrAddBlob(builder);
        }

        private static void SerializeModuleDefaultNamespace(MetadataBuilder metadataBuilder, string namespaceName)
        {
            metadataBuilder.AddCustomDebugInformation(
                parent: EntityHandle.ModuleDefinition,
                kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.DefaultNamespace),
                value: metadataBuilder.GetOrAddBlobUTF8(namespaceName));
        }
        
        private static BlobHandle SerializeImportsBlob(MetadataBuilder metadataBuilder, ImmutableArray<ImportInfo> imports, MetadataModel metadataModel)
        {
            var builder = new BlobBuilder();
            var encoder = new ImportDefinitionEncoder(metadataBuilder, builder);

            foreach (var import in imports)
            {
                SerializeImport(encoder, import, metadataModel);
            }

            return metadataBuilder.GetOrAddBlob(builder);
        }

        private readonly struct ImportInfo
        {
            public readonly ImportTargetKind Kind;
            public readonly string Target;
            public readonly string? ExternAlias;
            public readonly string Alias;
            public readonly VBImportScopeKind Scope;

            public ImportInfo(ImportTargetKind kind, string target, string alias, string? externAlias, VBImportScopeKind scope)
            {
                Kind = kind;
                Target = target;
                Alias = alias;
                ExternAlias = externAlias;
                Scope = scope;
            }
        }

        private static void SerializeImport(ImportDefinitionEncoder encoder, ImportInfo import, MetadataModel metadataModel)
        {
            var assemblyRef = default(AssemblyReferenceHandle);
            EntityHandle type;
            switch (import.Kind)
            {
                case ImportTargetKind.Assembly:
                    // alias: assembly alias 
                    // target: assembly name for module-level extern alias definition, or null for file level extern alias import

                    if (import.Target == null)
                    {
                        // TODO: skip if the alias isn't defined in an ancestor scope?
                        encoder.ImportAssemblyReferenceAlias(import.Alias);
                        break;
                    }

                    if (!metadataModel.TryResolveAssemblyReference(import.Target, out assemblyRef))
                    {
                        // no type from the assembly is used, the AssemblyRef is not present in the metadata
                        break;
                    }

                    encoder.AliasAssemblyReference(assemblyRef, import.Alias);
                    break;

                case ImportTargetKind.Namespace:
                    if (import.ExternAlias != null && !metadataModel.TryResolveAssemblyReference(import.ExternAlias, out assemblyRef))
                    {
                        // no type from the assembly is used, the AssemblyRef is not present in the metadata
                        break;
                    }

                    encoder.Namespace(import.Target, import.Alias, assemblyRef);
                    break;

                case ImportTargetKind.Type:
                    if (!metadataModel.TryResolveType(import.Target, out type))
                    {
                        // the type is not used in the source, the metadata is missing a TypeRef.
                        break;
                    }

                    encoder.Type(type, import.Alias);
                    break;

                case ImportTargetKind.NamespaceOrType:
                    if (metadataModel.TryResolveType(import.Target, out type))
                    {
                        encoder.Type(type, import.Alias);
                    }
                    else
                    {
                        encoder.Namespace(import.Target, import.Alias);
                    }

                    break;

                case ImportTargetKind.XmlNamespace:
                    encoder.XmlNamespace(import.Alias, import.Target);
                    break;

                case ImportTargetKind.DefaultNamespace:
                    // already handled 
                    throw ExceptionUtilities.Unreachable;

                case ImportTargetKind.CurrentNamespace:
                case ImportTargetKind.MethodToken:
                case ImportTargetKind.Defunct:
                    break;
            }
        }

        private void AddScopesRecursive(
            List<(int, int, ISymUnmanagedVariable[], ISymUnmanagedConstant[])> builder,
            List<StateMachineHoistedLocalScope> vbHoistedLocalScopeBuilder,
            ISymUnmanagedScope symScope, 
            int methodToken,
            bool vbSemantics,
            bool isTopScope)
        {
            int start, end;
            ISymUnmanagedVariable[] symLocals;
            ISymUnmanagedConstant[] symConstants;
            ISymUnmanagedScope[] symChildScopes;
            
            try
            {
                // VB Windows PDBs encode the range as end-inclusive, 
                // all Portable PDBs use end-exclusive encoding.
                start = symScope.GetStartOffset();
                end = symScope.GetEndOffset() + (vbSemantics && !isTopScope ? 1 : 0);

                symLocals = symScope.GetLocals();

                if (vbSemantics)
                {
                    int realLocalsCount = 0;
                    foreach (var symLocal in symLocals)
                    {
                        var name = symLocal.GetName();
                        if (MetadataModel.TryParseVisualBasicResumableLocalIndex(name, out int index))
                        {
                            while (vbHoistedLocalScopeBuilder.Count <= index)
                            {
                                vbHoistedLocalScopeBuilder.Add(new StateMachineHoistedLocalScope());
                            }

                            vbHoistedLocalScopeBuilder[index] = new StateMachineHoistedLocalScope(start, end);
                        }
                        else
                        {
                            symLocals[realLocalsCount++] = symLocal;
                        }
                    }

                    Array.Resize(ref symLocals, realLocalsCount);
                }

                symConstants = symScope.GetConstants();
                symChildScopes = symScope.GetChildren();
            }
            catch (Exception)
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidLocalScope, methodToken);
                return;
            }

            builder.Add((start, end, symLocals, symConstants));

            int scopeCountBeforeChildren = builder.Count;
            int previousChildScopeEnd = start;
            foreach (ISymUnmanagedScope child in symChildScopes)
            {
                int childScopeStart; 
                int childScopeEnd;

                try
                {
                    childScopeStart = child.GetStartOffset();
                    childScopeEnd = child.GetEndOffset();
                }
                catch (Exception)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidLocalScope, methodToken);
                    continue;
                }

                // scopes are properly nested:
                if (childScopeStart < previousChildScopeEnd || childScopeEnd > end)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidScopeILOffsetRange, methodToken, childScopeStart, childScopeEnd);
                    break;
                }

                previousChildScopeEnd = childScopeEnd;

                AddScopesRecursive(builder, vbHoistedLocalScopeBuilder, child, methodToken, vbSemantics, isTopScope: false);
            }

            if (!isTopScope && symLocals.Length == 0 && symConstants.Length == 0 && builder.Count == scopeCountBeforeChildren)
            {
                // remove the current scope, it's empty:
                builder.RemoveAt(builder.Count - 1);
            }
        }

        private void SerializeScope(
            MetadataBuilder metadataBuilder,
            MetadataModel metadataModel,
            MethodDefinitionHandle methodHandle,
            ImportScopeHandle importScopeHandle,
            int start,
            int end,
            ISymUnmanagedVariable[] symVariables,
            ISymUnmanagedConstant[] symConstants,
            Dictionary<int, TupleElementNamesInfo> tupleVariables,
            Dictionary<(string, int, int), TupleElementNamesInfo> tupleConstants,
            Dictionary<int, DynamicLocalInfo> dynamicVariables,
            Dictionary<string, List<DynamicLocalInfo>> dynamicConstants,
            ref LocalVariableHandle lastLocalVariableHandle,
            ref LocalConstantHandle lastLocalConstantHandle)
        {
            metadataBuilder.AddLocalScope(
                method: methodHandle,
                importScope: importScopeHandle,
                variableList: NextHandle(lastLocalVariableHandle),
                constantList: NextHandle(lastLocalConstantHandle),
                startOffset: start,
                length: end - start);

            bool TryPopConstantInfo(string name, out DynamicLocalInfo info)
            {
                if (dynamicConstants.TryGetValue(name, out var dynamicInfos) && dynamicInfos.Count > 0)
                {
                    info = dynamicInfos[0];
                    dynamicInfos.RemoveAt(0);
                    return true;
                }

                info = default;
                return false;
            }

            foreach (var symVariable in symVariables)
            {
                int slot;
                string name;
                LocalVariableAttributes attributes;

                try
                {
                    slot = symVariable.GetSlot();
                    name = symVariable.GetName();
                    attributes = (LocalVariableAttributes)symVariable.GetAttributes();
                }
                catch (Exception)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidLocalConstantData, MetadataTokens.GetToken(methodHandle));
                    continue;
                }

                lastLocalVariableHandle = metadataBuilder.AddLocalVariable(
                    attributes: attributes,
                    index: slot,
                    name: metadataBuilder.GetOrAddString(name));

                if (tupleVariables.TryGetValue(slot, out var tupleInfo))
                {
                    metadataBuilder.AddCustomDebugInformation(
                        parent: lastLocalVariableHandle,
                        kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.TupleElementNames),
                        value: SerializeTupleInfo(metadataBuilder, tupleInfo));
                }

                if (slot > 0 && dynamicVariables.TryGetValue(slot, out var dynamicInfo) ||
                    slot == 0 && TryPopConstantInfo(name, out dynamicInfo))
                {
                    metadataBuilder.AddCustomDebugInformation(
                        parent: lastLocalVariableHandle,
                        kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.DynamicLocalVariables),
                        value: SerializeDynamicInfo(metadataBuilder, dynamicInfo));
                }
            }

            foreach (var symConstant in symConstants)
            {
                string name;
                object value;
                byte[] signature;

                try
                {
                    name = symConstant.GetName();
                    value = symConstant.GetValue();
                    signature = symConstant.GetSignature();
                }
                catch (Exception)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidLocalConstantData, MetadataTokens.GetToken(methodHandle));
                    continue;
                }

                BlobHandle signatureHandle;
                try
                {
                    signatureHandle = SerializeConstantSignature(metadataBuilder, metadataModel, signature, value);
                }
                catch (BadImageFormatException)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidLocalConstantSignature, MetadataTokens.GetToken(methodHandle), name);
                    continue;
                }

                lastLocalConstantHandle = metadataBuilder.AddLocalConstant(
                    name: metadataBuilder.GetOrAddString(name),
                    signature: signatureHandle);

                if (tupleConstants.TryGetValue((name, start, end), out var tupleInfo))
                {
                    metadataBuilder.AddCustomDebugInformation(
                        parent: lastLocalConstantHandle,
                        kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.TupleElementNames),
                        value: SerializeTupleInfo(metadataBuilder, tupleInfo));
                }

                if (TryPopConstantInfo(name, out var dynamicInfo))
                {
                    metadataBuilder.AddCustomDebugInformation(
                        parent: lastLocalConstantHandle,
                        kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.DynamicLocalVariables),
                        value: SerializeDynamicInfo(metadataBuilder, dynamicInfo));
                }
            }
        }

        private unsafe static BlobHandle SerializeConstantSignature(MetadataBuilder metadataBuilder, MetadataModel metadataModel, byte[] signature, object value)
        {
            var builder = new BlobBuilder();
            ConvertConstantSignature(builder, metadataModel, signature, value);
            return metadataBuilder.GetOrAddBlob(builder);
        }

        private static BlobHandle SerializeDynamicInfo(MetadataBuilder metadataBuilder, DynamicLocalInfo dynamicInfo)
        {
            var builder = new BlobBuilder();
            MetadataUtilities.SerializeBitVector(builder, dynamicInfo.Flags);
            return metadataBuilder.GetOrAddBlob(builder);
        }

        private static BlobHandle SerializeTupleInfo(MetadataBuilder metadataBuilder, TupleElementNamesInfo tupleInfo)
        {
            var builder = new BlobBuilder();
            MetadataUtilities.SerializeTupleElementNames(builder, tupleInfo.ElementNames);
            return metadataBuilder.GetOrAddBlob(builder);
        }

        private static LocalVariableHandle NextHandle(LocalVariableHandle handle) =>
            MetadataTokens.LocalVariableHandle(MetadataTokens.GetRowNumber(handle) + 1);

        private static LocalConstantHandle NextHandle(LocalConstantHandle handle) =>
            MetadataTokens.LocalConstantHandle(MetadataTokens.GetRowNumber(handle) + 1);

        private BlobHandle SerializeSequencePoints(
            MetadataBuilder metadataBuilder,
            int localSignatureRowId,
            ImmutableArray<SymUnmanagedSequencePoint> sequencePoints,
            IReadOnlyDictionary<string, DocumentHandle> documentIndex,
            int methodIndex,
            out DocumentHandle singleDocumentHandle)
        {
            if (sequencePoints.Length == 0)
            {
                singleDocumentHandle = default;
                return default;
            }

            var writer = new BlobBuilder();

            int previousNonHiddenStartLine = -1;
            int previousNonHiddenStartColumn = -1;

            // header:
            writer.WriteCompressedInteger(localSignatureRowId);

            DocumentHandle previousDocument = TryGetSingleDocument(sequencePoints, documentIndex, methodIndex);
            singleDocumentHandle = previousDocument;

            int previousOffset = -1;
            for (int i = 0; i < sequencePoints.Length; i++)
            {
                var sequencePoint = SanitizeSequencePoint(sequencePoints[i], previousOffset);

                var currentDocument = GetDocumentHandle(sequencePoint.Document, documentIndex, methodIndex);
                if (previousDocument != currentDocument)
                {
                    // optional document in header or document record:
                    if (!previousDocument.IsNil)
                    {
                        writer.WriteCompressedInteger(0);
                    }

                    writer.WriteCompressedInteger(MetadataTokens.GetRowNumber(currentDocument));
                    previousDocument = currentDocument;
                }

                // delta IL offset:
                if (i > 0)
                {
                    writer.WriteCompressedInteger(sequencePoint.Offset - previousOffset);
                }
                else
                {
                    writer.WriteCompressedInteger(sequencePoint.Offset);
                }

                previousOffset = sequencePoint.Offset;

                if (sequencePoint.IsHidden)
                {
                    writer.WriteInt16(0);
                    continue;
                }

                // Delta Lines & Columns:
                SerializeDeltaLinesAndColumns(writer, sequencePoint);

                // delta Start Lines & Columns:
                if (previousNonHiddenStartLine < 0)
                {
                    Debug.Assert(previousNonHiddenStartColumn < 0);
                    writer.WriteCompressedInteger(sequencePoint.StartLine);
                    writer.WriteCompressedInteger(sequencePoint.StartColumn);
                }
                else
                {
                    writer.WriteCompressedSignedInteger(sequencePoint.StartLine - previousNonHiddenStartLine);
                    writer.WriteCompressedSignedInteger(sequencePoint.StartColumn - previousNonHiddenStartColumn);
                }

                previousNonHiddenStartLine = sequencePoint.StartLine;
                previousNonHiddenStartColumn = sequencePoint.StartColumn;
            }

            return metadataBuilder.GetOrAddBlob(writer);
        }

        private static SymUnmanagedSequencePoint SanitizeSequencePoint(SymUnmanagedSequencePoint sequencePoint, int previousOffset)
        {
            // Spec:
            // The values of non-hidden sequence point must satisfy the following constraints
            // - IL Offset is within range [0, 0x20000000)
            // - IL Offset of a sequence point is lesser than IL Offset of the subsequent sequence point.
            // - Start Line is within range [0, 0x20000000) and not equal to 0xfeefee.
            // - End Line is within range [0, 0x20000000) and not equal to 0xfeefee.
            // - Start Column is within range [0, 0x10000)
            // - End Column is within range [0, 0x10000)
            // - End Line is greater or equal to Start Line.
            // - If Start Line is equal to End Line then End Column is greater than Start Column.

            const int maxColumn = ushort.MaxValue;
            const int maxLine = MetadataUtilities.MaxCompressedIntegerValue;
            int offset = Math.Max(sequencePoint.Offset, previousOffset + 1);

            if (sequencePoint.IsHidden)
            {
                return new SymUnmanagedSequencePoint(offset, sequencePoint.Document, sequencePoint.StartLine, sequencePoint.StartColumn, sequencePoint.EndLine, sequencePoint.EndColumn);
            }

            int startLine = Math.Max(0, Math.Min(sequencePoint.StartLine, maxLine));
            int endLine = Math.Max(startLine, Math.Min(sequencePoint.EndLine, maxLine));
            int startColumn = Math.Max(0, Math.Min(sequencePoint.StartColumn, maxColumn));
            int endColumn = Math.Max(0, Math.Min(sequencePoint.EndColumn, maxColumn));

            if (startLine == endLine && startColumn >= endColumn)
            {
                // Managed C++ emits sequence points: (line, 0, line, 0), meaning the entire line:
                endColumn = (startColumn == 0 && endColumn == 0) ? maxColumn : startColumn + 1;
            }

            // TODO: report warning if SP has been adjusted

            return new SymUnmanagedSequencePoint(offset, sequencePoint.Document, startLine, startColumn, endLine, endColumn);
        }

        private DocumentHandle TryGetSingleDocument(ImmutableArray<SymUnmanagedSequencePoint> sequencePoints, IReadOnlyDictionary<string, DocumentHandle> documentIndex, int methodToken)
        {
            DocumentHandle singleDocument = GetDocumentHandle(sequencePoints[0].Document, documentIndex, methodToken);
            for (int i = 1; i < sequencePoints.Length; i++)
            {
                if (GetDocumentHandle(sequencePoints[i].Document, documentIndex, methodToken) != singleDocument)
                {
                    return default;
                }
            }

            return singleDocument;
        }

        private DocumentHandle GetDocumentHandle(ISymUnmanagedDocument document, IReadOnlyDictionary<string, DocumentHandle> documentIndex, int methodToken)
        {
            string name;
            try
            {
                name = document.GetName();
            }
            catch (Exception)
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidSequencePointDocument, methodToken);
                return default;
            }

            if (documentIndex.TryGetValue(name, out var handle))
            {
                return handle;
            }

            ReportDiagnostic(PdbDiagnosticId.InvalidSequencePointDocument, methodToken, name);
            return default;
        }

        private static void SerializeDeltaLinesAndColumns(BlobBuilder writer, SymUnmanagedSequencePoint sequencePoint)
        {
            int deltaLines = sequencePoint.EndLine - sequencePoint.StartLine;
            int deltaColumns = sequencePoint.EndColumn - sequencePoint.StartColumn;

            // only hidden sequence points have zero width
            Debug.Assert(deltaLines != 0 || deltaColumns != 0 || sequencePoint.IsHidden);

            writer.WriteCompressedInteger(deltaLines);

            if (deltaLines == 0)
            {
                writer.WriteCompressedInteger(deltaColumns);
            }
            else
            {
                writer.WriteCompressedSignedInteger(deltaColumns);
            }
        }

        private static void SerializeSourceLinkData(MetadataBuilder metadataBuilder, byte[] data)
        {
            metadataBuilder.AddCustomDebugInformation(
                parent: EntityHandle.ModuleDefinition,
                kind: metadataBuilder.GetOrAddGuid(PortableCustomDebugInfoKinds.SourceLink),
                value: metadataBuilder.GetOrAddBlob(data));
        }

        private static byte[]? ConvertSourceServerToSourceLinkData(byte[] sourceServerData)
        {
            var sourceLinkData = ConvertSourceServerToSourceLinkData(Encoding.UTF8.GetString(sourceServerData));
            return (sourceLinkData != null) ? Encoding.UTF8.GetBytes(sourceLinkData) : null;
        }

        // internal for testing
        internal static string? ConvertSourceServerToSourceLinkData(string sourceServerData)
        {
            // TODO: replicate what debugger does (https://github.com/dotnet/symreader-converter/issues/51)

            string[] lines = sourceServerData.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            int variablesStart = lines.IndexOf(line => line.StartsWith("SRCSRV: variables", StringComparison.Ordinal));
            int sourceFilesStart = lines.IndexOf(line => line.StartsWith("SRCSRV: source files", StringComparison.Ordinal));

            IEnumerable<(string key, string value)> GetPairs(int startLine, char separator)
            {
                for (int i = startLine; i < lines.Length; i++)
                {
                    string line = lines[i];
                    int equals = line.IndexOf(separator);
                    if (equals <= 0)
                    {
                        yield break;
                    }

                    yield return (line.Substring(0, equals), line.Substring(equals + 1));
                }
            }

            var (_, rawUrl) = GetPairs(variablesStart + 1, '=').FirstOrDefault(v => v.key == "RAWURL");
            var files = GetPairs(sourceFilesStart + 1, '*').ToArray();

            string? commonUriPrefix = rawUrl?.EndsWith("%var2%", StringComparison.Ordinal) == true ?
                rawUrl.Substring(0, rawUrl.Length - "%var2%".Length) : null;

            var builder = new StringBuilder();
            bool isFirstEntry = true;

            void AppendMapping(string key, string value, bool isPrefix)
            {
                if (key.Contains('*') || value.Contains('*') || key.Length == 0 || value.Length == 0)
                {
                    return;
                }

                string Escape(string str) => str.Replace("\"", "\\\"");

                if (!isFirstEntry)
                {
                    builder.AppendLine(",");
                }

                builder.Append("    \"");
                builder.Append(Escape(key));

                if (isPrefix)
                {
                    builder.Append('*');
                }

                builder.Append("\": \"");
                builder.Append(Escape(value));

                if (isPrefix)
                {
                    builder.Append('*');
                }

                builder.Append("\"");

                isFirstEntry = false;
            }

            string? commonPathPrefix = null;
            var nonMatching = new List<(string key, string value)>();

            foreach (var file in files)
            {
                if (!file.key.Replace('\\', '/').EndsWith(file.value, StringComparison.Ordinal))
                {
                    nonMatching.Add(file);
                    continue;
                }

                if (commonPathPrefix == null)
                {
                    commonPathPrefix = file.key.Substring(0, file.key.Length - file.value.Length);
                }
                else if (!file.key.StartsWith(commonPathPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    nonMatching.Add(file);
                    continue;
                }
            }

            builder.AppendLine("{");
            builder.AppendLine(@"""documents"": {");

            foreach (var file in nonMatching)
            {
                AppendMapping(file.key, commonUriPrefix + file.value, isPrefix: false);
            }

            if (commonPathPrefix != null)
            {
                AppendMapping(commonPathPrefix, commonUriPrefix ?? string.Empty, isPrefix: true);
            }

            builder.AppendLine();
            builder.AppendLine("  }");
            builder.AppendLine("}");

            return isFirstEntry ? null : builder.ToString();
        }
    }
}
