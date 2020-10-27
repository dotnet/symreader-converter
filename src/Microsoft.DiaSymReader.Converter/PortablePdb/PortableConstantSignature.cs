// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;

namespace Microsoft.DiaSymReader.PortablePdb
{
    // TODO: Copied from Microsoft.DiaSymReader.PortablePdb. Share.

    internal static class PortableConstantSignature
    {
        private static readonly object s_nullReferenceValue = 0;

        public static (object? Value, byte[] Signature) GetConstantValueAndSignature(MetadataReader mdReader, LocalConstantHandle handle, Func<EntityHandle, string?> getQualifiedTypeName)
        {
            var constant = mdReader.GetLocalConstant(handle);

            var sigReader = mdReader.GetBlobReader(constant.Signature);
            var sigWriter = new BlobWriter(sigReader.Length);

            // custom modifiers:
            int rawTypeCode;
            while (true)
            {
                rawTypeCode = sigReader.ReadCompressedInteger();
                if (rawTypeCode == (int)SignatureTypeCode.OptionalModifier || rawTypeCode == (int)SignatureTypeCode.RequiredModifier)
                {
                    sigReader.ReadCompressedInteger();
                }
                else
                {
                    break;
                }
            }

            int customModifiersLength = sigReader.Offset - 1;
            if (customModifiersLength > 0)
            {
                sigWriter.Write(mdReader.GetBlobBytes(constant.Signature), 0, customModifiersLength);
            }

            object? translatedValue;
            if (rawTypeCode == (int)MetadataUtilities.SignatureTypeCode_ValueType ||
                rawTypeCode == (int)MetadataUtilities.SignatureTypeCode_Class)
            {
                var typeHandle = sigReader.ReadTypeHandle();

                string? qualifiedName = getQualifiedTypeName(typeHandle);
                if (qualifiedName == "System.Decimal")
                {
                    translatedValue = sigReader.ReadDecimal();
                }
                else if (qualifiedName == "System.DateTime")
                {
                    translatedValue = BitConverter.Int64BitsToDouble(sigReader.ReadDateTime().Ticks);
                }
                else if (sigReader.RemainingBytes == 0)
                {
                    // null reference is returned as a boxed integer 0:
                    translatedValue = s_nullReferenceValue;
                }
                else
                {
                    // unknown (not produced by C# or VB)
                    translatedValue = null;
                }

                sigWriter.Write((byte)rawTypeCode);
                sigWriter.WriteCompressedInteger(MetadataUtilities.GetTypeDefOrRefOrSpecCodedIndex(typeHandle));
            }
            else
            {
                bool isEnumTypeCode;
                translatedValue = ReadAndTranslateValue(ref sigReader, (SignatureTypeCode)rawTypeCode, out isEnumTypeCode);

                if (sigReader.RemainingBytes == 0)
                {
                    // primitive type code:
                    sigWriter.Write((byte)rawTypeCode);
                }
                else if (isEnumTypeCode)
                {
                    var enumTypeHandle = sigReader.ReadTypeHandle();

                    // enum type signature:
                    sigWriter.Write((byte)MetadataUtilities.SignatureTypeCode_ValueType);
                    sigWriter.WriteCompressedInteger(MetadataUtilities.GetTypeDefOrRefOrSpecCodedIndex(enumTypeHandle));
                }

                if (sigReader.RemainingBytes > 0)
                {
                    throw new BadImageFormatException();
                }
            }

            return (translatedValue, sigWriter.ToArray());
        }

        private static object ReadAndTranslateValue(ref BlobReader sigReader, SignatureTypeCode typeCode, out bool isEnumTypeCode)
        {
            switch (typeCode)
            {
                case SignatureTypeCode.Boolean:
                    isEnumTypeCode = true;
                    return (short)(sigReader.ReadBoolean() ? 1 : 0);

                case SignatureTypeCode.Char:
                    isEnumTypeCode = true;
                    return (ushort)sigReader.ReadChar();

                case SignatureTypeCode.SByte:
                    isEnumTypeCode = true;
                    return (short)sigReader.ReadSByte();

                case SignatureTypeCode.Byte:
                    isEnumTypeCode = true;
                    return (short)sigReader.ReadByte();

                case SignatureTypeCode.Int16:
                    isEnumTypeCode = true;
                    return sigReader.ReadInt16();

                case SignatureTypeCode.UInt16:
                    isEnumTypeCode = true;
                    return sigReader.ReadUInt16();

                case SignatureTypeCode.Int32:
                    isEnumTypeCode = true;
                    return sigReader.ReadInt32();

                case SignatureTypeCode.UInt32:
                    isEnumTypeCode = true;
                    return sigReader.ReadUInt32();

                case SignatureTypeCode.Int64:
                    isEnumTypeCode = true;
                    return sigReader.ReadInt64();

                case SignatureTypeCode.UInt64:
                    isEnumTypeCode = true;
                    return sigReader.ReadUInt64();

                case SignatureTypeCode.Single:
                    isEnumTypeCode = false;
                    return sigReader.ReadSingle();

                case SignatureTypeCode.Double:
                    isEnumTypeCode = false;
                    return sigReader.ReadDouble();

                case SignatureTypeCode.String:
                    isEnumTypeCode = false;

                    if (sigReader.RemainingBytes == 1)
                    {
                        if (sigReader.ReadByte() != 0xff)
                        {
                            throw new BadImageFormatException();
                        }

                        return s_nullReferenceValue;
                    }

                    if (sigReader.RemainingBytes % 2 != 0)
                    {
                        throw new BadImageFormatException();
                    }

                    return sigReader.ReadUTF16(sigReader.RemainingBytes);

                case SignatureTypeCode.Object:
                    // null reference
                    isEnumTypeCode = false;
                    return s_nullReferenceValue;

                default:
                    throw new BadImageFormatException();
            }
        }
    }
}
