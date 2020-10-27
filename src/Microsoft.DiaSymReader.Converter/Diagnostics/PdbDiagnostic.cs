// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    public readonly struct PdbDiagnostic : IEquatable<PdbDiagnostic>
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

        public override bool Equals(object? obj) => 
            obj is PdbDiagnostic other && Equals(other);

        public bool Equals(PdbDiagnostic other) =>
            Id == other.Id &&
            Token == other.Token &&
            SequenceComparer<object>.Instance.Equals(Args, other.Args);

        public override int GetHashCode() =>
            Hash.Combine((int)Id, Hash.Combine(Token, Hash.CombineValues(Args)));

        public override string ToString() =>
            ToString(CultureInfo.CurrentCulture);

        public string ToString(IFormatProvider formatProvider)
        {
            string location = (Token != 0) ? ": " + string.Format(formatProvider, ConverterResources.DiagnosticLocation, Token) : "";
            return $"PDB{(int)Id:D4}{location}: {GetMessage(formatProvider)}";
        }

        public string? GetMessage(IFormatProvider formatProvider)
        {
            if (formatProvider == null)
            {
                throw new ArgumentNullException(nameof(formatProvider));
            }

            var template = Id.GetMessageTemplate();
            if (template == null)
            {
                return null;
            }

            return (Args?.Length > 0) ? string.Format(formatProvider, template, Args) : template;
        }
    }
}
