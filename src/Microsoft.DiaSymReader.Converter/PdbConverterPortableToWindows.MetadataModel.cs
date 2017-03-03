// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.DiaSymReader.Tools
{
    internal static partial class PdbConverterPortableToWindows<TDocumentWriter>
    {
        private sealed class MetadataModel
        {
            public MetadataReader Reader { get; }

			/// <summary>
            /// Maps standalone signature blobs to a handle. 
            /// </summary>
            private Lazy<Dictionary<byte[], StandaloneSignatureHandle>> _lazyStandaloneSignatureMap;

            public MetadataModel(MetadataReader reader)
            {
                Reader = reader;

                _lazyStandaloneSignatureMap = new Lazy<Dictionary<byte[], StandaloneSignatureHandle>>(BuildStandaloneSignatureMap);
            }

            private Dictionary<byte[], StandaloneSignatureHandle> BuildStandaloneSignatureMap()
            {
                int count = Reader.GetTableRowCount(TableIndex.StandAloneSig);

                var result = new Dictionary<byte[], StandaloneSignatureHandle>(count, ByteSequenceComparer.Instance);

                for (int rowId = 1; rowId < count; rowId++)
                {
                    var handle = MetadataTokens.StandaloneSignatureHandle(rowId);
                    var signature = Reader.GetStandaloneSignature(handle);
                    var bytes = Reader.GetBlobBytes(signature.Signature);
                    result[bytes] = handle;
                }

                return result;
            }

            public bool TryGetStandaloneSignatureHandle(byte[] signature, out StandaloneSignatureHandle handle) =>
                _lazyStandaloneSignatureMap.Value.TryGetValue(signature, out handle);

            internal string GetSerializedTypeName(EntityHandle targetType)
            {
                throw new NotImplementedException();
            }
        }
    }
}
