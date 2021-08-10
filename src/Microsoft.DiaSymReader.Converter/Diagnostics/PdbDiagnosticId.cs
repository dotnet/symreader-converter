// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    public enum PdbDiagnosticId
    {
        None = 0,
        MethodAssociatedWithLocalScopeHasNoBody = 1,
        LocalConstantNameTooLong = 2,
        LocalVariableNameTooLong = 3,
        MethodContainingLocalVariablesHasNoLocalSignature = 4,
        LocalScopeRangesNestingIsInvalid = 5,
        UnsupportedImportType = 6,
        UndefinedAssemblyReferenceAlias = 7,
        UnknownImportDefinitionKind = 8,
        InvalidStateMachineTypeName = 9,
        BothStateMachineTypeNameAndImportsSpecified = 10,
        DuplicateDynamicLocals = 11,
        DuplicateTupleElementNamesForSlot = 12,
        DuplicateTupleElementNamesForConstant = 13,
        InvalidImportStringFormat = 14,
        InvalidEntryPointToken = 15,
        InvalidScopeILOffsetRange = 16,
        InvalidLocalConstantData = 17,
        InvalidLocalConstantSignature = 18,
        InvalidLocalScope = 19,
        InvalidSequencePointDocument = 20,
        UnmappedDocumentName = 21,
        UrlSchemeIsNotHttp = 22,
        NoSupportedUrlsFoundInSourceLink = 23,
        InvalidSourceLinkData = 24,
        InvalidSourceServerData = 25,
        InvalidEmbeddedSource = 26,
        InconsistentStateMachineMethodMapping = 27,
        InvalidSourceLink = 28,
        MalformedSourceLinkUrl = 29,
        SourceChecksumAlgorithmSizeMismatch = 30,
    }

    public static class PdbDiagnosticIdExtensions
    {
        public static bool IsValid(this PdbDiagnosticId id)
            => id >= 0 && id <= PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch;

        internal static string? GetMessageTemplate(this PdbDiagnosticId id)
        {
            return id switch
            {
                PdbDiagnosticId.None => null,
                PdbDiagnosticId.MethodAssociatedWithLocalScopeHasNoBody => ConverterResources.MethodAssociatedWithLocalScopeHasNoBody,
                PdbDiagnosticId.LocalConstantNameTooLong => ConverterResources.LocalConstantNameTooLong,
                PdbDiagnosticId.LocalVariableNameTooLong => ConverterResources.LocalVariableNameTooLong,
                PdbDiagnosticId.MethodContainingLocalVariablesHasNoLocalSignature => ConverterResources.MethodContainingLocalVariablesHasNoLocalSignature,
                PdbDiagnosticId.LocalScopeRangesNestingIsInvalid => ConverterResources.LocalScopeRangesNestingIsInvalid,
                PdbDiagnosticId.UnsupportedImportType => ConverterResources.UnsupportedImportType,
                PdbDiagnosticId.UndefinedAssemblyReferenceAlias => ConverterResources.UndefinedAssemblyReferenceAlias,
                PdbDiagnosticId.UnknownImportDefinitionKind => ConverterResources.UnknownImportDefinitionKind,
                PdbDiagnosticId.InvalidStateMachineTypeName => ConverterResources.InvalidStateMachineTypeName,
                PdbDiagnosticId.BothStateMachineTypeNameAndImportsSpecified => ConverterResources.BothStateMachineTypeNameAndImportsSpecified,
                PdbDiagnosticId.DuplicateDynamicLocals => ConverterResources.DuplicateDynamicLocals,
                PdbDiagnosticId.DuplicateTupleElementNamesForSlot => ConverterResources.DuplicateTupleElementNamesForSlot,
                PdbDiagnosticId.DuplicateTupleElementNamesForConstant => ConverterResources.DuplicateTupleElementNamesForConstant,
                PdbDiagnosticId.InvalidImportStringFormat => ConverterResources.InvalidImportStringFormat,
                PdbDiagnosticId.InvalidEntryPointToken => ConverterResources.InvalidEntryPointToken,
                PdbDiagnosticId.InvalidScopeILOffsetRange => ConverterResources.InvalidScopeILOffsetRange,
                PdbDiagnosticId.InvalidLocalConstantData => ConverterResources.InvalidLocalConstantData,
                PdbDiagnosticId.InvalidLocalConstantSignature => ConverterResources.InvalidLocalConstantSignature,
                PdbDiagnosticId.InvalidLocalScope => ConverterResources.InvalidLocalScope,
                PdbDiagnosticId.InvalidSequencePointDocument => ConverterResources.InvalidSequencePointDocument,
                PdbDiagnosticId.UnmappedDocumentName => ConverterResources.UnmappedDocumentName,
                PdbDiagnosticId.UrlSchemeIsNotHttp => ConverterResources.UrlSchemeIsNotHttp,
                PdbDiagnosticId.NoSupportedUrlsFoundInSourceLink => ConverterResources.NoSupportedUrlsFoundInSourceLink,
                PdbDiagnosticId.InvalidSourceLinkData => ConverterResources.InvalidSourceLinkData,
                PdbDiagnosticId.InvalidSourceServerData => ConverterResources.InvalidSourceServerData,
                PdbDiagnosticId.InvalidEmbeddedSource => ConverterResources.InvalidEmbeddedSource,
                PdbDiagnosticId.InconsistentStateMachineMethodMapping => ConverterResources.InconsistentStateMachineMethodMapping,
                PdbDiagnosticId.InvalidSourceLink => ConverterResources.InvalidSourceLink,
                PdbDiagnosticId.MalformedSourceLinkUrl => ConverterResources.MalformedSourceLinkUrl,
                PdbDiagnosticId.SourceChecksumAlgorithmSizeMismatch => ConverterResources.SourceChecksumAlgorithmSizeMismatch,
                _ => throw ExceptionUtilities.UnexpectedValue(id),
            };
        }
    }
}
