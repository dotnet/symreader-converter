﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.DiaSymReader.PortablePdb;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    internal static partial class PdbConverterWindowsToPortable
    {
        /// <exception cref="COMException"/>
        /// <exception cref="InvalidDataException"/>
        public static void Convert(PEReader peReader, Stream sourcePdbStream, Stream targetPdbStream)
        {
            var metadataBuilder = new MetadataBuilder();
            var pdbId = ReadPdbId(peReader);

            var symReader = SymReaderFactory.CreateWindowsPdbReader(sourcePdbStream, peReader);

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
                string name = document.GetName();
                Guid language = document.GetLanguage();

                var rid = metadataBuilder.AddDocument(
                    name: metadataBuilder.GetOrAddDocumentName(name),
                    hashAlgorithm: metadataBuilder.GetOrAddGuid(document.GetHashAlgorithm()),
                    hash: metadataBuilder.GetOrAddBlob(document.GetChecksum()),
                    language: metadataBuilder.GetOrAddGuid(language));

                documentIndex.Add(name, rid);
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
            importScopes.Add(default(ImportScopeInfo));

            var externAliasImports = new List<ImportInfo>();
            var externAliasStringSet = new HashSet<string>(StringComparer.Ordinal);

            string vbDefaultNamespace = null;
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
                        importGroups = default(ImmutableArray<ImmutableArray<ImportInfo>>);
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
                        importGroups = default(ImmutableArray<ImmutableArray<ImportInfo>>);
                    }
                    else
                    {
                        importGroups = ImmutableArray.CreateRange(importStringGroups.Select(g => ParseImportStrings(g, vbSemantics: false)));
                    }
                }

                if (importGroups.IsDefault)
                {
                    importScopesByMethod.Add(default(ImportScopeHandle));
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
                parentScope: default(ImportScopeHandle),
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
                    metadataBuilder.AddMethodDebugInformation(default(DocumentHandle), sequencePoints: default(BlobHandle));
                    continue;
                }

                // method debug info:
                MethodBodyBlock methodBodyOpt;
                int localSignatureRowId;
                if (methodDef.RelativeVirtualAddress != 0)
                {
                    methodBodyOpt = peReader.GetMethodBody(methodDef.RelativeVirtualAddress);
                    localSignatureRowId = methodBodyOpt.LocalSignature.IsNil ? 0 : MetadataTokens.GetRowNumber(methodBodyOpt.LocalSignature);
                }
                else
                {
                    methodBodyOpt = null;
                    localSignatureRowId = 0;
                }

                var symSequencePoints = symMethod.GetSequencePoints().ToImmutableArray();

                BlobHandle sequencePointsBlob = SerializeSequencePoints(metadataBuilder, localSignatureRowId, symSequencePoints, documentIndex, out var singleDocumentHandle);

                metadataBuilder.AddMethodDebugInformation(
                    document: singleDocumentHandle,
                    sequencePoints: sequencePointsBlob);

                // state machine and async info:
                var symAsyncMethod = symMethod.AsAsyncMethod();
                if (symAsyncMethod != null)
                {
                    var kickoffToken = MetadataTokens.Handle(symAsyncMethod.GetKickoffMethod());
                    metadataBuilder.AddStateMachineMethod(
                        moveNextMethod: methodHandle,
                        kickoffMethod: (MethodDefinitionHandle)kickoffToken);

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
                                if (importScope.IsNil)
                                {
                                    int forwardToMethodRowId = MetadataUtilities.GetRowId(CustomDebugInfoReader.DecodeForwardRecord(record.Data));
                                    if (forwardToMethodRowId >= 1 && forwardToMethodRowId <= importScopesByMethod.Count)
                                    {
                                        importScope = importScopesByMethod[forwardToMethodRowId - 1];
                                    }
                                    else
                                    {
                                        // TODO: error: invalid CDI data
                                    }

                                    // TODO: if (importScope.IsNil) warning: forwarded to method that doesn't have debug info
                                }
                                else
                                {
                                    // TODO: warning: import debug info forwarded as well as specified
                                }

                                break;

                            case CustomDebugInfoKind.StateMachineTypeName:
                                if (importScope.IsNil)
                                {
                                    string nonGenericName = CustomDebugInfoReader.DecodeForwardIteratorRecord(record.Data);
                                    var moveNextHandle = metadataModel.FindStateMachineMoveNextMethod(methodDef, nonGenericName, isGenericSuffixIncluded: false);
                                    if (!moveNextHandle.IsNil)
                                    {
                                        importScope = importScopesByMethod[MetadataTokens.GetRowNumber(moveNextHandle) - 1];
                                    }
                                    else
                                    {
                                        // TODO: warning: invalid state machine name in CDI
                                    }
                                }
                                else
                                {
                                    // TODO: warning: kickoff methods shouldn't have import scopes defined
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
                        }
                    }
                }

                var rootScope = symMethod.GetRootScope();
                if (rootScope.GetNamespaces().Length != 0 || rootScope.GetLocals().Length != 0 || rootScope.GetConstants().Length == 0)
                {
                    // TODO: warning: 
                    // "Root scope must be empty (method 0x{0:x8})", MetadataTokens.GetToken(methodHandle))
                }

                var childScopes = rootScope.GetChildren();
                if (childScopes.Length > 0)
                {
                    BuildDynamicLocalMaps(dynamicVariables, dynamicConstants, dynamicLocals);
                    BuildTupleLocalMaps(tupleVariables, tupleConstants, tupleLocals);

                    Debug.Assert(scopes.Count == 0);
                    foreach (var child in childScopes)
                    {
                        AddScopesRecursive(scopes, child, vbSemantics, isTopScope: true);
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
                            vbSemantics,
                            lastLocalVariableHandle: ref lastLocalVariableHandle,
                            lastLocalConstantHandle: ref lastLocalConstantHandle);
                    }

                    dynamicConstants.Clear();
                    dynamicVariables.Clear();
                    tupleConstants.Clear();
                    tupleVariables.Clear();
                    scopes.Clear();
                }
                else if (methodBodyOpt != null)
                {
                    metadataBuilder.AddLocalScope(
                        method: methodHandle,
                        importScope: importScope,
                        variableList: NextHandle(lastLocalVariableHandle),
                        constantList: NextHandle(lastLocalConstantHandle),
                        startOffset: 0,
                        length: methodBodyOpt.GetILReader().Length);
                }
            }

            var serializer = new PortablePdbBuilder(metadataBuilder, typeSystemRowCounts, debugEntryPointToken, idProvider: _ => pdbId);
            BlobBuilder blobBuilder = new BlobBuilder();
            serializer.Serialize(blobBuilder);
            blobBuilder.WriteContentTo(targetPdbStream);
        }

        private static void BuildDynamicLocalMaps(
            Dictionary<int, DynamicLocalInfo> variables, 
            Dictionary<string, List<DynamicLocalInfo>> constants, 
            ImmutableArray<DynamicLocalInfo> infos)
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
                else if (!variables.ContainsKey(info.SlotId))
                {
                    variables.Add(info.SlotId, info);
                }
                else
                {
                    // TODO: warning
                }
            }
        }

        private static void BuildTupleLocalMaps(
            Dictionary<int, TupleElementNamesInfo> variables,
            Dictionary<(string name, int scopeStart, int scopeEnd), TupleElementNamesInfo> constants,
            ImmutableArray<TupleElementNamesInfo> infos)
        {
            Debug.Assert(variables.Count == 0);
            Debug.Assert(constants.Count == 0);
            foreach (var info in infos)
            {
                if (info.SlotIndex >= 0)
                {
                    variables.Add(info.SlotIndex, info);
                }
                else
                {
                    constants.Add((info.LocalName, info.ScopeStart, info.ScopeEnd), info);
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
            string externAlias = null;
            var scope = VBImportScopeKind.Unspecified;

            if (vbSemantics ? 
                CustomDebugInfoReader.TryParseVisualBasicImportString(importString, out alias, out target, out kind, out scope) :
                CustomDebugInfoReader.TryParseCSharpImportString(importString, out alias, out externAlias, out target, out kind))
            {
                import = new ImportInfo(kind, target, alias, externAlias, scope);
                return true;
            }

            // TODO: report warning
            import = default(ImportInfo);
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

        private static BlobContentId ReadPdbId(PEReader peReader)
        {
            foreach (var entry in peReader.ReadDebugDirectory())
            {
                if (entry.Type == DebugDirectoryEntryType.CodeView)
                {
                    // TODO: const
                    if (entry.MajorVersion == 0x504D)
                    {
                        throw new InvalidDataException(ConverterResources.SpecifiedPEBuiltWithPortablePdb);
                    }

                    return new BlobContentId(peReader.ReadCodeViewDebugDirectoryData(entry).Guid, entry.Stamp);
                }
            }

            throw new InvalidDataException(ConverterResources.SpecifiedPEFileHasNoAssociatedPdb);
        }

        private static MethodDefinitionHandle ReadEntryPointHandle(ISymUnmanagedReader symReader)
        {
            var handle = MetadataTokens.EntityHandle(symReader.GetUserEntryPoint());
            if (handle.IsNil)
            {
                return default(MethodDefinitionHandle);
            }

            if (handle.Kind != HandleKind.MethodDefinition)
            {
                // TODO: warning ConverterResources.InvalidUserEntryPointInSourcePdb;
                return default(MethodDefinitionHandle);
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

            public override bool Equals(object obj) => obj is ImportScopeInfo && Equals((ImportScopeInfo)obj);
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

        private static ImmutableArray<ImportInfo> ParseImportStrings(ImmutableArray<string> importStrings, bool vbSemantics)
        {
            var builder = ArrayBuilder<ImportInfo>.GetInstance();
            foreach (var importString in importStrings)
            {
                ImportInfo import;
                if (TryParseImportString(importString, out import, vbSemantics))
                {
                    builder.Add(import);
                }
            }

            return builder.ToImmutableAndFree();
        }

        private static BlobHandle SerializeModuleImportScope(
            MetadataBuilder metadataBuilder,
            IEnumerable<ImportInfo> csExternAliasImports,
            IEnumerable<ImportInfo> vbProjectLevelImports,
            string vbDefaultNamespace,
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

        private struct ImportInfo
        {
            public readonly ImportTargetKind Kind;
            public readonly string Target;
            public readonly string ExternAlias;
            public readonly string Alias;
            public readonly VBImportScopeKind Scope;

            public ImportInfo(ImportTargetKind kind, string target, string alias, string externAlias, VBImportScopeKind scope)
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

        private static void AddScopesRecursive(
            List<(int, int, ISymUnmanagedVariable[], ISymUnmanagedConstant[])> builder,
            ISymUnmanagedScope symScope, 
            bool vbSemantics,
            bool isTopScope)
        {
            // VB Windows PDB encode the range as end-inclusive, 
            // all Portable PDBs use end-exclusive encoding.
            int start = symScope.GetStartOffset();
            int end = symScope.GetEndOffset() + (vbSemantics && !isTopScope ? 1 : 0);

            // TODO: Once https://github.com/dotnet/roslyn/issues/8473 is implemented, convert to State Machine Hoisted Variable Scopes CDI
            var symLocals = symScope.GetLocals().Where(l => !l.GetName().StartsWith("$VB$ResumableLocal_")).ToArray();
            var symConstants = symScope.GetConstants();
            var symChildScopes = symScope.GetChildren();

            builder.Add((start, end, symLocals, symConstants));

            int scopeCountBeforeChildren = builder.Count;
            int previousChildScopeEnd = start;
            foreach (ISymUnmanagedScope child in symChildScopes)
            {
                int childScopeStart = child.GetStartOffset();
                int childScopeEnd = child.GetEndOffset();

                // scopes are properly nested:
                if (childScopeStart < previousChildScopeEnd || childScopeEnd > end)
                {
                    // TODO: loc/warning
                    // "Invalid scope IL offset range: [{childScopeStart}, {childScopeEnd})."
                    break;
                }

                previousChildScopeEnd = childScopeEnd;

                AddScopesRecursive(builder, child, vbSemantics, isTopScope: false);
            }

            if (!isTopScope && symLocals.Length == 0 && symConstants.Length == 0 && builder.Count == scopeCountBeforeChildren)
            {
                // remove the current scope, it's empty:
                builder.RemoveAt(builder.Count - 1);
            }
        }

        private static void SerializeScope(
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
            bool vbSemantics,
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

                info = default(DynamicLocalInfo);
                return false;
            }

            foreach (var symVariable in symVariables)
            {
                int slot = symVariable.GetSlot();
                string name = symVariable.GetName();

                lastLocalVariableHandle = metadataBuilder.AddLocalVariable(
                    attributes: (LocalVariableAttributes)symVariable.GetAttributes(),
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
                string name = symConstant.GetName();
                object value = symConstant.GetValue();

                lastLocalConstantHandle = metadataBuilder.AddLocalConstant(
                    name: metadataBuilder.GetOrAddString(name),
                    signature: SerializeConstantSignature(metadataBuilder, metadataModel, symConstant.GetSignature(), value));

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

        private static BlobHandle SerializeSequencePoints(
            MetadataBuilder metadataBuilder,
            int localSignatureRowId,
            ImmutableArray<SymUnmanagedSequencePoint> sequencePoints,
            Dictionary<string, DocumentHandle> documentIndex,
            out DocumentHandle singleDocumentHandle)
        {
            if (sequencePoints.Length == 0)
            {
                singleDocumentHandle = default(DocumentHandle);
                return default(BlobHandle);
            }

            var writer = new BlobBuilder();

            int previousNonHiddenStartLine = -1;
            int previousNonHiddenStartColumn = -1;

            // header:
            writer.WriteCompressedInteger(localSignatureRowId);

            DocumentHandle previousDocument = TryGetSingleDocument(sequencePoints, documentIndex);
            singleDocumentHandle = previousDocument;

            for (int i = 0; i < sequencePoints.Length; i++)
            {
                var currentDocument = documentIndex[sequencePoints[i].Document.GetName()];
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
                    writer.WriteCompressedInteger((sequencePoints[i].Offset - sequencePoints[i - 1].Offset));
                }
                else
                {
                    writer.WriteCompressedInteger(sequencePoints[i].Offset);
                }

                if (sequencePoints[i].IsHidden)
                {
                    writer.WriteInt16(0);
                    continue;
                }

                // Delta Lines & Columns:
                SerializeDeltaLinesAndColumns(writer, sequencePoints[i]);

                // delta Start Lines & Columns:
                if (previousNonHiddenStartLine < 0)
                {
                    Debug.Assert(previousNonHiddenStartColumn < 0);
                    writer.WriteCompressedInteger(sequencePoints[i].StartLine);
                    writer.WriteCompressedInteger(sequencePoints[i].StartColumn);
                }
                else
                {
                    writer.WriteCompressedSignedInteger(sequencePoints[i].StartLine - previousNonHiddenStartLine);
                    writer.WriteCompressedSignedInteger(sequencePoints[i].StartColumn - previousNonHiddenStartColumn);
                }

                previousNonHiddenStartLine = sequencePoints[i].StartLine;
                previousNonHiddenStartColumn = sequencePoints[i].StartColumn;
            }

            return metadataBuilder.GetOrAddBlob(writer);
        }

        private static DocumentHandle TryGetSingleDocument(ImmutableArray<SymUnmanagedSequencePoint> sequencePoints, Dictionary<string, DocumentHandle> documentIndex)
        {
            DocumentHandle singleDocument = documentIndex[sequencePoints[0].Document.GetName()];
            for (int i = 1; i < sequencePoints.Length; i++)
            {
                if (documentIndex[sequencePoints[i].Document.GetName()] != singleDocument)
                {
                    return default(DocumentHandle);
                }
            }

            return singleDocument;
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
    }
}
