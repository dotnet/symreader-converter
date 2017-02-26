using System;

namespace Microsoft.DiaSymReader
{
    /// <summary>
    /// Exception to enable callers to catch all of the exceptions originating
    /// from writing PDBs. We resurface such exceptions as this type, to eventually
    /// be reported as PDB-writing failure diagnostics to the user.
    /// Unfortunately, an exception originating in a user-implemented
    /// Stream derivation will come out of the symbol writer as a COMException
    /// missing all of the original exception info.
    /// </summary>
    internal sealed class PdbWritingException : Exception
    {
        internal PdbWritingException(Exception inner) :
            base(inner.Message, inner)
        {
        }
    }
}
