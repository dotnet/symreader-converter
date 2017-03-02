﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    public struct TestResource
    {
        public readonly byte[] PE;
        public readonly byte[] Pdb;

        public TestResource(byte[] pe, byte[] pdb)
        {
            PE = pe;
            Pdb = pdb;
        }

        public override string ToString()
        {
            return "TR";
        }
    }
}
