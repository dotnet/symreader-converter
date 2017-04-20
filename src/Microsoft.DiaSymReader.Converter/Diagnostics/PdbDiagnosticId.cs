// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    public enum PdbDiagnosticId
    {
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
        UriSchemeIsNotHttp = 22,
        NoSupportedUrisFoundInSourceLink = 23,
        InvalidSourceLinkData = 24,
        InvalidSourceServerData = 25,
        BothSourceLinkDataAndSourceServerData = 26,
    }

    internal static class PdbDiagnosticIdExtensions
    {
        internal static string GetMessageTemplate(this PdbDiagnosticId id)
        {
            switch (id)
            {
                case default(PdbDiagnosticId):
                    return null;

                case PdbDiagnosticId.MethodAssociatedWithLocalScopeHasNoBody: return ConverterResources.MethodAssociatedWithLocalScopeHasNoBody;
                case PdbDiagnosticId.LocalConstantNameTooLong: return ConverterResources.LocalConstantNameTooLong;
                case PdbDiagnosticId.LocalVariableNameTooLong: return ConverterResources.LocalVariableNameTooLong;
                case PdbDiagnosticId.MethodContainingLocalVariablesHasNoLocalSignature: return ConverterResources.MethodContainingLocalVariablesHasNoLocalSignature;
                case PdbDiagnosticId.LocalScopeRangesNestingIsInvalid: return ConverterResources.LocalScopeRangesNestingIsInvalid;
                case PdbDiagnosticId.UnsupportedImportType: return ConverterResources.UnsupportedImportType;
                case PdbDiagnosticId.UndefinedAssemblyReferenceAlias: return ConverterResources.UndefinedAssemblyReferenceAlias;
                case PdbDiagnosticId.UnknownImportDefinitionKind: return ConverterResources.UnknownImportDefinitionKind;
                case PdbDiagnosticId.InvalidStateMachineTypeName: return ConverterResources.InvalidStateMachineTypeName;
                case PdbDiagnosticId.BothStateMachineTypeNameAndImportsSpecified: return ConverterResources.BothStateMachineTypeNameAndImportsSpecified;
                case PdbDiagnosticId.DuplicateDynamicLocals: return ConverterResources.DuplicateDynamicLocals;
                case PdbDiagnosticId.DuplicateTupleElementNamesForSlot: return ConverterResources.DuplicateTupleElementNamesForSlot;
                case PdbDiagnosticId.DuplicateTupleElementNamesForConstant: return ConverterResources.DuplicateTupleElementNamesForConstant;
                case PdbDiagnosticId.InvalidImportStringFormat: return ConverterResources.InvalidImportStringFormat;
                case PdbDiagnosticId.InvalidEntryPointToken: return ConverterResources.InvalidEntryPointToken;
                case PdbDiagnosticId.InvalidScopeILOffsetRange: return ConverterResources.InvalidScopeILOffsetRange;
                case PdbDiagnosticId.InvalidLocalConstantData: return ConverterResources.InvalidLocalConstantData;
                case PdbDiagnosticId.InvalidLocalConstantSignature: return ConverterResources.InvalidLocalConstantSignature;
                case PdbDiagnosticId.InvalidLocalScope: return ConverterResources.InvalidLocalScope;
                case PdbDiagnosticId.InvalidSequencePointDocument: return ConverterResources.InvalidSequencePointDocument;
                case PdbDiagnosticId.UnmappedDocumentName: return ConverterResources.UnmappedDocumentName;
                case PdbDiagnosticId.UriSchemeIsNotHttp: return ConverterResources.UriSchemeIsNotHttp;
                case PdbDiagnosticId.NoSupportedUrisFoundInSourceLink: return ConverterResources.NoSupportedUrisFoundInSourceLink;
                case PdbDiagnosticId.InvalidSourceLinkData: return ConverterResources.InvalidSourceLinkData;
                case PdbDiagnosticId.InvalidSourceServerData: return ConverterResources.InvalidSourceServerData;
                case PdbDiagnosticId.BothSourceLinkDataAndSourceServerData: return ConverterResources.BothSourceLinkDataAndSourceServerData;

                default:
                    throw ExceptionUtilities.UnexpectedValue(id);
            }
        }
    }
}