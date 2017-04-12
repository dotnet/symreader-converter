// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    public struct PdbDiagnostic
    {
        public PdbDiagnosticId Id { get; }
        public int Token { get; }
        public object[] Args { get; }

        internal PdbDiagnostic(PdbDiagnosticId id, int token, object[] args)
        {
            Id = id;
            Token = token;
            Args = args;
        }

        public override string ToString() => ToString(CultureInfo.CurrentCulture);

        public string ToString(IFormatProvider formatProvider)
        {
            string location = (Token != 0) ? ": " + string.Format(formatProvider, ConverterResources.DiagnosticLocation, Token) : "";
            return $"PDB{(int)Id:D4}{location}: {GetMessage(formatProvider)}";
        }

        public string GetMessage(IFormatProvider formatProvider)
        {
            if (formatProvider == null)
            {
                throw new ArgumentNullException(nameof(formatProvider));
            }

            var template = GetMessageTemplate();
            return (Args?.Length > 0) ? string.Format(formatProvider, template, Args) : template;
        }

        private string GetMessageTemplate()
        {
            switch (Id)
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

                default:
                    throw ExceptionUtilities.UnexpectedValue(Id);
            }
        }
    }
}
