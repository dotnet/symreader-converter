// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.DiaSymReader.Tools
{
    internal static class StreamUtilities
    {
        public static void ValidateStream(Stream stream, string parameterName, bool readRequired = false, bool writeRequired = false, bool seekRequired = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (readRequired && !stream.CanRead)
            {
                throw new ArgumentException(ConverterResources.StreamMustBeReadable, parameterName);
            }

            if (writeRequired && !stream.CanWrite)
            {
                throw new ArgumentException(ConverterResources.StreamMustBeWritable, parameterName);
            }

            if (seekRequired && !stream.CanSeek)
            {
                throw new ArgumentException(ConverterResources.StreamMustBeSeakable, parameterName);
            }
        }
    }
}
