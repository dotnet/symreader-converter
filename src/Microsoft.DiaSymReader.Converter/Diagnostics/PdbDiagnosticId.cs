// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }
}