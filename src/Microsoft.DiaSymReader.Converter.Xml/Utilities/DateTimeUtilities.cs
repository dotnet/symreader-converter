﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;

namespace Roslyn.Utilities
{
    internal static class DateTimeUtilities
    {
        internal const string DateTimeDateDataFieldName = "dateData";

        // From DateTime.cs.
        private const long TicksMask = 0x3FFFFFFFFFFFFFFF;

        internal static DateTime ToDateTime(double raw)
        {
            // This mechanism for getting the tick count from the underlying ulong field is copied
            // from System.DateTime.InternalTicks (ndp\clr\src\BCL\System\DateTime.cs).
            var tickCount = BitConverter.DoubleToInt64Bits(raw) & TicksMask;
            return new DateTime(tickCount);
        }

        internal static DateTime ToDateTime(ulong raw)
        {
            // This mechanism for getting the tick count from the underlying ulong field is copied
            // from System.DateTime.InternalTicks (ndp\clr\src\BCL\System\DateTime.cs).
            var tickCount = unchecked((long)raw) & TicksMask;
            return new DateTime(tickCount);
        }
    }
}
