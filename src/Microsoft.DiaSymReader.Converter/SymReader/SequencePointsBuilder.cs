// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.DiaSymReader
{
    internal sealed class SequencePointsBuilder
    {
        private int _count;
        private int[] _offsets;
        private int[] _startLines;
        private int[] _startColumns;
        private int[] _endLines;
        private int[] _endColumns;

        public SequencePointsBuilder(int capacity)
        {
            _offsets = new int[capacity];
            _startLines = new int[capacity];
            _startColumns = new int[capacity];
            _endLines = new int[capacity];
            _endColumns = new int[capacity];
        }

        private void EnsureCapacity(int length)
        {
            if (length > _offsets.Length)
            {
                int newLength = Math.Max(length, (_offsets.Length + 1) * 2);

                Array.Resize(ref _offsets, newLength);
                Array.Resize(ref _startLines, newLength);
                Array.Resize(ref _startColumns, newLength);
                Array.Resize(ref _endLines, newLength);
                Array.Resize(ref _endColumns, newLength);
            }
        }

        public void Add(int offset, int startLine, int startColumn, int endLine, int endColumn)
        {
            int index = _count++;

            EnsureCapacity(_count);

            _offsets[index] = offset;
            _startLines[index] = startLine;
            _startColumns[index] = startColumn;
            _endLines[index] = endLine;
            _endColumns[index] = endColumn;
        }

        public void Clear()
        {
            _count = 0;
        }

        public void WriteSequencePoints<TDocumentWriter>(PdbWriter<TDocumentWriter> pdbWriter, TDocumentWriter symDocument)
        {
            if (_count == 0)
            {
                return;
            }

            pdbWriter.DefineSequencePoints(
                symDocument,
                _count,
                _offsets,
                _startLines,
                _startColumns,
                _endLines,
                _endColumns);

            Clear();
        }
    }
}
