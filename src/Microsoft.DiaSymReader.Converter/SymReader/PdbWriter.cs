// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;

namespace Microsoft.DiaSymReader
{
    internal abstract class PdbWriter<TDocumentWriter>
    {
        public abstract TDocumentWriter DefineDocument(string name, Guid language, Guid vendor, Guid type, Guid algorithmId, byte[] checksum);
        public abstract void DefineSequencePoints(TDocumentWriter document, int count, int[] offsets, int[] startLines, int[] startColumns, int[] endLines, int[] endColumns);
        public abstract void OpenMethod(int methodToken);
        public abstract void CloseMethod();
        public abstract void OpenScope(int startOffset);
        public abstract void CloseScope(int endOffset);
        public abstract void DefineLocalVariable(int index, string name, LocalVariableAttributes attributes, int localSignatureToken);
        public abstract void DefineLocalConstant(string name, object value, int constantSignatureToken);
        public abstract void UsingNamespace(string importString);
        public abstract void SetAsyncInfo(int moveNextMethodToken, int kickoffMethodToken, int catchHandlerOffset, int[] yieldOffsets, int[] resumeOffsets);
        public abstract void DefineCustomMetadata(byte[] metadata);
        public abstract void SetEntryPoint(int entryMethodToken);
        public abstract void UpdateSignature(Guid guid, uint stamp, int age);
        public abstract void SetSourceServerData(byte[] data);
        public abstract void SetSourceLinkData(byte[] data);
    }
}
