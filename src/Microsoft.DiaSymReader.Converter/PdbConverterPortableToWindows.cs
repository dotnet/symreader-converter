// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.DiaSymReader.PortablePdb;
using Microsoft.SourceLink.Tools;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed class PdbConverterPortableToWindows
    {
        private readonly Action<PdbDiagnostic>? _diagnosticReporter;

        public PdbConverterPortableToWindows(Action<PdbDiagnostic>? diagnosticReporter)
        {
            _diagnosticReporter = diagnosticReporter;
        }

        private void ReportDiagnostic(PdbDiagnosticId id, int token, params object[] args)
        {
            _diagnosticReporter?.Invoke(new PdbDiagnostic(id, token, args));
        }

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
            return (languageGuid == PdbGuids.Language.CSharp || languageGuid == PdbGuids.Language.VisualBasic || languageGuid == PdbGuids.Language.FSharp) ?
                PdbGuids.LanguageVendor.Microsoft : default;
        }

        private readonly struct LocalScopeInfo
        {
            public readonly LocalScope? LocalScope;

            public readonly int HoistedVariableStartIndex;
            public readonly int HoistedVariableEndIndex;

            public LocalScopeInfo(LocalScope? localScope, int hoistedVariableStartIndex, int hoistedVariableEndIndex)
            {
                LocalScope = localScope;
                HoistedVariableStartIndex = hoistedVariableStartIndex;
                HoistedVariableEndIndex = hoistedVariableEndIndex;
            }

            public bool IsDefault => !IsLocalScope && !IsHoistedScope;
            public bool IsLocalScope => LocalScope.HasValue;
            public bool IsHoistedScope => HoistedVariableStartIndex < HoistedVariableEndIndex;
        }

        internal void Convert(PEReader peReader, MetadataReader pdbReader, SymUnmanagedWriter pdbWriter, PortablePdbConversionOptions options)
        {
            if (!SymReaderHelpers.TryReadPdbId(peReader, out var pePdbId, out int peAge))
            {
                throw new InvalidDataException(ConverterResources.SpecifiedPEFileHasNoAssociatedPdb);
            }

            if (pdbReader.DebugMetadataHeader == null || !new BlobContentId(pdbReader.DebugMetadataHeader.Id).Equals(pePdbId))
            {
                throw new InvalidDataException(ConverterResources.PdbNotMatchingDebugDirectory);
            }

            string? vbDefaultNamespace = MetadataUtilities.GetVisualBasicDefaultNamespace(pdbReader);
            bool vbSemantics = vbDefaultNamespace != null;
            string? vbDefaultNamespaceImportString = string.IsNullOrEmpty(vbDefaultNamespace) ? null : "*" + vbDefaultNamespace;

            var metadataReader = peReader.GetMetadataReader();
            var metadataModel = new MetadataModel(metadataReader, vbSemantics);

            var nonEmbeddedDocumentNames = new ArrayBuilder<string>(pdbReader.Documents.Count);
            var symSequencePointsWriter = new SymUnmanagedSequencePointsWriter(pdbWriter, capacity: 64);
            var declaredExternAliases = new HashSet<string>();
            var importStringsBuilder = new List<string>();
            var importGroups = new List<int>();
            var cdiBuilder = new BlobBuilder();
            var dynamicLocals = new List<(string LocalName, byte[] Flags, int Count, int SlotIndex)>();
            var tupleLocals = new List<(string LocalName, int SlotIndex, int ScopeStart, int ScopeEnd, ImmutableArray<string?> Names)>();
            var openScopeEndOffsets = new Stack<int>();

            // state for calculating import string forwarding:
            var lastImportScopeHandle = default(ImportScopeHandle);
            var vbLastImportScopeNamespace = default(string);
            var lastImportScopeMethodDefHandle = default(MethodDefinitionHandle);
            var importStringsMap = new Dictionary<ImmutableArray<string>, MethodDefinitionHandle>(SequenceComparer<string>.Instance);

            var aliasedAssemblyRefs = GetAliasedAssemblyRefs(pdbReader);
            pdbWriter.DocumentTableCapacity = pdbReader.Documents.Count;

            foreach (var documentHandle in pdbReader.Documents)
            {
                var document = pdbReader.GetDocument(documentHandle);
                var languageGuid = pdbReader.GetGuid(document.Language);
                var name = pdbReader.GetString(document.Name);

                var embeddedSourceHandle = pdbReader.GetCustomDebugInformation(documentHandle, PortableCustomDebugInfoKinds.EmbeddedSource);
                var sourceBlob = embeddedSourceHandle.IsNil ? null : pdbReader.GetBlobBytes(embeddedSourceHandle);

                if (embeddedSourceHandle.IsNil)
                {
                    nonEmbeddedDocumentNames.Add(name);
                }

                var algorithmId = pdbReader.GetGuid(document.HashAlgorithm);
                var checksum = pdbReader.GetBlobBytes(document.Hash);
                ValidateAndCorrectSourceChecksum(ref algorithmId, checksum, name);

                pdbWriter.DefineDocument(
                    name: name,
                    language: languageGuid,
                    vendor: GetLanguageVendorGuid(languageGuid),
                    type: PdbGuids.DocumentType.Text,
                    algorithmId: algorithmId,
                    checksum: checksum,
                    source: sourceBlob);
            }

            int localScopeRowNumber = 0;
            int localScopeCount = pdbReader.GetTableRowCount(TableIndex.LocalScope);

            static int CompareScopeRanges(int leftStart, int leftEnd, int rightStart, int rightEnd)
            {
                int result = leftStart.CompareTo(rightStart);
                return (result != 0) ? result : rightEnd.CompareTo(leftEnd);
            }
            
            // Handle of the method that is gonna contain list of AssemblyRef aliases.
            // Other methods will forward to it.
            var methodDefHandleWithAssemblyRefAliases = default(MethodDefinitionHandle);

            foreach (var methodDebugInfoHandle in pdbReader.MethodDebugInformation)
            {
                var methodDebugInfo = pdbReader.GetMethodDebugInformation(methodDebugInfoHandle);
                var methodDefHandle = methodDebugInfoHandle.ToDefinitionHandle();
                int methodToken = MetadataTokens.GetToken(methodDefHandle);
                var methodDef = metadataReader.GetMethodDefinition(methodDefHandle);
#if DEBUG
                var declaringTypeDef = metadataReader.GetTypeDefinition(methodDef.GetDeclaringType());
                var typeName = metadataReader.GetString(declaringTypeDef.Name);
                var methodName = metadataReader.GetString(methodDef.Name);
#endif
                bool methodOpened = false;

                var methodBodyOpt = (methodDef.RelativeVirtualAddress != 0 && (methodDef.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Managed) ? 
                    peReader.GetMethodBody(methodDef.RelativeVirtualAddress) : null;

                var vbCurrentMethodNamespace = vbSemantics ? GetMethodNamespace(metadataReader, methodDef) : null;
                var moveNextHandle = metadataModel.FindStateMachineMoveNextMethod(methodDefHandle, vbSemantics);
                bool isKickOffMethod = !moveNextHandle.IsNil;
                var hoistedLocalScopes = isKickOffMethod ? default : GetStateMachineHoistedLocalScopes(pdbReader, methodDefHandle);

                int vbHoistedScopeIndex = -1;
                ImmutableArray<(StateMachineHoistedLocalScope Scope, int Index)> vbHoistedScopes;
                
                // Names of local variables hoisted to state machine fields indexed by variable slot index extracted from the field name.
                ImmutableArray<string> vbResumableLocalNames;

                if (vbSemantics && !hoistedLocalScopes.IsDefault)
                {
                    // we are in MoveNext method
                    vbHoistedScopes = hoistedLocalScopes.SelectWithIndex().Where(s => !s.Value.IsDefault).ToImmutableArray().
                        Sort((left, right) => CompareScopeRanges(left.Value.StartOffset, left.Value.EndOffset, right.Value.StartOffset, right.Value.EndOffset));

                    vbResumableLocalNames = metadataModel.GetVisualBasicHoistedLocalNames(methodDef.GetDeclaringType());
                }
                else
                {
                    vbHoistedScopes = default;
                    vbResumableLocalNames = default;
                }

                var forwardImportScopesToMethodDef = default(MethodDefinitionHandle);
                Debug.Assert(dynamicLocals.Count == 0);
                Debug.Assert(tupleLocals.Count == 0);
                Debug.Assert(openScopeEndOffsets.Count == 0);

                void LazyOpenMethod()
                {
                    if (!methodOpened)
                    {
#if DEBUG
                        Debug.WriteLine($"Open Method '{typeName}::{methodName}' {methodToken:X8}");
#endif
                        pdbWriter.OpenMethod(methodToken);
                        methodOpened = true;
                    }
                }

                LocalScopeInfo GetNextLocalScope()
                {
                    Debug.Assert(localScopeRowNumber <= localScopeCount);

                    LocalScope? nextLocalScope = null;
                    int nextLocalScopeIndex = localScopeRowNumber + 1;
                    if (nextLocalScopeIndex <= localScopeCount)
                    {
                        var scope = pdbReader.GetLocalScope(MetadataTokens.LocalScopeHandle(nextLocalScopeIndex));
                        if (scope.Method == methodDefHandle)
                        {
                            nextLocalScope = scope;
                        }
                    }

                    StateMachineHoistedLocalScope? nextHoistedScope = null;
                    int nextHoistedScopeIndex = vbHoistedScopeIndex + 1;
                    if (!vbHoistedScopes.IsDefault && nextHoistedScopeIndex < vbHoistedScopes.Length)
                    {
                        nextHoistedScope = vbHoistedScopes[nextHoistedScopeIndex].Scope;
                    }

                    int c = nextLocalScope == null ? +1 :
                            nextHoistedScope == null ? -1 :
                            CompareScopeRanges(nextLocalScope.Value.StartOffset, nextLocalScope.Value.EndOffset, nextHoistedScope.Value.StartOffset, nextHoistedScope.Value.EndOffset);

                    int hoistedScopeStartIndex = 0;
                    int hoistedScopeEndIndex = 0;

                    if (c <= 0 && nextLocalScope != null)
                    {
                        localScopeRowNumber = nextLocalScopeIndex;
                    }
                    else
                    {
                        nextLocalScope = null;
                    }

                    if (c >= 0 && nextHoistedScope != null)
                    {
                        static bool ScopeEquals(StateMachineHoistedLocalScope left, StateMachineHoistedLocalScope right)
                            => left.StartOffset == right.StartOffset && left.EndOffset == right.EndOffset;

                        // determine all hoisted variables that have equal scopes:
                        int i = nextHoistedScopeIndex + 1;
                        while (i < vbHoistedScopes.Length && ScopeEquals(nextHoistedScope.Value, vbHoistedScopes[i].Scope))
                        {
                            i++;
                        }

                        hoistedScopeStartIndex = nextHoistedScopeIndex;
                        hoistedScopeEndIndex = i;
                        vbHoistedScopeIndex = i - 1;
                    }
                    else
                    {
                        hoistedScopeStartIndex = hoistedScopeEndIndex = 0;
                    }

                    return new LocalScopeInfo(nextLocalScope, hoistedScopeStartIndex, hoistedScopeEndIndex);
                }

                void CloseOpenScopes(int currentScopeStartOffset)
                {
                    // close all open scopes that end before this scope starts:
                    while (openScopeEndOffsets.Count > 0 && currentScopeStartOffset >= openScopeEndOffsets.Peek())
                    {
                        int scopeEnd = openScopeEndOffsets.Pop();
                        Debug.WriteLine($"Close Scope [.., {scopeEnd})");

                        // Note that the root scope end is not end-inclusive in VB:
                        pdbWriter.CloseScope(AdjustEndScopeOffset(scopeEnd, isEndInclusive: vbSemantics && openScopeEndOffsets.Count > 0));
                    }
                }

                bool hasAnyScopes = false;

                void OpenScope(int startOffset, int endOffset)
                {
                    CloseOpenScopes(startOffset);

                    Debug.WriteLine($"Open Scope [{startOffset}, {endOffset})");
                    pdbWriter.OpenScope(startOffset);
                    openScopeEndOffsets.Push(endOffset);

                    hasAnyScopes = true;
                }

                bool isFirstMethodLocalScope = true;
                while (true)
                {
                    var currentLocalScope = GetNextLocalScope();
                    if (currentLocalScope.IsDefault)
                    {
                        break;
                    }

                    // kickoff methods don't have any scopes emitted to Windows PDBs
                    if (methodBodyOpt == null)
                    {
                        ReportDiagnostic(PdbDiagnosticId.MethodAssociatedWithLocalScopeHasNoBody, MetadataTokens.GetToken(MetadataTokens.LocalScopeHandle(localScopeRowNumber)));
                    }
                    else if (!isKickOffMethod)
                    {
                        LazyOpenMethod();

                        if (currentLocalScope.IsLocalScope)
                        {
                            var localScope = currentLocalScope.LocalScope!.Value;
                            OpenScope(localScope.StartOffset, localScope.EndOffset);

                            if (isFirstMethodLocalScope)
                            {
                                if (lastImportScopeHandle == localScope.ImportScope && vbLastImportScopeNamespace == vbCurrentMethodNamespace)
                                {
                                    // forward to a method that has the same imports:
                                    forwardImportScopesToMethodDef = lastImportScopeMethodDefHandle;
                                }
                                else
                                {
                                    Debug.Assert(importStringsBuilder.Count == 0);
                                    Debug.Assert(declaredExternAliases.Count == 0);
                                    Debug.Assert(importGroups.Count == 0);

                                    AddImportStrings(importStringsBuilder, importGroups, declaredExternAliases, pdbReader, metadataModel, localScope.ImportScope, aliasedAssemblyRefs, vbDefaultNamespaceImportString, vbCurrentMethodNamespace, vbSemantics);
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
                                        importStringsMap[importStrings] = methodDefHandle;
                                    }

                                    lastImportScopeHandle = localScope.ImportScope;
                                    vbLastImportScopeNamespace = vbCurrentMethodNamespace;
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

                            foreach (var localVariableHandle in localScope.GetLocalVariables())
                            {
                                var variable = pdbReader.GetLocalVariable(localVariableHandle);
                                string name = pdbReader.GetString(variable.Name);

                                if (name.Length > MaxEntityNameLength)
                                {
                                    ReportDiagnostic(PdbDiagnosticId.LocalConstantNameTooLong, MetadataTokens.GetToken(localVariableHandle));
                                    continue;
                                }

                                if (methodBodyOpt.LocalSignature.IsNil)
                                {
                                    ReportDiagnostic(PdbDiagnosticId.MethodContainingLocalVariablesHasNoLocalSignature, methodToken);
                                    continue;
                                }

                                pdbWriter.DefineLocalVariable(variable.Index, name, (int)variable.Attributes, MetadataTokens.GetToken(methodBodyOpt.LocalSignature));

                                var dynamicFlags = MetadataUtilities.ReadDynamicCustomDebugInformation(pdbReader, localVariableHandle);
                                if (TryGetDynamicLocal(name, variable.Index, dynamicFlags, out var dynamicLocal))
                                {
                                    dynamicLocals.Add(dynamicLocal);
                                }

                                var tupleElementNames = MetadataUtilities.ReadTupleCustomDebugInformation(pdbReader, localVariableHandle);
                                if (!tupleElementNames.IsDefaultOrEmpty)
                                {
                                    tupleLocals.Add((name, SlotIndex: variable.Index, ScopeStart: 0, ScopeEnd: 0, Names: tupleElementNames));
                                }
                            }

                            foreach (var localConstantHandle in localScope.GetLocalConstants())
                            {
                                var constant = pdbReader.GetLocalConstant(localConstantHandle);
                                string name = pdbReader.GetString(constant.Name);

                                if (name.Length > MaxEntityNameLength)
                                {
                                    ReportDiagnostic(PdbDiagnosticId.LocalConstantNameTooLong, MetadataTokens.GetToken(localConstantHandle));
                                    continue;
                                }

                                var (value, signature) = PortableConstantSignature.GetConstantValueAndSignature(pdbReader, localConstantHandle, metadataReader.GetQualifiedTypeName);
                                if (!metadataModel.TryGetStandaloneSignatureHandle(signature, out var constantSignatureHandle))
                                {
                                    // Signature will be unspecified. At least we store the name and the value.
                                    constantSignatureHandle = default;
                                }

                                pdbWriter.DefineLocalConstant(name, value, MetadataTokens.GetToken(constantSignatureHandle));

                                var dynamicFlags = MetadataUtilities.ReadDynamicCustomDebugInformation(pdbReader, localConstantHandle);
                                if (TryGetDynamicLocal(name, 0, dynamicFlags, out var dynamicLocal))
                                {
                                    dynamicLocals.Add(dynamicLocal);
                                }

                                var tupleElementNames = MetadataUtilities.ReadTupleCustomDebugInformation(pdbReader, localConstantHandle);
                                if (!tupleElementNames.IsDefaultOrEmpty)
                                {
                                    // Note that the end offset of tuple locals is always end-exclusive, regardless of whether the PDB uses VB semantics or not.
                                    tupleLocals.Add((name, SlotIndex: -1, ScopeStart: localScope.StartOffset, ScopeEnd: localScope.EndOffset, Names: tupleElementNames));
                                }
                            }
                        }

                        if (currentLocalScope.IsHoistedScope)
                        {
                            Debug.Assert(vbSemantics);
                            Debug.Assert(!vbResumableLocalNames.IsDefault);

                            bool scopeOpened = currentLocalScope.IsLocalScope;

                            // add $VB$ResumableLocal pseudo-variables:
                            for (int i = currentLocalScope.HoistedVariableStartIndex; i < currentLocalScope.HoistedVariableEndIndex; i++)
                            {
                                var (scope, index) = vbHoistedScopes[i];
                                if (index >= vbResumableLocalNames.Length)
                                {
                                    // TODO: report error
                                    continue;
                                }

                                if (!scopeOpened)
                                {
                                    OpenScope(scope.StartOffset, scope.EndOffset);
                                    scopeOpened = true;
                                }

                                pdbWriter.DefineLocalVariable(
                                    index,
                                    vbResumableLocalNames[index],
                                    attributes: 0,
                                    localSignatureToken: MetadataTokens.GetToken(methodBodyOpt.LocalSignature));
                            }
                        }
                    }

                    isFirstMethodLocalScope = false;
                }

                CloseOpenScopes(int.MaxValue);
                if (openScopeEndOffsets.Count > 0)
                {
                    ReportDiagnostic(PdbDiagnosticId.LocalScopeRangesNestingIsInvalid, methodToken);
                    openScopeEndOffsets.Clear();
                }

                if (!methodDebugInfo.SequencePointsBlob.IsNil)
                {
                    LazyOpenMethod();
                    WriteSequencePoints(symSequencePointsWriter, methodDebugInfo.GetSequencePoints(), pdbReader.Documents.Count, methodToken);
                }

                // async method data:
                var asyncData = MetadataUtilities.ReadAsyncMethodData(pdbReader, methodDebugInfoHandle);
                if (!asyncData.IsNone)
                {
                    LazyOpenMethod();
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
                    if (!vbSemantics && hasAnyScopes)
                    {
                        if (forwardImportScopesToMethodDef.IsNil)
                        {
                            // record the number of import strings in each scope:
                            cdiEncoder.AddUsingGroups(importGroups);

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

                    if (!vbSemantics && !hoistedLocalScopes.IsDefault)
                    {
                        cdiEncoder.AddStateMachineHoistedLocalScopes(hoistedLocalScopes);
                    }

                    if (dynamicLocals.Count > 0)
                    {
                        cdiEncoder.AddDynamicLocals(dynamicLocals);
                        dynamicLocals.Clear();
                    }

                    if (tupleLocals.Count > 0)
                    {
                        cdiEncoder.AddTupleElementNames(tupleLocals);
                        tupleLocals.Clear();
                    }
                }

                importGroups.Clear();

                // the following blobs map 1:1
                CopyCustomDebugInfoRecord(ref cdiEncoder, pdbReader, methodDefHandle, PortableCustomDebugInfoKinds.EncLocalSlotMap, CustomDebugInfoKind.EditAndContinueLocalSlotMap);
                CopyCustomDebugInfoRecord(ref cdiEncoder, pdbReader, methodDefHandle, PortableCustomDebugInfoKinds.EncLambdaAndClosureMap, CustomDebugInfoKind.EditAndContinueLambdaMap);
                CopyCustomDebugInfoRecord(ref cdiEncoder, pdbReader, methodDefHandle, SymReaderHelpers.EncStateMachineSuspensionPoints, SymReaderHelpers.CustomDebugInfoKind_EncStateMachineSuspensionPoints);

                if (cdiEncoder.RecordCount > 0)
                {
                    LazyOpenMethod();
                    pdbWriter.DefineCustomMetadata(cdiEncoder.ToArray());
                }

                cdiBuilder.Clear();

                if (methodOpened && aliasedAssemblyRefs.Length > 0 && !isKickOffMethod && methodDefHandleWithAssemblyRefAliases.IsNil)
                {
                    methodDefHandleWithAssemblyRefAliases = methodDefHandle;
                }

                if (methodOpened)
                {
                    Debug.WriteLine($"Close Method {methodToken:X8}");
                    pdbWriter.CloseMethod();
                }
            }

            if (!pdbReader.DebugMetadataHeader.EntryPoint.IsNil)
            {
                pdbWriter.SetEntryPoint(MetadataTokens.GetToken(pdbReader.DebugMetadataHeader.EntryPoint));
            }

            var sourceLinkHandle = pdbReader.GetCustomDebugInformation(EntityHandle.ModuleDefinition, PortableCustomDebugInfoKinds.SourceLink);
            if (!sourceLinkHandle.IsNil)
            {
                // always include Source Link
                pdbWriter.SetSourceLinkData(pdbReader.GetBlobBytes(sourceLinkHandle));

                if (!options.SuppressSourceLinkConversion)
                {
                    var srcsvrData = ConvertSourceServerData(pdbReader.GetStringUTF8(sourceLinkHandle), nonEmbeddedDocumentNames, options);

                    // an error has been reported:
                    if (srcsvrData != null)
                    {
                        pdbWriter.SetSourceServerData(Encoding.UTF8.GetBytes(srcsvrData));
                    }
                }
            }

            var compilerOptions = ReadCompilationOptions(pdbReader).ToImmutableDictionary();
            if (compilerOptions.TryGetValue("compiler-version", out var compilerVersionString) &&
                compilerOptions.TryGetValue("language", out var language) &&
                language is "C#" or "Visual Basic" or "F#" &&
                TryConvertCompilerVersionToFileVersion(compilerVersionString, out var fileMajor, out var fileMinor, out var fileBuild, out var fileRevision))
            {
                pdbWriter.AddCompilerInfo(fileMajor, fileMinor, fileBuild, fileRevision, $"{language} - {compilerVersionString}");
            }

            SymReaderHelpers.GetWindowsPdbSignature(pdbReader.DebugMetadataHeader.Id, out var guid, out var stamp, out var age);
            pdbWriter.UpdateSignature(guid, stamp, age);
        }

        private static IEnumerable<KeyValuePair<string, string>> ReadCompilationOptions(MetadataReader reader)
        {
            var compilerOptions = reader.GetCustomDebugInformation(EntityHandle.ModuleDefinition, PortableCustomDebugInfoKinds.CompilationOptions);
            if (compilerOptions.IsNil)
            {
                yield break;
            }

            var blobReader = reader.GetBlobReader(compilerOptions);

            // Compiler flag bytes are UTF-8 null-terminated key-value pairs
            var nullIndex = blobReader.IndexOf(0);
            while (nullIndex >= 0)
            {
                var key = blobReader.ReadUTF8(nullIndex);

                // Skip the null terminator
                blobReader.ReadByte();

                nullIndex = blobReader.IndexOf(0);
                var value = blobReader.ReadUTF8(nullIndex);

                yield return new KeyValuePair<string, string>(key, value);

                // Skip the null terminator
                blobReader.ReadByte();
                nullIndex = blobReader.IndexOf(0);
            }
        }

        /// <summary>
        /// Roslyn compilers use version in AssemblyFileVersionAttribute for the numeric version and
        /// the semantic version stored in AssemblyInformationalVersionAttribute for the version string.
        /// The latter is also stored in compiler-version, but the former is not stored in compiler options.
        /// We calculate it based on the version scheme used by Roslyn build infrastructure.
        /// See https://github.com/dotnet/arcade/blob/release/6.0/Documentation/CorePackages/Versioning.md
        /// </summary>
        internal static bool TryConvertCompilerVersionToFileVersion(string str, out ushort fileMajor, out ushort fileMinor, out ushort fileBuild, out ushort fileRevision)
        {
            // assumes format: major.minor.revision-labels.SHORT_DATE.REVISION+CommitSha
            var match = Regex.Match(str, "([0-9]+)[.]([0-9]+)[.]([0-9]+)-[^.]+[.]([0-9]+)[.]([0-9]+).*");
            if (match.Success &&
                ushort.TryParse(match.Groups[1].Value, out var major) &&
                ushort.TryParse(match.Groups[2].Value, out var minor) &&
                ushort.TryParse(match.Groups[3].Value, out var build) &&
                ushort.TryParse(match.Groups[4].Value, out var shortDate) &&
                ushort.TryParse(match.Groups[5].Value, out var revision))
            {
                var dd = shortDate % 50;
                var mm = (shortDate / 50) % 20;
                var yy = shortDate / 1000;

                fileMajor = major;
                try
                {
                    checked
                    {
                        fileMinor = (ushort)(minor * 100 + build / 100);
                        fileBuild = (ushort)((build % 100) * 100 + yy);
                        fileRevision = (ushort)((50 * mm + dd) * 100 + revision);
                        return true;
                    }
                }
                catch (OverflowException)
                {
                }
            }

            fileMajor = fileMinor = fileBuild = fileRevision = 0;
            return false;
        }

        // internal for testing
        internal void ValidateAndCorrectSourceChecksum(ref Guid algorithmId, byte[] checksum, string documentName)
        {
            const int SizeOfSHA1 = 20;
            const int SizeOfSHA256 = 32;

            int expectedSize;
            string algorithmName;

            if (algorithmId == default)
            {
                expectedSize = 0;
                algorithmName = ConverterResources.None;
            }
            else if (algorithmId == PdbGuids.HashAlgorithm.SHA1)
            {
                expectedSize = SizeOfSHA1;
                algorithmName = nameof(PdbGuids.HashAlgorithm.SHA1);
            }
            else if (algorithmId == PdbGuids.HashAlgorithm.SHA256)
            {
                expectedSize = SizeOfSHA256;
                algorithmName = nameof(PdbGuids.HashAlgorithm.SHA256);
            }
            else
            {
                // ignore unknown algorithms
                return;
            }

            if (checksum.Length != expectedSize)
            {
                ReportDiagnostic(PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch, 0, new[] { algorithmName, documentName });

                if (algorithmId == default)
                {
                    // guess correct algorithm id based on the checksum size:
                    switch (checksum.Length)
                    {
                        case SizeOfSHA1:
                            algorithmId = PdbGuids.HashAlgorithm.SHA1;
                            break;

                        case SizeOfSHA256:
                            algorithmId = PdbGuids.HashAlgorithm.SHA256;
                            break;
                    }
                }
            }
        }

        private static string? GetMethodNamespace(MetadataReader metadataReader, MethodDefinition methodDef)
        {
            var typeDefHandle = methodDef.GetDeclaringType();
            if (typeDefHandle.IsNil)
            {
                return null;
            }

            while (true)
            {
                var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                typeDefHandle = typeDef.GetDeclaringType();
                if (typeDefHandle.IsNil)
                {
                    return metadataReader.GetString(typeDef.Namespace);
                }
            }
        }

        private static int AdjustEndScopeOffset(int exclusiveOffset, bool isEndInclusive) =>
            isEndInclusive ? exclusiveOffset - 1 : exclusiveOffset;

        private static bool TryGetDynamicLocal(
            string name,
            int slotIndex,
            ImmutableArray<bool> flagsOpt,
            out (string LocalName, byte[] Flags, int Count, int SlotIndex) result)
        {
            // C# compiler skips variables with too many flags or too long name:
            if (flagsOpt.IsDefaultOrEmpty || 
                flagsOpt.Length > CustomDebugInfoEncoder.DynamicAttributeSize ||
                name.Length >= CustomDebugInfoEncoder.IdentifierSize)
            {
                result = default((string, byte[], int, int));
                return false;
            }

            var bytes = new byte[Math.Min(flagsOpt.Length, CustomDebugInfoEncoder.DynamicAttributeSize)];
            for (int k = 0; k < bytes.Length; k++)
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
                return default;
            }

            return MetadataUtilities.DecodeHoistedLocalScopes(pdbReader.GetBlobReader(cdiHandle));
        }

        private static string GetIteratorTypeName(MetadataReader metadataReader, MethodDefinitionHandle moveNextHandle)
        {
            // TODO: errors
            var moveNextDef = metadataReader.GetMethodDefinition(moveNextHandle);
            var iteratorType = moveNextDef.GetDeclaringType();
            var name = metadataReader.GetString(metadataReader.GetTypeDefinition(iteratorType).Name);

            // trim generic arity from the name:
            int backtick = name.LastIndexOf('`');
            return (backtick > 0) ? name.Substring(0, backtick) : name;
        }

        private static void WriteImports(SymUnmanagedWriter pdbWriter, ImmutableArray<string> importStrings)
        {
            foreach (var importString in importStrings)
            {
                pdbWriter.UsingNamespace(importString);
            }
        }

        private void AddImportStrings(
            List<string> importStrings,
            List<int> importGroups,
            HashSet<string> declaredExternAliases,
            MetadataReader pdbReader,
            MetadataModel metadataModel,
            ImportScopeHandle importScopeHandle,
            ImmutableArray<(AssemblyReferenceHandle, string)> aliasedAssemblyRefs,
            string? vbDefaultNamespaceImportString,
            string? vbCurrentMethodNamespace,
            bool vbSemantics)
        {
            Debug.Assert(declaredExternAliases.Count == 0);
            AddExternAliases(declaredExternAliases, pdbReader, importScopeHandle);

            while (!importScopeHandle.IsNil)
            {
                var importScope = pdbReader.GetImportScope(importScopeHandle);
                bool isProjectLevel = importScope.Parent.IsNil;

                if (isProjectLevel && vbDefaultNamespaceImportString != null)
                {
                    Debug.Assert(vbSemantics);
                    importStrings.Add(vbDefaultNamespaceImportString);
                }

                int importStringCount = 0;
                foreach (var import in importScope.GetImports())
                {
                    var importString = TryEncodeImport(pdbReader, metadataModel, importScopeHandle, import, declaredExternAliases, aliasedAssemblyRefs, isProjectLevel, vbSemantics);
                    if (importString == null)
                    {
                        // diagnostic already reported if applicable
                        continue;
                    }

                    if (importString.Length > MaxEntityNameLength)
                    {
                        ReportDiagnostic(PdbDiagnosticId.LocalScopeRangesNestingIsInvalid, MetadataTokens.GetToken(importScopeHandle), importString);
                        continue;
                    }

                    importStrings.Add(importString);
                    importStringCount++;
                }

                if (isProjectLevel && vbCurrentMethodNamespace != null)
                {
                    Debug.Assert(vbSemantics);
                    importStrings.Add(vbCurrentMethodNamespace);
                }

                // Skip C# project-level scope if it doesn't include namespaces.
                // Currently regular (non-scripting) C# doesn't support project-level namespace imports.
                if (vbSemantics || !isProjectLevel || importStringCount > 0)
                {
                    importGroups.Add(importStringCount);
                }

                importScopeHandle = importScope.Parent;
            }

            declaredExternAliases.Clear();
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
            // F# doesn't emit import scopes
            if (pdbReader.ImportScopes.Count == 0)
            {
                return ImmutableArray<(AssemblyReferenceHandle, string)>.Empty;
            }

            // C# serialized aliased assembly refs to the first import scope.
            // In Windows PDBs they are attached as CDIs to any method in the assembly and the other methods 
            // have CDI that forwards to it.
            return (from import in pdbReader.GetImportScope(MetadataTokens.ImportScopeHandle(1)).GetImports()
                    where import.Kind == ImportDefinitionKind.AliasAssemblyReference
                    select (import.TargetAssembly, pdbReader.GetStringUTF8(import.Alias))).ToImmutableArray();
        }

        private string? TryEncodeImport(
            MetadataReader pdbReader, 
            MetadataModel metadataModel, 
            ImportScopeHandle importScopeHandle,
            ImportDefinition import,
            HashSet<string> declaredExternAliases,
            ImmutableArray<(AssemblyReferenceHandle, string)> aliasedAssemblyRefs,
            bool isProjectLevel,
            bool vbSemantics)
        {
            string? typeName;
            string namespaceName;

            // See Roslyn implementation: PdbWriter.TryEncodeImport, MetadataWriter.SerializeImport
            switch (import.Kind)
            {
                case ImportDefinitionKind.AliasType:
                case ImportDefinitionKind.ImportType:
                    // C#, VB

                    if (vbSemantics)
                    {
                        // VB doesn't support generics in imports:
                        if (import.TargetType.Kind == HandleKind.TypeSpecification)
                        {
                            return null;
                        }

                        typeName = metadataModel.GetSerializedTypeName(import.TargetType);
                        if (typeName == null)
                        {
                            ReportDiagnostic(PdbDiagnosticId.UnsupportedImportType, MetadataTokens.GetToken(importScopeHandle), MetadataTokens.GetToken(import.TargetType));
                            return null;
                        }

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
                        typeName = metadataModel.GetSerializedTypeName(import.TargetType);
                        if (typeName == null)
                        {
                            ReportDiagnostic(PdbDiagnosticId.UnsupportedImportType, MetadataTokens.GetToken(importScopeHandle), MetadataTokens.GetToken(import.TargetType));
                            return null;
                        }

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
                        return (isProjectLevel ? "@PA:" : "@FA:") + pdbReader.GetStringUTF8(import.Alias) + "=" + namespaceName;
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
                        return (isProjectLevel ? "@P:" : "@F:") + namespaceName;
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

                    // The import string uses extern alias to represent an assembly reference.
                    // Find one that is declared within the current scope.
                    string? assemblyRefAlias = TryGetAssemblyReferenceAlias(import.TargetAssembly, declaredExternAliases, aliasedAssemblyRefs);
                    if (assemblyRefAlias == null)
                    {
                        ReportDiagnostic(PdbDiagnosticId.UndefinedAssemblyReferenceAlias, MetadataTokens.GetToken(importScopeHandle), MetadataTokens.GetToken(import.TargetAssembly));
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
                    ReportDiagnostic(PdbDiagnosticId.UnknownImportDefinitionKind, MetadataTokens.GetToken(importScopeHandle), (int)import.Kind);
                    return null;
            }
        }

        private static string? TryGetAssemblyReferenceAlias(
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

        private void WriteSequencePoints(
            SymUnmanagedSequencePointsWriter symSequencePointsWriter, 
            SequencePointCollection sequencePoints,
            int documentCount,
            int methodToken)
        {
            foreach (var sequencePoint in sequencePoints)
            {
                int documentIndex = MetadataTokens.GetRowNumber(sequencePoint.Document) - 1;
                if (documentIndex > documentCount)
                {
                    ReportDiagnostic(PdbDiagnosticId.InvalidSequencePointDocument, methodToken, MetadataTokens.GetToken(sequencePoint.Document));
                    continue;
                }

                symSequencePointsWriter.Add(
                    documentIndex: documentIndex,
                    offset: sequencePoint.Offset,
                    startLine: sequencePoint.StartLine,
                    startColumn: sequencePoint.StartColumn,
                    endLine: sequencePoint.EndLine,
                    endColumn: sequencePoint.EndColumn);
            }

            symSequencePointsWriter.Flush();
        }

        private const string SrcSvr_RAWURL = "RAWURL";
        private const string SrcSvr_SRCSRVVERCTRL = "SRCSRVVERCTRL";
        private const string SrcSvr_SRCSRVTRG = "SRCSRVTRG";

        // Avoid loading JSON dependency if not needed.
        // Internal for testing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal string? ConvertSourceServerData(string sourceLink, IReadOnlyCollection<string> documentNames, PortablePdbConversionOptions options)
        {
            if (documentNames.Count == 0)
            {
                // no documents in the PDB
                return null;
            }

            SourceLinkMap map;
            try
            {
                map = SourceLinkMap.Parse(sourceLink);
            }
            catch (JsonException e)
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidSourceLink, 0, e.Message);
                return null;
            }
            catch (InvalidDataException)
            {
                ReportDiagnostic(PdbDiagnosticId.InvalidSourceLink, 0, ConverterResources.InvalidJsonDataFormat);
                return null;
            }

            var builder = new StringBuilder();
            var mapping = new List<(string name, string uri)>();

            string? commonScheme = null;
            foreach (string documentName in documentNames)
            {
                if (!map.TryGetUri(documentName, out var uri))
                {
                    ReportDiagnostic(PdbDiagnosticId.UnmappedDocumentName, 0, documentName);
                    continue;
                }

                if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                {
                    ReportDiagnostic(PdbDiagnosticId.MalformedSourceLinkUrl, 0, uri);
                    continue;
                }

                string scheme;
                if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    scheme = "http";
                }
                else if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    scheme = "https";
                }
                else
                {
                    ReportDiagnostic(PdbDiagnosticId.UrlSchemeIsNotHttp, 0, uri);
                    continue;
                }

                if (commonScheme == null)
                {
                    commonScheme = scheme;
                }
                else if (commonScheme != scheme)
                {
                    commonScheme = "http";
                }

                mapping.Add((documentName, uri));
            }

            if (commonScheme == null)
            {
                ReportDiagnostic(PdbDiagnosticId.NoSupportedUrlsFoundInSourceLink, 0);
                return null;
            }

            string commonPrefix = StringUtilities.GetLongestCommonPrefix(mapping.Select(p => p.uri));

            builder.Append("SRCSRV: ini ------------------------------------------------\r\n");
            builder.Append("VERSION=2\r\n");
            builder.Append("SRCSRV: variables ------------------------------------------\r\n");
            builder.Append(SrcSvr_RAWURL + "=");
            builder.Append(commonPrefix);
            builder.Append("%var2%\r\n");
            builder.Append(SrcSvr_SRCSRVVERCTRL + "=");
            builder.Append(commonScheme);
            builder.Append("\r\n");
            builder.Append(SrcSvr_SRCSRVTRG + "=%RAWURL%\r\n");

            foreach (var variable in options.SrcSvrVariables)
            {
                builder.Append(variable.Key);
                builder.Append('=');
                builder.Append(variable.Value);
                builder.Append("\r\n");
            }

            builder.Append("SRCSRV: source files ---------------------------------------\r\n");

            foreach (var (name, uri) in mapping)
            {
                builder.Append(name);
                builder.Append('*');
                builder.Append(uri.Substring(commonPrefix.Length));
                builder.Append("\r\n");
            }

            builder.Append("SRCSRV: end ------------------------------------------------");

            return builder.ToString();
        }

        private static bool IsIdentifierStartChar(char c)
            => c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '_';

        private static bool IsIdentifierChar(char c)
            => IsIdentifierStartChar(c) || c >= '0' && c <= '9';

        internal static void ValidateSrcSvrVariables(ImmutableArray<KeyValuePair<string, string>> variables, string parameterName)
        {
            foreach (var variable in variables)
            {
                ValidateSrcSvrVariable(variable.Key, variable.Value, parameterName);
            }
        }

        internal static void ValidateSrcSvrVariable(string name, string value, string parameterName)
        {
            if (string.IsNullOrEmpty(name) || !IsIdentifierStartChar(name[0]) || !name.All(IsIdentifierChar))
            {
                throw new ArgumentException(parameterName, string.Format(ConverterResources.InvalidSrcSvrVariableName, name));
            }

            if (value == null || value.Any(c => c == '\0' || c == '\r' || c == '\n'))
            {
                throw new ArgumentException(parameterName, string.Format(ConverterResources.InvalidSrcSvrVariableValue, name));
            }

            if (name.Equals(SrcSvr_RAWURL, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(SrcSvr_SRCSRVTRG, StringComparison.OrdinalIgnoreCase) ||
                name.Equals(SrcSvr_SRCSRVVERCTRL, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(parameterName, string.Format(ConverterResources.ReservedSrcSvrVariableName, name));
            }
        }
    }
}
