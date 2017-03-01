// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.Debugging
{
    internal struct CustomDebugInfoEncoder
    {
        public BlobBuilder Builder { get; }

        private readonly Blob _recordCountFixup;
        private int _recordCount;

        public CustomDebugInfoEncoder(BlobBuilder builder)
        {
            Debug.Assert(builder.Count == 0);

            Builder = builder;
            _recordCount = 0;

            // header:
            builder.WriteByte(CustomDebugInfoConstants.Version);

            // reserve byte for record count:
            _recordCountFixup = builder.ReserveBytes(1);

            // alignment:
            builder.WriteInt16(0);
        }

        public int RecordCount => _recordCount;

        /// <exception cref="InvalidOperationException">More than <see cref="byte.MaxValue"/> records added.</exception>
        public byte[] ToArray()
        {
            if (_recordCount == 0)
            {
                return null;
            }

            Debug.Assert(_recordCount <= byte.MaxValue);
            new BlobWriter(_recordCountFixup).WriteByte((byte)_recordCount);
            return Builder.ToArray();
        }

        public void AddReferenceToIteratorClass(string iteratorClassName)
        {
            Debug.Assert(iteratorClassName != null);

            AddRecord(
                CustomDebugInfoKind.ForwardIterator,
                iteratorClassName,
                (name, builder) => 
                {
                    builder.WriteUTF16(name);
                    builder.WriteInt16(0);
                });
        }

        public void AddReferenceToPreviousMethodWithUsingInfo(MethodDefinitionHandle methodHandle)
        {
            AddRecord(
                CustomDebugInfoKind.ForwardInfo,
                methodHandle,
                (mh, builder) => 
                {
                    int token = MetadataTokens.GetToken(mh);
                    builder.WriteInt32(token);
                }
                );
        }

        public void AddReferenceToMethodWithModuleInfo(MethodDefinitionHandle methodHandle)
        {
            AddRecord(
                CustomDebugInfoKind.ForwardToModuleInfo,
                methodHandle,
                (mh, builder) => builder.WriteInt32(MetadataTokens.GetToken(mh)));
        }

        public void AddUsingInfo(IReadOnlyCollection<int> usingCounts)
        {
            Debug.Assert(usingCounts.Count <= ushort.MaxValue);
           
            // This originally wrote (uint)12, (ushort)1, (ushort)0 in the
            // case where usingCounts was empty, but I'm not sure why.
            if (usingCounts.Count == 0)
            {
                return;
            }

            AddRecord(
                CustomDebugInfoKind.UsingInfo,
                usingCounts,
                (uc, builder) =>
                {
                    builder.WriteUInt16((ushort)uc.Count);
                    foreach (int usingCount in uc)
                    {
                        Debug.Assert(usingCount <= ushort.MaxValue);
                        builder.WriteUInt16((ushort)usingCount);
                    }
                });
        }

        public void AddStateMachineLocalScopes(ImmutableArray<StateMachineHoistedLocalScope> scopes)
        {
            if (scopes.IsEmpty)
            {
                return;
            }

            AddRecord(
                CustomDebugInfoKind.StateMachineHoistedLocalScopes,
                scopes,
                (s, builder) =>
                {
                    builder.WriteInt32(s.Length);
                    foreach (var scope in s)
                    {
                        if (scope.IsDefault)
                        {
                            builder.WriteInt32(0);
                            builder.WriteInt32(0);
                        }
                        else
                        {
                            // Dev12 C# emits end-inclusive range
                            builder.WriteInt32(scope.StartOffset);
                            builder.WriteInt32(scope.EndOffset - 1);
                        }
                    }
                });
        }

        internal const int DynamicAttributeSize = 64;
        internal const int IdentifierSize = 64;

        public void AddDynamicLocals(IReadOnlyCollection<(string LocalName, byte[] Flags, int Count, int SlotIndex)> dynamicLocals)
        {
            Debug.Assert(dynamicLocals != null);

            AddRecord(
                CustomDebugInfoKind.DynamicLocals,
                dynamicLocals,
                (info, builder) =>
                {
                    builder.WriteInt32(dynamicLocals.Count);

                    foreach (var (name, flags, count, slotIndex) in dynamicLocals)
                    {
                        Debug.Assert(flags.Length == DynamicAttributeSize);
                        Debug.Assert(name.Length <= IdentifierSize);

                        builder.WriteBytes(flags);
                        builder.WriteInt32(count);
                        builder.WriteInt32(slotIndex);
                        builder.WriteUTF16(name);
                        builder.WriteBytes(0, sizeof(char) * (IdentifierSize - name.Length));
                    }
                });
        }

        public void AddRecord<T>(
            CustomDebugInfoKind kind,
            T debugInfo,
            Action<T, BlobBuilder> recordSerializer)
        {
            int startOffset = Builder.Count;
            Builder.WriteByte(CustomDebugInfoConstants.Version);
            Builder.WriteByte((byte)kind);
            Builder.WriteByte(0);

            // alignment size and length (will be patched)
            var alignmentSizeAndLengthWriter = new BlobWriter(Builder.ReserveBytes(sizeof(byte) + sizeof(uint)));

            recordSerializer(debugInfo, Builder);

            int length = Builder.Count - startOffset;
            int alignedLength = 4 * ((length + 3) / 4);
            byte alignmentSize = (byte)(alignedLength - length);
            Builder.WriteBytes(0, alignmentSize);

            // Fill in alignment size and length. 
            // For backward compat, alignment size should only be emitted for records introduced since Roslyn. 
            alignmentSizeAndLengthWriter.WriteByte((kind > CustomDebugInfoKind.DynamicLocals) ? alignmentSize : (byte)0);
            alignmentSizeAndLengthWriter.WriteUInt32((uint)alignedLength);

            _recordCount++;
        }
    }
}
