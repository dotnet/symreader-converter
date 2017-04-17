// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.DiaSymReader;
using Roslyn.Utilities;
using PooledStringBuilder = Microsoft.CodeAnalysis.Collections.PooledStringBuilder;

namespace Microsoft.DiaSymReader.Tools
{
    /// <summary>
    /// Class to write out XML for a PDB.
    /// </summary>
    public sealed class PdbToXmlConverter
    {
        // For printing integers in a standard hex format.
        private const string IntHexFormat = "0x{0:X}";

        private readonly MetadataReader _metadataReader;
        private readonly ISymUnmanagedReader3 _symReader;
        private readonly MetadataReader _portablePdbMetadataOpt;
        private readonly PdbToXmlOptions _options;
        private readonly XmlWriter _writer;

        private static readonly XmlWriterSettings s_xmlWriterSettings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            NewLineChars = "\r\n",
        };

        private PdbToXmlConverter(XmlWriter writer, ISymUnmanagedReader3 symReader, MetadataReader metadataReader, PdbToXmlOptions options)
        {
            _symReader = symReader;
            _metadataReader = metadataReader;
            _writer = writer;
            _options = options;
            _portablePdbMetadataOpt = GetPortablePdbMetadata(symReader);
        }

        private unsafe static MetadataReader GetPortablePdbMetadata(ISymUnmanagedReader3 symReader)
        {
            if (symReader is ISymUnmanagedReader4 symReader4)
            {
                int hr = symReader4.GetPortableDebugMetadata(out byte* metadata, out int size);
                if (hr == HResult.S_OK)
                {
                    return new MetadataReader(metadata, size);
                }
            }

            return null;
        }

        public static string DeltaPdbToXml(Stream deltaPdb, IEnumerable<int> methodTokens)
        {
            var writer = new StringWriter();
            ToXml(
                writer,
                deltaPdb,
                metadataReaderOpt: null,
                options: PdbToXmlOptions.IncludeTokens,
                methodHandles: methodTokens.Select(token => (MethodDefinitionHandle)MetadataTokens.Handle(token)));

            return writer.ToString();
        }

        public static string ToXml(Stream pdbStream, Stream peStream, PdbToXmlOptions options = PdbToXmlOptions.ResolveTokens, string methodName = null)
        {
            var writer = new StringWriter();
            ToXml(writer, pdbStream, peStream, options, methodName);
            return writer.ToString();
        }

        public static string ToXml(Stream pdbStream, byte[] peImage, PdbToXmlOptions options = PdbToXmlOptions.ResolveTokens, string methodName = null)
        {
            var writer = new StringWriter();
            ToXml(writer, pdbStream, new MemoryStream(peImage), options, methodName);
            return writer.ToString();
        }

        public static void ToXml(TextWriter xmlWriter, Stream pdbStream, Stream peStream, PdbToXmlOptions options = PdbToXmlOptions.Default, string methodName = null)
        {
            IEnumerable<MethodDefinitionHandle> methodHandles;

            using (var peReader = new PEReader(peStream, PEStreamOptions.LeaveOpen))
            {
                var metadataReader = peReader.GetMetadataReader();

                if (string.IsNullOrEmpty(methodName))
                {
                    methodHandles = metadataReader.MethodDefinitions;
                }
                else
                {
                    var matching = metadataReader.MethodDefinitions.
                        Where(methodHandle => GetQualifiedMethodName(metadataReader, methodHandle) == methodName).ToArray();

                    if (matching.Length == 0)
                    {
                        xmlWriter.WriteLine("<error>");
                        xmlWriter.WriteLine($"<message><![CDATA[No method '{methodName}' found in metadata.]]></message>");
                        xmlWriter.WriteLine("<available-methods>");

                        foreach (var methodHandle in metadataReader.MethodDefinitions)
                        {
                            xmlWriter.Write("<method><![CDATA[");
                            xmlWriter.Write(GetQualifiedMethodName(metadataReader, methodHandle));
                            xmlWriter.Write("]]></method>");
                            xmlWriter.WriteLine();
                        }

                        xmlWriter.WriteLine("</available-methods>");
                        xmlWriter.WriteLine("</error>");

                        return;
                    }

                    methodHandles = matching;
                }

                ToXml(xmlWriter, pdbStream, metadataReader, options, methodHandles);
            }
        }

        /// <summary>
        /// Load the PDB given the parameters at the ctor and spew it out to the XmlWriter specified
        /// at the ctor.
        /// </summary>
        private static void ToXml(TextWriter xmlWriter, Stream pdbStream, MetadataReader metadataReaderOpt, PdbToXmlOptions options, IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            Debug.Assert(pdbStream != null);
            Debug.Assert((options & PdbToXmlOptions.ResolveTokens) == 0 || metadataReaderOpt != null);

            using (var writer = XmlWriter.Create(xmlWriter, s_xmlWriterSettings))
            {
                // metadata reader is on stack -> no owner needed
                var symReader = CreateReader(pdbStream, metadataReaderOpt, metadataMemoryOwnerOpt: null);

                try
                {
                    var converter = new PdbToXmlConverter(writer, symReader, metadataReaderOpt, options);
                    converter.WriteRoot(methodHandles ?? metadataReaderOpt.MethodDefinitions);
                }
                finally
                {
                    ((ISymUnmanagedDispose)symReader).Destroy();
                }
            }
        }

        private static ISymUnmanagedReader3 CreateReader(Stream pdbStream, MetadataReader metadataReaderOpt, IDisposable metadataMemoryOwnerOpt)
        {
            if (SymReaderFactory.IsPortable(pdbStream))
            {
                return (ISymUnmanagedReader3)new PortablePdb.SymBinder().GetReaderFromStream(pdbStream,
                    new Roslyn.Test.PdbUtilities.DummyMetadataImport(metadataReaderOpt, metadataMemoryOwnerOpt));
            }
            else
            {
                return SymReaderFactory.CreateWindowsPdbReader(pdbStream, metadataReaderOpt, metadataMemoryOwnerOpt);
            }
        }

        private void WriteRoot(IEnumerable<MethodDefinitionHandle> methodHandles)
        {
            _writer.WriteStartDocument();
            _writer.WriteStartElement("symbols");

            var documents = _symReader.GetDocuments();
            var documentIndex = BuildDocumentIndex(documents);
            var methodTokenMap = BuildMethodTokenMap();

            if ((_options & PdbToXmlOptions.ExcludeDocuments) == 0)
            {
                WriteDocuments(documents, documentIndex);
            }

            if ((_options & PdbToXmlOptions.ExcludeMethods) == 0)
            {
                WriteEntryPoint();
                WriteAllMethods(methodHandles, methodTokenMap, documentIndex);
                WriteAllMethodSpans();
            }

            if ((_options & PdbToXmlOptions.IncludeSourceServerInformation) != 0)
            {
                WriteSourceServerInformation();
            }

            _writer.WriteEndElement();
        }

        private void WriteAllMethods(IEnumerable<MethodDefinitionHandle> methodHandles, ImmutableArray<MethodDefinitionHandle> tokenMap, IReadOnlyDictionary<string, int> documentIndex)
        {
            _writer.WriteStartElement("methods");

            foreach (var methodHandle in methodHandles)
            {
                WriteMethod(methodHandle, tokenMap, documentIndex);
            }

            _writer.WriteEndElement();
        }

        private void WriteMethod(MethodDefinitionHandle methodHandle, ImmutableArray<MethodDefinitionHandle> tokenMap, IReadOnlyDictionary<string, int> documentIndex)
        {
            int token = _metadataReader.GetToken(methodHandle);
            ISymUnmanagedMethod method = _symReader.GetMethod(token);

            var windowsCdi = default(byte[]);
            var portableCdi = ImmutableArray<(Guid kind, ImmutableArray<byte> data)>.Empty;

            var sequencePoints = ImmutableArray<SymUnmanagedSequencePoint>.Empty;
            ISymUnmanagedAsyncMethod asyncMethod = null;
            ISymUnmanagedScope rootScope = null;

            if ((_options & PdbToXmlOptions.ExcludeCustomDebugInformation) == 0)
            {
                if (_portablePdbMetadataOpt != null)
                {
                    portableCdi = GetPortableCustomDebugInfo(methodHandle);
                }
                else
                {
                    windowsCdi = _symReader.GetCustomDebugInfo(token, methodVersion: 1);
                }
            }

            if (method != null)
            {
                if ((_options & PdbToXmlOptions.ExcludeAsyncInfo) == 0)
                {
                    asyncMethod = method.AsAsyncMethod();
                }

                if ((_options & PdbToXmlOptions.ExcludeSequencePoints) == 0)
                {
                    sequencePoints = method.GetSequencePoints().ToImmutableArray();
                }

                if ((_options & PdbToXmlOptions.ExcludeScopes) == 0)
                {
                    rootScope = method.GetRootScope();
                }
            }

            if (windowsCdi == null && portableCdi.IsEmpty && sequencePoints.IsEmpty && rootScope == null && asyncMethod == null)
            {
                // no debug info to write
                return;
            }

            _writer.WriteStartElement("method");

            int methodRowId = MetadataTokens.GetRowNumber(methodHandle);
            int tokenToDisplay = (methodRowId <= tokenMap.Length) ? MetadataTokens.GetToken(tokenMap[methodRowId - 1]) : token;

            WriteMethodAttributes(tokenToDisplay, isReference: false);
            WriteCustomDebugInfo(windowsCdi, portableCdi);

            if (!sequencePoints.IsEmpty)
            {
                WriteSequencePoints(sequencePoints, documentIndex);
            }

            if (rootScope != null)
            {
                WriteScopes(rootScope);
            }

            if (asyncMethod != null)
            {
                WriteAsyncInfo(asyncMethod);
            }

            _writer.WriteEndElement();
        }

        private void WriteCustomDebugInfo(byte[] windowsCdi, ImmutableArray<(Guid kind, ImmutableArray<byte> data)> portableCdi)
        {
            Debug.Assert(windowsCdi == null || portableCdi.IsEmpty);

            if (windowsCdi == null && portableCdi.IsEmpty)
            {
                return;
            }

            _writer.WriteStartElement("customDebugInfo");

            if (windowsCdi != null)
            {
                WriteCustomDebugInfo(windowsCdi);
            }
            else
            {
                WriteCustomDebugInfo(portableCdi);
            }

            _writer.WriteEndElement();
        }

        /// <summary>
        /// Given a byte array of custom debug info, parse the array and write out XML describing
        /// its structure and contents.
        /// </summary>
        private void WriteCustomDebugInfo(byte[] bytes)
        {
            var records = CustomDebugInfoReader.GetCustomDebugInfoRecords(bytes).ToArray();

            foreach (var record in records)
            {
                if (record.Version != CustomDebugInfoConstants.Version)
                {
                    WriteUnknownCustomDebugInfo(record);
                }
                else
                {
                    var data = record.Data;
                    switch (record.Kind)
                    {
                        case CustomDebugInfoKind.UsingGroups:
                            WriteUsingGroupsCustomDebugInfo(data);
                            break;
                        case CustomDebugInfoKind.ForwardMethodInfo:
                            WriteForwardMethodInfoCustomDebugInfo(data);
                            break;
                        case CustomDebugInfoKind.ForwardModuleInfo:
                            WriteForwardModuleInfoCustomDebugInfo(data);
                            break;
                        case CustomDebugInfoKind.DynamicLocals:
                            WriteDynamicLocalsCustomDebugInfo(data);
                            break;
                        case CustomDebugInfoKind.StateMachineTypeName:
                            WriteStateMachineTypeNameCustomDebugInfo(data);
                            break;
                        case CustomDebugInfoKind.TupleElementNames:
                            WriteTupleElementNamesCustomDebugInfo(data);
                            break;
                        case CustomDebugInfoKind.StateMachineHoistedLocalScopes:
                            WriteStateMachineHoistedLocalScopesCustomDebugInfo(data, isPortable: false);
                            break;
                        case CustomDebugInfoKind.EditAndContinueLocalSlotMap:
                            WriteEditAndContinueLocalSlotMap(data);
                            break;
                        case CustomDebugInfoKind.EditAndContinueLambdaMap:
                            WriteEditAndContinueLambdaAndClosureMap(data);
                            break;
                        default:
                            WriteUnknownCustomDebugInfo(record);
                            break;
                    }
                }
            }
        }

        // Order in Windows PDBs and Portable PDBs is different, but it doesn't matter. 
        // To enable matching between XML of both forats use the Windows PDB order for both.
        private static IReadOnlyDictionary<Guid, int> s_cdiOrdering = new Dictionary<Guid, int>()
        {
            {PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes, 0},
            {PortableCustomDebugInfoKinds.EncLocalSlotMap, 1},
            {PortableCustomDebugInfoKinds.EncLambdaAndClosureMap, 2},
        };

        private void WriteCustomDebugInfo(ImmutableArray<(Guid kind, ImmutableArray<byte> data)> cdis)
        {
            var mdReader = _portablePdbMetadataOpt;
            Debug.Assert(mdReader != null);

            foreach (var (kind, data) in cdis)
            {
                if (kind == PortableCustomDebugInfoKinds.StateMachineHoistedLocalScopes)
                {
                    WriteStateMachineHoistedLocalScopesCustomDebugInfo(data, isPortable: true);
                }
                else if (kind == PortableCustomDebugInfoKinds.EncLambdaAndClosureMap)
                {
                    WriteEditAndContinueLambdaAndClosureMap(data);
                }
                else if (kind == PortableCustomDebugInfoKinds.EncLocalSlotMap)
                {
                    WriteEditAndContinueLocalSlotMap(data);
                }
            }
        }

        private ImmutableArray<(Guid kind, ImmutableArray<byte> data)> GetPortableCustomDebugInfo(MethodDefinitionHandle handle)
        {
            var mdReader = _portablePdbMetadataOpt;
            Debug.Assert(mdReader != null);

            var cdiHandles = mdReader.GetCustomDebugInformation(handle);
            if (cdiHandles.Count == 0)
            {
                return ImmutableArray<(Guid, ImmutableArray<byte>)>.Empty;
            }

            var builder = new List<(int ordinal, Guid kind, ImmutableArray<byte> data)>();

            foreach (var cdiHandle in cdiHandles)
            {
                var cdi = mdReader.GetCustomDebugInformation(cdiHandle);
                var kind = mdReader.GetGuid(cdi.Kind);

                if (s_cdiOrdering.TryGetValue(kind, out int ordinal))
                {
                    builder.Add((ordinal, kind, mdReader.GetBlobContent(cdi.Value)));
                }
            }

            return builder.OrderBy(e => e.ordinal).Select(e => (e.kind, e.data)).ToImmutableArray();
        }

        /// <summary>
        /// If the custom debug info is in a format that we don't understand, then we will
        /// just print a standard record header followed by the rest of the record as a
        /// single hex string.
        /// </summary>
        private void WriteUnknownCustomDebugInfo(CustomDebugInfoRecord record)
        {
            _writer.WriteStartElement("unknown");
            _writer.WriteAttributeString("kind", record.Kind.ToString());
            _writer.WriteAttributeString("version", CultureInvariantToString(record.Version));
            _writer.WriteAttributeString("payload", BitConverter.ToString(record.Data.ToArray()));
            _writer.WriteEndElement();
        }

        /// <summary>
        /// For each namespace declaration enclosing a method (innermost-to-outermost), there is a count
        /// of the number of imports in that declaration.
        /// </summary>
        /// <remarks>
        /// There's always at least one entry (for the global namespace).
        /// </remarks>
        private void WriteUsingGroupsCustomDebugInfo(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("using");

            ImmutableArray<short> counts = CustomDebugInfoReader.DecodeUsingRecord(data);

            foreach (short importCount in counts)
            {
                _writer.WriteStartElement("namespace");
                _writer.WriteAttributeString("usingCount", CultureInvariantToString(importCount));
                _writer.WriteEndElement(); //namespace
            }

            _writer.WriteEndElement(); //using
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Emitting tokens makes tests more fragile.
        /// </remarks>
        private void WriteForwardMethodInfoCustomDebugInfo(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("forward");

            int token = CustomDebugInfoReader.DecodeForwardRecord(data);
            WriteMethodAttributes(token, isReference: true);

            _writer.WriteEndElement(); //forward
        }

        /// <summary>
        /// This indicates that further information can be obtained by looking at the custom debug
        /// info of another method (specified by token).
        /// </summary>
        /// <remarks>
        /// Appears when there are extern aliases and edit-and-continue is disabled.
        /// Emitting tokens makes tests more fragile.
        /// </remarks>
        private void WriteForwardModuleInfoCustomDebugInfo(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("forwardToModule");

            int token = CustomDebugInfoReader.DecodeForwardRecord(data);
            WriteMethodAttributes(token, isReference: true);

            _writer.WriteEndElement(); //forwardToModule
        }

        private void WriteStateMachineHoistedLocalScopesCustomDebugInfo(ImmutableArray<byte> data, bool isPortable)
        {
            _writer.WriteStartElement("hoistedLocalScopes");

            var scopes = isPortable ? 
                DecodePortableHoistedLocalScopes(data) :
                CustomDebugInfoReader.DecodeStateMachineHoistedLocalScopesRecord(data);

            foreach (StateMachineHoistedLocalScope scope in scopes)
            {
                _writer.WriteStartElement("slot");

                if (!scope.IsDefault)
                {
                    _writer.WriteAttributeString("startOffset", AsILOffset(scope.StartOffset));
                    _writer.WriteAttributeString("endOffset", AsILOffset(scope.EndOffset));
                }

                _writer.WriteEndElement(); //slot
            }

            _writer.WriteEndElement();
        }

        // TODO: copied from EE, unify

        /// <exception cref="BadImageFormatException">Invalid data format.</exception>
        private unsafe static ImmutableArray<StateMachineHoistedLocalScope> DecodePortableHoistedLocalScopes(ImmutableArray<byte> data)
        {
            if (data.Length == 0)
            {
                return ImmutableArray<StateMachineHoistedLocalScope>.Empty;
            }

            fixed (byte* buffer = data.ToArray())
            {
                var reader = new BlobReader(buffer, data.Length);
                var result = ImmutableArray.CreateBuilder<StateMachineHoistedLocalScope>();

                do
                {
                    int startOffset = reader.ReadInt32();
                    int length = reader.ReadInt32();

                    result.Add(new StateMachineHoistedLocalScope(startOffset, startOffset + length));
                }
                while (reader.RemainingBytes > 0);

                return result.ToImmutable();
            }
        }

        /// <summary>
        /// Contains a name string.
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when are iterator methods.
        /// </remarks>
        private void WriteStateMachineTypeNameCustomDebugInfo(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("forwardIterator");

            string name = CustomDebugInfoReader.DecodeForwardIteratorRecord(data);

            _writer.WriteAttributeString("name", name);

            _writer.WriteEndElement(); //forwardIterator
        }

        /// <summary>
        /// Contains a list of buckets, each of which contains a number of flags, a slot ID, and a name.
        /// TODO: comment when the structure is understood.
        /// </summary>
        /// <remarks>
        /// Appears when there are dynamic locals.
        /// </remarks>
        private void WriteDynamicLocalsCustomDebugInfo(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("dynamicLocals");

            var dynamicLocals = CustomDebugInfoReader.DecodeDynamicLocalsRecord(data);

            foreach (DynamicLocalInfo dynamicLocal in dynamicLocals)
            {
                var flags = dynamicLocal.Flags;

                var pooled = PooledStringBuilder.GetInstance();
                var flagsBuilder = pooled.Builder;
                foreach (bool flag in flags)
                {
                    flagsBuilder.Append(flag ?  '1' : '0');
                }

                _writer.WriteStartElement("bucket");
                _writer.WriteAttributeString("flags", pooled.ToStringAndFree());
                _writer.WriteAttributeString("slotId", CultureInvariantToString(dynamicLocal.SlotId));
                _writer.WriteAttributeString("localName", dynamicLocal.LocalName);
                _writer.WriteEndElement(); //bucket
            }

            _writer.WriteEndElement(); //dynamicLocals
        }

        private void WriteTupleElementNamesCustomDebugInfo(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("tupleElementNames");

            var tuples = CustomDebugInfoReader.DecodeTupleElementNamesRecord(data);

            foreach (var tuple in tuples)
            {
                _writer.WriteStartElement("local");
                _writer.WriteAttributeString("elementNames", JoinNames(tuple.ElementNames));
                _writer.WriteAttributeString("slotIndex", CultureInvariantToString(tuple.SlotIndex));
                _writer.WriteAttributeString("localName", tuple.LocalName);
                _writer.WriteAttributeString("scopeStart", AsILOffset(tuple.ScopeStart));
                _writer.WriteAttributeString("scopeEnd", AsILOffset(tuple.ScopeEnd));
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private static string JoinNames(ImmutableArray<string> names)
        {
            var pooledBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledBuilder.Builder;
            foreach (var name in names)
            {
                builder.Append('|');
                if (name != null)
                {
                    builder.Append(name);
                }
            }
            return pooledBuilder.ToStringAndFree();
        }

        private unsafe void WriteEditAndContinueLocalSlotMap(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("encLocalSlotMap");
            try
            {
                if (data.Length == 0)
                {
                    return;
                }

                int syntaxOffsetBaseline = -1;

                fixed (byte* compressedSlotMapPtr = data.ToArray())
                {
                    var blobReader = new BlobReader(compressedSlotMapPtr, data.Length);

                    while (blobReader.RemainingBytes > 0)
                    {
                        byte b = blobReader.ReadByte();

                        if (b == 0xff)
                        {
                            if (!blobReader.TryReadCompressedInteger(out syntaxOffsetBaseline))
                            {
                                _writer.WriteElementString("baseline", "?");
                                return;
                            }

                            syntaxOffsetBaseline = -syntaxOffsetBaseline;
                            continue;
                        }

                        _writer.WriteStartElement("slot");

                        if (b == 0)
                        {
                            // short-lived temp, no info
                            _writer.WriteAttributeString("kind", "temp");
                        }
                        else
                        {
                            int synthesizedKind = (b & 0x3f) - 1;
                            bool hasOrdinal = (b & (1 << 7)) != 0;

                            int syntaxOffset;
                            bool badSyntaxOffset = !blobReader.TryReadCompressedInteger(out syntaxOffset);
                            syntaxOffset += syntaxOffsetBaseline;

                            int ordinal = 0;
                            bool badOrdinal = hasOrdinal && !blobReader.TryReadCompressedInteger(out ordinal);

                            _writer.WriteAttributeString("kind", CultureInvariantToString(synthesizedKind));
                            _writer.WriteAttributeString("offset", badSyntaxOffset ? "?" : CultureInvariantToString(syntaxOffset));

                            if (badOrdinal || hasOrdinal)
                            {
                                _writer.WriteAttributeString("ordinal", badOrdinal ? "?" : CultureInvariantToString(ordinal));
                            }
                        }

                        _writer.WriteEndElement();
                    }
                }
            }
            finally
            {
                _writer.WriteEndElement(); //encLocalSlotMap
            }
        }

        private unsafe void WriteEditAndContinueLambdaAndClosureMap(ImmutableArray<byte> data)
        {
            _writer.WriteStartElement("encLambdaMap");
            try
            {
                if (data.Length == 0)
                {
                    return;
                }

                int methodOrdinal = -1;
                int syntaxOffsetBaseline = -1;
                int closureCount;

                fixed (byte* blobPtr = data.ToArray())
                {
                    var blobReader = new BlobReader(blobPtr, data.Length);

                    if (!blobReader.TryReadCompressedInteger(out methodOrdinal))
                    {
                        _writer.WriteElementString("methodOrdinal", "?");
                        _writer.WriteEndElement();
                        return;
                    }

                    // [-1, inf)
                    methodOrdinal--;
                    _writer.WriteElementString("methodOrdinal", CultureInvariantToString(methodOrdinal));

                    if (!blobReader.TryReadCompressedInteger(out syntaxOffsetBaseline))
                    {
                        _writer.WriteElementString("baseline", "?");
                        _writer.WriteEndElement();
                        return;
                    }

                    syntaxOffsetBaseline = -syntaxOffsetBaseline;
                    if (!blobReader.TryReadCompressedInteger(out closureCount))
                    {
                        _writer.WriteElementString("closureCount", "?");
                        _writer.WriteEndElement();
                        return;
                    }

                    for (int i = 0; i < closureCount; i++)
                    {
                        _writer.WriteStartElement("closure");
                        try
                        {
                            int syntaxOffset;
                            if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                            {
                                _writer.WriteElementString("offset", "?");
                                break;
                            }

                            _writer.WriteAttributeString("offset", CultureInvariantToString(syntaxOffset + syntaxOffsetBaseline));
                        }
                        finally
                        {
                            _writer.WriteEndElement();
                        }
                    }

                    while (blobReader.RemainingBytes > 0)
                    {
                        _writer.WriteStartElement("lambda");
                        try
                        {
                            int syntaxOffset;
                            if (!blobReader.TryReadCompressedInteger(out syntaxOffset))
                            {
                                _writer.WriteElementString("offset", "?");
                                return;
                            }

                            _writer.WriteAttributeString("offset", CultureInvariantToString(syntaxOffset + syntaxOffsetBaseline));

                            int closureOrdinal;
                            if (!blobReader.TryReadCompressedInteger(out closureOrdinal))
                            {
                                _writer.WriteElementString("closure", "?");
                                return;
                            }

                            closureOrdinal -= 2;

                            if (closureOrdinal == -2)
                            {
                                _writer.WriteAttributeString("closure", "this");
                            }
                            else if (closureOrdinal != -1)
                            {
                                _writer.WriteAttributeString("closure",
                                    CultureInvariantToString(closureOrdinal) + (closureOrdinal >= closureCount ? " (invalid)" : ""));
                            }
                        }
                        finally
                        {
                            _writer.WriteEndElement();
                        }
                    }
                }
            }
            finally
            {
                _writer.WriteEndElement(); //encLocalSlotMap
            }
        }

        private void WriteScopes(ISymUnmanagedScope rootScope)
        {
            // The root scope is always empty. The first scope opened by SymWriter is the child of the root scope.
            if (rootScope.GetNamespaces().Length == 0 && rootScope.GetLocals().Length == 0 && rootScope.GetConstants().Length == 0)
            {
                foreach (ISymUnmanagedScope child in rootScope.GetChildren())
                {
                    WriteScope(child, isRoot: false);
                }
            }
            else
            {
                // This shouldn't be executed for PDBs generated via SymWriter.
                WriteScope(rootScope, isRoot: true);
            }
        }

        private void WriteScope(ISymUnmanagedScope scope, bool isRoot)
        {
            _writer.WriteStartElement(isRoot ? "rootScope" : "scope");
            _writer.WriteAttributeString("startOffset", AsILOffset(scope.GetStartOffset()));
            _writer.WriteAttributeString("endOffset", AsILOffset(scope.GetEndOffset()));

            if ((_options & PdbToXmlOptions.ExcludeNamespaces) == 0)
            {
                foreach (ISymUnmanagedNamespace @namespace in scope.GetNamespaces())
                {
                    WriteNamespace(@namespace);
                }
            }

            WriteLocals(scope);

            foreach (ISymUnmanagedScope child in scope.GetChildren())
            {
                WriteScope(child, isRoot: false);
            }

            _writer.WriteEndElement();
        }

        private void WriteNamespace(ISymUnmanagedNamespace @namespace)
        {
            string rawName = @namespace.GetName();

            string alias;
            string externAlias;
            string target;
            ImportTargetKind kind;
            VBImportScopeKind scope;

            try
            {
                if (rawName.Length == 0)
                {
                    externAlias = null;
                    var parsingSucceeded = CustomDebugInfoReader.TryParseVisualBasicImportString(rawName, out alias, out target, out kind, out scope);
                    Debug.Assert(parsingSucceeded);
                }
                else
                {
                    switch (rawName[0])
                    {
                        case 'U':
                        case 'A':
                        case 'X':
                        case 'Z':
                        case 'E':
                        case 'T':
                            scope = VBImportScopeKind.Unspecified;
                            if (!CustomDebugInfoReader.TryParseCSharpImportString(rawName, out alias, out externAlias, out target, out kind))
                            {
                                throw new InvalidOperationException($"Invalid import '{rawName}'");
                            }
                            break;

                        default:
                            externAlias = null;
                            if (!CustomDebugInfoReader.TryParseVisualBasicImportString(rawName, out alias, out target, out kind, out scope))
                            {
                                throw new InvalidOperationException($"Invalid import '{rawName}'");
                            }
                            break;
                    }
                }
            }
            catch (ArgumentException) when ((_options & PdbToXmlOptions.ThrowOnError) == 0)
            {
                _writer.WriteStartElement("invalid-custom-data");
                _writer.WriteAttributeString("raw", rawName);
                _writer.WriteEndElement();
                return;
            }

            switch (kind)
            {
                case ImportTargetKind.CurrentNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == VBImportScopeKind.Unspecified);

                    _writer.WriteStartElement("currentnamespace");
                    _writer.WriteAttributeString("name", target);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.DefaultNamespace:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == VBImportScopeKind.Unspecified);

                    _writer.WriteStartElement("defaultnamespace");
                    _writer.WriteAttributeString("name", target);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.MethodToken:
                    Debug.Assert(alias == null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == VBImportScopeKind.Unspecified);

                    int token = Convert.ToInt32(target);
                    _writer.WriteStartElement("importsforward");
                    WriteMethodAttributes(token, isReference: true);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.XmlNamespace:
                    Debug.Assert(externAlias == null);

                    _writer.WriteStartElement("xmlnamespace");
                    _writer.WriteAttributeString("prefix", alias);
                    _writer.WriteAttributeString("name", target);
                    WriteScopeAttribute(scope);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.NamespaceOrType:
                    Debug.Assert(externAlias == null);

                    _writer.WriteStartElement("alias");
                    _writer.WriteAttributeString("name", alias);
                    _writer.WriteAttributeString("target", target);
                    _writer.WriteAttributeString("kind", "namespace"); // Strange, but retaining to avoid breaking tests.
                    WriteScopeAttribute(scope);
                    _writer.WriteEndElement();
                    break;

                case ImportTargetKind.Namespace:
                    if (alias != null)
                    {
                        _writer.WriteStartElement("alias");
                        _writer.WriteAttributeString("name", alias);
                        if (externAlias != null)
                        {
                            _writer.WriteAttributeString("qualifier", externAlias);
                        }

                        _writer.WriteAttributeString("target", target);
                        _writer.WriteAttributeString("kind", "namespace");
                        Debug.Assert(scope == VBImportScopeKind.Unspecified); // Only C# hits this case.
                        _writer.WriteEndElement();
                    }
                    else
                    {
                        _writer.WriteStartElement("namespace");
                        if (externAlias != null) _writer.WriteAttributeString("qualifier", externAlias);
                        _writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        _writer.WriteEndElement();
                    }

                    break;

                case ImportTargetKind.Type:
                    Debug.Assert(externAlias == null);
                    if (alias != null)
                    {
                        _writer.WriteStartElement("alias");
                        _writer.WriteAttributeString("name", alias);
                        _writer.WriteAttributeString("target", target);
                        _writer.WriteAttributeString("kind", "type");
                        Debug.Assert(scope == VBImportScopeKind.Unspecified); // Only C# hits this case.
                        _writer.WriteEndElement();
                    }
                    else
                    {
                        _writer.WriteStartElement("type");
                        _writer.WriteAttributeString("name", target);
                        WriteScopeAttribute(scope);
                        _writer.WriteEndElement();
                    }

                    break;

                case ImportTargetKind.Assembly:
                    Debug.Assert(alias != null);
                    Debug.Assert(externAlias == null);
                    Debug.Assert(scope == VBImportScopeKind.Unspecified);
                    if (target == null)
                    {
                        _writer.WriteStartElement("extern");
                        _writer.WriteAttributeString("alias", alias);
                        _writer.WriteEndElement();
                    }
                    else
                    {
                        _writer.WriteStartElement("externinfo");
                        _writer.WriteAttributeString("alias", alias);
                        _writer.WriteAttributeString("assembly", target);
                        _writer.WriteEndElement();
                    }

                    break;

                case ImportTargetKind.Defunct:
                    Debug.Assert(alias == null);
                    Debug.Assert(scope == VBImportScopeKind.Unspecified);
                    _writer.WriteStartElement("defunct");
                    _writer.WriteAttributeString("name", rawName);
                    _writer.WriteEndElement();
                    break;

                default:
                    Debug.Assert(false, "Unexpected import kind '" + kind + "'");
                    _writer.WriteStartElement("unknown");
                    _writer.WriteAttributeString("name", rawName);
                    _writer.WriteEndElement();
                    break;
            }
        }

        private void WriteScopeAttribute(VBImportScopeKind scope)
        {
            if (scope == VBImportScopeKind.File)
            {
                _writer.WriteAttributeString("importlevel", "file");
            }
            else if (scope == VBImportScopeKind.Project)
            {
                _writer.WriteAttributeString("importlevel", "project");
            }
            else
            {
                Debug.Assert(scope == VBImportScopeKind.Unspecified, "Unexpected scope '" + scope + "'");
            }
        }

        private void WriteAsyncInfo(ISymUnmanagedAsyncMethod asyncMethod)
        {
            _writer.WriteStartElement("asyncInfo");

            var catchOffset = asyncMethod.GetCatchHandlerILOffset();
            if (catchOffset >= 0)
            {
                _writer.WriteStartElement("catchHandler");
                _writer.WriteAttributeString("offset", AsILOffset(catchOffset));
                _writer.WriteEndElement();
            }

            _writer.WriteStartElement("kickoffMethod");
            WriteMethodAttributes(asyncMethod.GetKickoffMethod(), isReference: true);
            _writer.WriteEndElement();

            foreach (var info in asyncMethod.GetAsyncStepInfos())
            {
                _writer.WriteStartElement("await");
                _writer.WriteAttributeString("yield", AsILOffset(info.YieldOffset));
                _writer.WriteAttributeString("resume", AsILOffset(info.ResumeOffset));
                WriteMethodAttributes(info.ResumeMethod, isReference: true);
                _writer.WriteEndElement();
            }

            _writer.WriteEndElement();
        }

        private void WriteLocals(ISymUnmanagedScope scope)
        {
            foreach (ISymUnmanagedVariable l in scope.GetLocals())
            {
                _writer.WriteStartElement("local");
                _writer.WriteAttributeString("name", l.GetName());

                // NOTE: VB emits "fake" locals for resumable locals which are actually backed by fields.
                //       These locals always map to the slot #0 which is just a valid number that is 
                //       not used. Only scoping information is used by EE in this case.
                _writer.WriteAttributeString("il_index", CultureInvariantToString(l.GetSlot()));

                _writer.WriteAttributeString("il_start", AsILOffset(scope.GetStartOffset()));
                _writer.WriteAttributeString("il_end", AsILOffset(scope.GetEndOffset()));
                _writer.WriteAttributeString("attributes", CultureInvariantToString(l.GetAttributes()));
                _writer.WriteEndElement();
            }

            foreach (ISymUnmanagedConstant constant in scope.GetConstants())
            {
                string name = constant.GetName();

                // Constant signatures might be missing in Windows PDB that were produced by a conversion from Portable PDBs.
                // Check for HResult explicitly to avoid exceptions.
                int hr = constant.GetSignature(0, out _, null);
                byte[] signature = (hr == 0) ? constant.GetSignature() : Array.Empty<byte>();

                object value = constant.GetValue();

                _writer.WriteStartElement("constant");
                _writer.WriteAttributeString("name", name);

                if (value is 0 && IsPossiblyNullConstantType(signature))
                {
                    _writer.WriteAttributeString("value", "null");

                    if (signature.Length == 0)
                    {
                        _writer.WriteAttributeString("unknown-signature", "");
                    }
                    else if (signature[0] == (int)SignatureTypeCode.String)
                    {
                        _writer.WriteAttributeString("type", "String");
                    }
                    else if (signature[0] == (int)SignatureTypeCode.Object)
                    {
                        _writer.WriteAttributeString("type", "Object");
                    }
                    else
                    {
                        _writer.WriteAttributeString("signature", FormatLocalConstantSignature(signature));
                    }
                }
                else if (value == null)
                {
                    // empty string
                    if (signature.Length > 0 && signature[0] == (byte)SignatureTypeCode.String)
                    {
                        _writer.WriteAttributeString("value", "");
                        _writer.WriteAttributeString("type", "String");
                    }
                    else
                    {
                        _writer.WriteAttributeString("value", "null");
                        _writer.WriteAttributeString("unknown-signature", BitConverter.ToString(signature.ToArray()));
                    }
                }
                else if (value is decimal)
                {
                    // TODO: check that the signature is a TypeRef
                    _writer.WriteAttributeString("value", ((decimal)value).ToString(CultureInfo.InvariantCulture));
                    _writer.WriteAttributeString("type", value.GetType().Name);
                }
                else if (value is double && signature.Length > 0 && signature[0] != (byte)SignatureTypeCode.Double)
                {
                    // TODO: check that the signature is a TypeRef
                    _writer.WriteAttributeString("value", DateTimeUtilities.ToDateTime((double)value).ToString(CultureInfo.InvariantCulture));
                    _writer.WriteAttributeString("type", "DateTime");
                }
                else
                {
                    var str = value as string;
                    if (str != null)
                    {
                        _writer.WriteAttributeString("value", StringUtilities.EscapeNonPrintableCharacters(str));
                    }
                    else
                    {
                        _writer.WriteAttributeString("value", string.Format(CultureInfo.InvariantCulture, "{0}", value));
                    }

                    if (signature.Length == 0)
                    {
                        _writer.WriteAttributeString("runtime-type", value.GetType().Name);
                        _writer.WriteAttributeString("unknown-signature", BitConverter.ToString(signature.ToArray()));
                    }
                    else
                    {
                        var runtimeType = GetConstantRuntimeType(signature);
                        if (runtimeType == null &&
                            (value is sbyte || value is byte || value is short || value is ushort ||
                             value is int || value is uint || value is long || value is ulong))
                        {
                            _writer.WriteAttributeString("signature", FormatLocalConstantSignature(signature));
                        }
                        else if (runtimeType == value.GetType())
                        {
                            _writer.WriteAttributeString("type", ((SignatureTypeCode)signature[0]).ToString());
                        }
                        else
                        {
                            _writer.WriteAttributeString("runtime-type", value.GetType().Name);
                            _writer.WriteAttributeString("unknown-signature", BitConverter.ToString(signature.ToArray()));
                        }
                    }
                }

                _writer.WriteEndElement();
            }
        }

        private static bool IsPossiblyNullConstantType(byte[] signature)
        {
            if (signature.Length == 0)
            {
                return true;
            }   

            switch ((SignatureTypeCode)signature[0])
            {
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.Char:
                case SignatureTypeCode.Int16:
                case SignatureTypeCode.UInt16:
                case SignatureTypeCode.Int32:
                case SignatureTypeCode.UInt32:
                case SignatureTypeCode.Int64:
                case SignatureTypeCode.UInt64:
                case SignatureTypeCode.IntPtr:
                case SignatureTypeCode.UIntPtr:
                case SignatureTypeCode.Single:
                case SignatureTypeCode.Double:
                    return false;

                case SignatureTypeCode.GenericTypeInstance:
                    if (signature.Length == 1)
                    {
                        // bad signature
                        return true;
                    }

                    // don't care about projections changing value type to class type here
                    return (SignatureTypeKind)signature[1] != SignatureTypeKind.ValueType;

                case (SignatureTypeCode)SignatureTypeKind.ValueType: 
                    // don't care about projections changing value type to class type here
                    return false;

                default:
                    return true;
            }
        }

        private unsafe string FormatLocalConstantSignature(byte[] signature)
        {
            fixed (byte* sigPtr = signature.ToArray())
            {
                var sigReader = new BlobReader(sigPtr, signature.Length);
                var decoder = new SignatureDecoder<string, object>(ConstantSignatureVisualizer.Instance, _metadataReader, genericContext: null);
                return decoder.DecodeType(ref sigReader, allowTypeSpecifications: true);
            }
        }

        private sealed class ConstantSignatureVisualizer : ISignatureTypeProvider<string, object>
        {
            public static readonly ConstantSignatureVisualizer Instance = new ConstantSignatureVisualizer();

            public string GetArrayType(string elementType, ArrayShape shape)
            {
                return elementType + "[" + new string(',', shape.Rank) + "]";
            }

            public string GetByReferenceType(string elementType)
            {
                return elementType + "&";
            }

            public string GetFunctionPointerType(MethodSignature<string> signature)
            {
                // TODO:
                return "method-ptr";
            }

            public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
            {
                // using {} since the result is embedded in XML
                return genericType + "{" + string.Join(", ", typeArguments) + "}";
            }

            public string GetGenericMethodParameter(object genericContext, int index)
            {
                return "!!" + index;
            }

            public string GetGenericTypeParameter(object genericContext, int index)
            {
                return "!" + index;
            }

            public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
            {
                return (isRequired ? "modreq" : "modopt") + "(" + modifier + ") " + unmodifiedType;
            }

            public string GetPinnedType(string elementType)
            {
                return "pinned " + elementType;
            }

            public string GetPointerType(string elementType)
            {
                return elementType + "*";
            }

            public string GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                return typeCode.ToString();
            }

            public string GetSZArrayType(string elementType)
            {
                return elementType + "[]";
            }

            public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            {
                var typeDef = reader.GetTypeDefinition(handle);
                var name = reader.GetString(typeDef.Name);
                return typeDef.Namespace.IsNil ? name : reader.GetString(typeDef.Namespace) + "." + name;
            }

            public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var typeRef = reader.GetTypeReference(handle);
                var name = reader.GetString(typeRef.Name);
                return typeRef.Namespace.IsNil ? name : reader.GetString(typeRef.Namespace) + "." + name;
            }

            public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                var sigReader = reader.GetBlobReader(reader.GetTypeSpecification(handle).Signature);
                return new SignatureDecoder<string, object>(Instance, reader, genericContext).DecodeType(ref sigReader);
            }
        }

        private static Type GetConstantRuntimeType(byte[] signature)
        {
            switch ((SignatureTypeCode)signature[0])
            {
                case SignatureTypeCode.Boolean:
                case SignatureTypeCode.Byte:
                case SignatureTypeCode.SByte:
                case SignatureTypeCode.Int16:
                    return typeof(short);

                case SignatureTypeCode.Char:
                case SignatureTypeCode.UInt16:
                    return typeof(ushort);

                case SignatureTypeCode.Int32:
                    return typeof(int);

                case SignatureTypeCode.UInt32:
                    return typeof(uint);

                case SignatureTypeCode.Int64:
                    return typeof(long);

                case SignatureTypeCode.UInt64:
                    return typeof(ulong);

                case SignatureTypeCode.Single:
                    return typeof(float);

                case SignatureTypeCode.Double:
                    return typeof(double);

                case SignatureTypeCode.String:
                    return typeof(string);
            }

            return null;
        }

        private void WriteSequencePoints(ImmutableArray<SymUnmanagedSequencePoint> sequencePoints, IReadOnlyDictionary<string, int> documentIndex)
        {
            Debug.Assert(!sequencePoints.IsDefaultOrEmpty);

            _writer.WriteStartElement("sequencePoints");

            // Write out sequence points
            foreach (var sequencePoint in sequencePoints)
            {
                _writer.WriteStartElement("entry");
                _writer.WriteAttributeString("offset", AsILOffset(sequencePoint.Offset));

                if (sequencePoint.IsHidden)
                {
                    if (sequencePoint.StartLine != sequencePoint.EndLine || sequencePoint.StartColumn != 0 || sequencePoint.EndColumn != 0)
                    {
                        _writer.WriteAttributeString("hidden", "invalid");
                    }
                    else
                    {
                        _writer.WriteAttributeString("hidden", XmlConvert.ToString(true));
                    }
                }
                else
                {
                    _writer.WriteAttributeString("startLine", CultureInvariantToString(sequencePoint.StartLine));
                    _writer.WriteAttributeString("startColumn", CultureInvariantToString(sequencePoint.StartColumn));
                    _writer.WriteAttributeString("endLine", CultureInvariantToString(sequencePoint.EndLine));
                    _writer.WriteAttributeString("endColumn", CultureInvariantToString(sequencePoint.EndColumn));
                }

                int documentId;
                string documentName = sequencePoint.Document.GetName();
                if (documentName.Length > 0)
                {
                    if (documentIndex.TryGetValue(documentName, out documentId))
                    {
                        _writer.WriteAttributeString("document", CultureInvariantToString(documentId));
                    }
                    else
                    {
                        _writer.WriteAttributeString("document", "?");
                    }
                }

                _writer.WriteEndElement();
            }

            _writer.WriteEndElement(); // sequencepoints
        }

        private unsafe ImmutableArray<MethodDefinitionHandle> BuildMethodTokenMap()
        {
            if (!(_symReader is ISymUnmanagedReader4 symReader4) ||
                symReader4.GetPortableDebugMetadata(out byte* metadata, out int size) != 0)
            {
                return ImmutableArray<MethodDefinitionHandle>.Empty;
            }

            var reader = new MetadataReader(metadata, size);

            return (from handle in reader.GetEditAndContinueMapEntries()
                    where handle.Kind == HandleKind.MethodDebugInformation
                    select MetadataTokens.MethodDefinitionHandle(MetadataTokens.GetRowNumber(handle))).ToImmutableArray();
        }

        private IReadOnlyDictionary<string, int> BuildDocumentIndex(IReadOnlyList<ISymUnmanagedDocument> documents)
        {
            var index = new Dictionary<string, int>(documents.Count);

            int id = 1;
            foreach (var document in documents)
            {
                string name = document.GetName();

                // Native PDB doesn't allow no-name documents. SymWriter silently ignores them.
                // In Portable PDB all methods must be contained in a document, whose name may be empty. 
                // Skip such documents - like they were never in the document table.
                if (name.Length == 0)
                {
                    continue;
                }

                // Skip adding dups into the index, but increment id so that we 
                // can tell what methods are referring to the duplicate.
                if (!index.ContainsKey(name))
                {
                    index.Add(name, id);
                }

                id++;
            }

            return index;
        }

        private void WriteDocuments(IEnumerable<ISymUnmanagedDocument> documents, IReadOnlyDictionary<string, int> documentIndex)
        {
            bool hasDocument = false;

            foreach (var doc in documents)
            {
                string name = doc.GetName();

                int id;
                if (!documentIndex.TryGetValue(name, out id))
                {
                    continue;
                }

                if (!hasDocument)
                {
                    _writer.WriteStartElement("files");
                }

                hasDocument = true;

                _writer.WriteStartElement("file");

                _writer.WriteAttributeString("id", CultureInvariantToString(id));
                _writer.WriteAttributeString("name", name);
                _writer.WriteAttributeString("language", doc.GetLanguage().ToString());
                _writer.WriteAttributeString("languageVendor", doc.GetLanguageVendor().ToString());
                _writer.WriteAttributeString("documentType", doc.GetDocumentType().ToString());

                var algorithm = doc.GetHashAlgorithm();
                if (algorithm != default(Guid))
                {
                    var checksumBytes = doc.GetChecksum();
                    if (checksumBytes.Length != 0)
                    {
                        _writer.WriteAttributeString("checkSumAlgorithmId", algorithm.ToString());
                        _writer.WriteAttributeString("checkSum", string.Concat(checksumBytes.Select(b => $"{b,2:X}, ")));
                    }
                }

                Marshal.ThrowExceptionForHR(doc.HasEmbeddedSource(out bool hasEmbeddedSource));
                if (hasEmbeddedSource)
                {
                    Marshal.ThrowExceptionForHR(doc.GetSourceLength(out int sourceLength));
                    _writer.WriteAttributeString("embeddedSourceLength", sourceLength.ToString());
                }

                _writer.WriteEndElement();
            }

            if (hasDocument)
            {
                _writer.WriteEndElement();
            }
        }

        private void WriteAllMethodSpans()
        {
            if ((_options & PdbToXmlOptions.IncludeMethodSpans) == 0)
            {
                return;
            }

            _writer.WriteStartElement("method-spans");

            foreach (ISymUnmanagedDocument doc in _symReader.GetDocuments())
            {
                foreach (ISymUnmanagedMethod method in _symReader.GetMethodsInDocument(doc))
                {
                    _writer.WriteStartElement("method");

                    WriteMethodAttributes(method.GetToken(), isReference: true);

                    foreach (var methodDocument in method.GetDocumentsForMethod())
                    {
                        _writer.WriteStartElement("document");

                        int startLine, endLine;
                        ((ISymEncUnmanagedMethod)method).GetSourceExtentInDocument(methodDocument, out startLine, out endLine);

                        _writer.WriteAttributeString("startLine", startLine.ToString());
                        _writer.WriteAttributeString("endLine", endLine.ToString());

                        _writer.WriteEndElement();
                    }

                    _writer.WriteEndElement();
                }
            }

            _writer.WriteEndElement();
        }

        // Write out a reference to the entry point method (if one exists)
        private void WriteEntryPoint()
        {
            int token = _symReader.GetUserEntryPoint();
            if (token != 0)
            {
                _writer.WriteStartElement("entryPoint");
                WriteMethodAttributes(token, isReference: true);
                _writer.WriteEndElement();
            }
        }

        // Write out XML snippet to refer to the given method.
        private void WriteMethodAttributes(int token, bool isReference)
        {
            if ((_options & PdbToXmlOptions.ResolveTokens) != 0)
            {
                var handle = MetadataTokens.Handle(token);

                try
                {
                    switch (handle.Kind)
                    {
                        case HandleKind.MethodDefinition:
                            WriteResolvedToken((MethodDefinitionHandle)handle, isReference);
                            break;

                        case HandleKind.MemberReference:
                            WriteResolvedToken((MemberReferenceHandle)handle);
                            break;

                        default:
                            WriteToken(token);
                            _writer.WriteAttributeString("error", $"Unexpected token type: {handle.Kind}");
                            break;
                    }
                }
                catch (BadImageFormatException e) // TODO: filter
                {
                    if ((_options & PdbToXmlOptions.ThrowOnError) != 0)
                    {
                        throw;
                    }

                    WriteToken(token);
                    _writer.WriteAttributeString("metadata-error", e.Message);
                }
            }

            if ((_options & PdbToXmlOptions.IncludeTokens) != 0)
            {
                WriteToken(token);
            }
        }

        private static string GetQualifiedMethodName(MetadataReader metadataReader, MethodDefinitionHandle methodHandle)
        {
            var method = metadataReader.GetMethodDefinition(methodHandle);
            var containingTypeHandle = method.GetDeclaringType();

            string fullTypeName = GetFullTypeName(metadataReader, containingTypeHandle);
            string methodName = metadataReader.GetString(method.Name);

            return fullTypeName != null ? fullTypeName + "." + methodName : methodName;
        }

        private void WriteResolvedToken(MethodDefinitionHandle methodHandle, bool isReference)
        {
            var method = _metadataReader.GetMethodDefinition(methodHandle);

            // type name
            var containingTypeHandle = method.GetDeclaringType();
            var fullName = GetFullTypeName(_metadataReader, containingTypeHandle);
            if (fullName != null)
            {
                _writer.WriteAttributeString(isReference ? "declaringType" : "containingType", fullName);
            }

            // method name
            _writer.WriteAttributeString(isReference ? "methodName" : "name", _metadataReader.GetString(method.Name));

            // parameters:
            var parameterNames = (from paramHandle in method.GetParameters()
                                  let parameter = _metadataReader.GetParameter(paramHandle)
                                  where parameter.SequenceNumber > 0 // exclude return parameter
                                  select parameter.Name.IsNil ? "?" : _metadataReader.GetString(parameter.Name)).ToArray();

            if (parameterNames.Length > 0)
            {
                _writer.WriteAttributeString("parameterNames", string.Join(", ", parameterNames));
            }
        }

        private void WriteResolvedToken(MemberReferenceHandle memberRefHandle)
        {
            var memberRef = _metadataReader.GetMemberReference(memberRefHandle);

            // type name
            string fullName = GetFullTypeName(_metadataReader, memberRef.Parent);
            if (fullName != null)
            {
                _writer.WriteAttributeString("declaringType", fullName);
            }

            // method name
            _writer.WriteAttributeString("methodName", _metadataReader.GetString(memberRef.Name));
        }

        private static bool IsNested(TypeAttributes flags)
        {
            return (flags & ((TypeAttributes)0x00000006)) != 0;
        }

        private static string GetFullTypeName(MetadataReader metadataReader, EntityHandle handle)
        {
            if (handle.IsNil)
            {
                return null;
            }

            if (handle.Kind == HandleKind.TypeDefinition)
            {
                var type = metadataReader.GetTypeDefinition((TypeDefinitionHandle)handle);
                string name = metadataReader.GetString(type.Name);

                while (IsNested(type.Attributes))
                {
                    var enclosingType = metadataReader.GetTypeDefinition(type.GetDeclaringType());
                    name = metadataReader.GetString(enclosingType.Name) + "+" + name;
                    type = enclosingType;
                }

                if (type.Namespace.IsNil)
                {
                    return name;
                }

                return metadataReader.GetString(type.Namespace) + "." + name;
            }

            if (handle.Kind == HandleKind.TypeReference)
            {
                var typeRef = metadataReader.GetTypeReference((TypeReferenceHandle)handle);
                string name = metadataReader.GetString(typeRef.Name);
                if (typeRef.Namespace.IsNil)
                {
                    return name;
                }

                return metadataReader.GetString(typeRef.Namespace) + "." + name;
            }

            return "<" + string.Format(PdbToXmlResources.UnexpectedTokenKind, AsToken(metadataReader.GetToken(handle))) + ">";
        }

        private unsafe void WriteSourceServerInformation()
        {
            var srcsvrModule = _symReader as ISymUnmanagedSourceServerModule;
            if (srcsvrModule != null)
            {
                int hr = srcsvrModule.GetSourceServerData(out int length, out byte* data);
                if (hr != HResult.S_OK)
                {
                    return;
                }

                _writer.WriteStartElement("srcsvr");

                try
                {
                    string str = Encoding.UTF8.GetString(data, length);

                    try
                    {
                        _writer.WriteCData(str);
                    }
                    catch (ArgumentException)
                    {
                        try
                        {
                            _writer.WriteValue(str);
                        }
                        catch (ArgumentException)
                        {
                            _writer.WriteAttributeString("encoding", "base64");
                            var bytes = new byte[length];
                            Marshal.Copy((IntPtr)data, bytes, 0, bytes.Length);
                            _writer.WriteBase64(bytes, 0, bytes.Length);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeCoTaskMem((IntPtr)data);
                }

                _writer.WriteEndElement();
            }
        }

        #region Utils

        private void WriteToken(int token)
        {
            _writer.WriteAttributeString("token", AsToken(token));
        }

        internal static string AsToken(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x}", i);
        }

        internal static string AsILOffset(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "0x{0:x}", i);
        }

        internal static string CultureInvariantToString(int input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }

        #endregion
    }
}
