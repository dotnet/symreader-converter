// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Roslyn.Utilities;

namespace Microsoft.DiaSymReader.Tools
{
    partial class PdbConverterWindowsToPortable
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
                        if (value is 0)
                        {
                            builder.WriteByte((byte)SignatureTypeCode.Object);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Boolean:
                        if (value is short boolValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteBoolean(boolValue != 0);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.SByte:
                        if (value is short sbyteValue && sbyteValue >= sbyte.MinValue && sbyteValue <= sbyte.MaxValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteSByte((sbyte)sbyteValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Byte:
                        if (value is short byteValue && byteValue >= byte.MinValue && byteValue <= byte.MaxValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteByte((byte)byteValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Int16:
                        if (value is short shortValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteInt16(shortValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Char:
                    case SignatureTypeCode.UInt16:
                        if (value is ushort charValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteUInt16(charValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Int32:
                        if (value is int intValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteInt32(intValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.UInt32:
                        if (value is uint uintValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteUInt32(uintValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Int64:
                        if (value is long longValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteInt64(longValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.UInt64:
                        if (value is ulong ulongValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteUInt64(ulongValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Single:
                        if (value is float floatValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteSingle(floatValue);
                            break;
                        }

                        throw new BadImageFormatException();

                    case SignatureTypeCode.Double:
                        if (value is double doubleValue)
                        {
                            builder.WriteByte(rawTypeCode);
                            builder.WriteDouble(doubleValue);
                            break;
                        }

                        throw new BadImageFormatException();

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
