﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    internal static partial class PdbConverterWindowsToPortable
    {
        private unsafe static void ConvertConstantSignature(BlobBuilder builder, MetadataModel metadataModel, byte[] signature, object value)
        {
            fixed (byte* sigPtr = signature)
            {
                var sigReader = new BlobReader(sigPtr, signature.Length);

                // copy custom modifiers over:
                byte rawTypeCode;
                while (true)
                {
                    rawTypeCode = sigReader.ReadByte();
                    if (rawTypeCode != (int)SignatureTypeCode.OptionalModifier && rawTypeCode != (int)SignatureTypeCode.RequiredModifier)
                    {
                        break;
                    }

                    builder.WriteByte(rawTypeCode);
                    builder.WriteCompressedInteger(sigReader.ReadCompressedInteger());
                }

                switch ((SignatureTypeCode)rawTypeCode)
                {
                    case (SignatureTypeCode)SignatureTypeKind.Class:
                    case (SignatureTypeCode)SignatureTypeKind.ValueType:
                        int typeRefDefSpec = sigReader.ReadCompressedInteger();

                        if (value is decimal)
                        {
                            // GeneralConstant: VALUETYPE TypeDefOrRefOrSpecEncoded <decimal>
                            builder.WriteByte((byte)SignatureTypeKind.ValueType);
                            builder.WriteCompressedInteger(typeRefDefSpec);
                            builder.WriteDecimal((decimal)value);
                        }
                        else if (value is double d)
                        {
                            // GeneralConstant: VALUETYPE TypeDefOrRefOrSpecEncoded <date-time>
                            builder.WriteByte((byte)SignatureTypeKind.ValueType);
                            builder.WriteCompressedInteger(typeRefDefSpec);
                            builder.WriteDateTime(new DateTime(BitConverter.DoubleToInt64Bits(d)));
                        }
                        else if (value is 0 && rawTypeCode == (byte)SignatureTypeKind.Class)
                        {
                            // GeneralConstant: CLASS TypeDefOrRefOrSpecEncoded
                            builder.WriteByte(rawTypeCode);
                            builder.WriteCompressedInteger(typeRefDefSpec);
                        }
                        else
                        {
                            // EnumConstant ::= EnumTypeCode EnumValue EnumType
                            // EnumTypeCode ::= BOOLEAN | CHAR | I1 | U1 | I2 | U2 | I4 | U4 | I8 | U8
                            // EnumType     ::= TypeDefOrRefOrSpecEncoded

                            var enumTypeCode = AssemblyDisplayNameBuilder.GetConstantTypeCode(value);
                            builder.WriteByte((byte)enumTypeCode);
                            builder.WriteConstant(value);
                            builder.WriteCompressedInteger(typeRefDefSpec);
                        }

                        break;

                    case SignatureTypeCode.Object:
                        // null (null values are represented as 0 in Windows PDB):
                        Debug.Assert(value is 0);
                        builder.WriteByte((byte)SignatureTypeCode.Object);
                        break;

                    case SignatureTypeCode.Boolean:
                    case SignatureTypeCode.Char:
                    case SignatureTypeCode.SByte:
                    case SignatureTypeCode.Byte:
                    case SignatureTypeCode.Int16:
                    case SignatureTypeCode.UInt16:
                    case SignatureTypeCode.Int32:
                    case SignatureTypeCode.UInt32:
                    case SignatureTypeCode.Int64:
                    case SignatureTypeCode.UInt64:
                    case SignatureTypeCode.Single:
                    case SignatureTypeCode.Double:
                        // PrimitiveConstant
                        builder.WriteByte(rawTypeCode);
                        builder.WriteConstant(value);
                        break;

                    case SignatureTypeCode.String:
                        builder.WriteByte(rawTypeCode);
                        if (value is 0)
                        {
                            // null string
                            builder.WriteByte(0xff);
                        }
                        else if (value == null)
                        {
                            // empty string
                            builder.WriteUTF16(string.Empty);
                        }
                        else if (value is string str)
                        {
                            builder.WriteUTF16(str);
                        }
                        else
                        {
                            throw new BadImageFormatException();
                        }

                        break;

                    case SignatureTypeCode.SZArray:
                    case SignatureTypeCode.Array:
                    case SignatureTypeCode.GenericTypeInstance:
                        // Note: enums have non-null value and may be generic (null values are represented as 0 in Windows PDB):
                        Debug.Assert(rawTypeCode == (byte)SignatureTypeCode.GenericTypeInstance || value is 0);

                        // Find an existing TypeSpec in metadata.
                        // If there isn't one we can't represent the constant type in the Portable PDB, use Object.

                        // -1 for the type code we already read.
                        sigReader.Offset--;

                        var spec = sigReader.ReadBytes(sigReader.RemainingBytes);

                        TypeSpecificationHandle typeSpec;
                        if (metadataModel.TryResolveTypeSpecification(spec, out typeSpec))
                        {
                            builder.WriteCompressedInteger(CodedIndex.TypeDefOrRefOrSpec(typeSpec));
                        }
                        else if (rawTypeCode == (byte)SignatureTypeCode.GenericTypeInstance)
                        {
                            // enum constant (an integer):
                            builder.WriteByte((byte)AssemblyDisplayNameBuilder.GetConstantTypeCode(value));
                            builder.WriteConstant(value);

                            // TODO: warning - can't translate const type exactly
                        }
                        else
                        {
                            // null array:
                            builder.WriteByte((byte)SignatureTypeCode.Object);

                            // TODO: warning - can't translate const type exactly
                        }

                        break;

                    case SignatureTypeCode.GenericMethodParameter:
                    case SignatureTypeCode.GenericTypeParameter:
                    case SignatureTypeCode.FunctionPointer:
                    case SignatureTypeCode.Pointer:
                        // generic parameters, pointers are not valid types for constants:
                        throw new BadImageFormatException();
                }

                if (sigReader.RemainingBytes > 0)
                {
                    throw new BadImageFormatException();
                }
            }
        }
    }
}
