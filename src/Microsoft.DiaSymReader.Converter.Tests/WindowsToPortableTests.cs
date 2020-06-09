// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using Roslyn.Test.Utilities;

namespace Microsoft.DiaSymReader.Tools.UnitTests
{
    using static PdbValidationMetadata;

    public class WindowsToPortableTests
    {
        [Fact]
        public void Convert_Documents()
        {
            VerifyPortablePdb(
                TestResources.Documents.DllAndPdb(portable: true),
                TestResources.Documents.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 104): 
=================================================================================================================================================
   Name               Language  HashAlgorithm  Hash                                                         
=================================================================================================================================================
1: '/_/Documents.cs'  C#        SHA-256        46-E0-DA-8D-C6-03-94-AE-09-FA-AD-1C-D8-6F-60-19-64-BE-4E-5A-B0-D8-D9-60-0D-79-E8-92-50-16-04-06  
2: 'C:\a\b\c\d\1.cs'  C#        nil            nil                                                          
3: 'C:\a\b\c\D\2.cs'  C#        nil            nil                                                          
4: 'C:\a\b\C\d\3.cs'  C#        nil            nil                                                          
5: 'C:\a\b\c\d\x.cs'  C#        nil            nil                                                          
6: 'C:\A\b\c\x.cs'    C#        nil            nil                                                          
7: 'C:\a\b\x.cs'      C#        nil            nil                                                          
8: 'C:\a\B\3.cs'      C#        nil            nil                                                          
9: 'C:\a\B\c\4.cs'    C#        nil            nil                                                          
a: 'C:\*\5.cs'        C#        nil            nil                                                          
b: ':6.cs'            C#        nil            nil                                                          
c: 'C:\a\b\X.cs'      C#        nil            nil                                                          
d: 'C:\a\B\x.cs'      C#        nil            nil                                                          

MethodDebugInformation (index: 0x31, size: 8): 
===============================================
   IL   
===============================================
1:
{
  IL_0000: (6, 5) - (6, 6) [#1]
  IL_0001: (10, 9) - (10, 30) [#2]
  IL_0008: (20, 9) - (20, 30) [#3]
  IL_000F: (30, 9) - (30, 30) [#4]
  IL_0016: (40, 9) - (40, 30) [#4]
  IL_001D: <hidden>
  IL_0023: (50, 9) - (50, 30) [#5]
  IL_002A: (60, 9) - (60, 30) [#6]
  IL_0031: (70, 9) - (70, 30) [#7]
  IL_0038: (80, 9) - (80, 30) [#8]
  IL_003F: (90, 9) - (90, 30) [#9]
  IL_0046: (100, 9) - (100, 30) [#a]
  IL_004D: (110, 9) - (110, 30) [#b]
  IL_0054: (120, 9) - (120, 30) [#c]
  IL_005B: (130, 9) - (130, 30) [#d]
  IL_0062: (131, 5) - (131, 6) [#d]
}
2: nil

LocalScope (index: 0x32, size: 16): 
===================================================================================================
   Method                  ImportScope               Variables  Constants  StartOffset  Length  
===================================================================================================
1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       99      

ImportScope (index: 0x35, size: 8): 
======================================
   Parent                    Imports   
======================================
1: nil (ImportScope)         nil       
2: 0x35000001 (ImportScope)  'System'
«
CustomDebugInformation (index: 0x37, size: 12): 
==============================================================================================================================================================
    Parent               Kind                             Value                                                                                                
==============================================================================================================================================================
1: 0x00000001 (Module)  Compilation Options              63-6F-6D-70-69-6C-65-72-2D-76-65-72-73-69-6F-6E-00-33-2E-37-2E-30-2D-64-65-76-00-6C-61-6E-67-75-...  
{
  compiler-version: 3.7.0-dev
  language: C#
  portability-policy: 0
  runtime-version: 4.8.4180.0
  optimization: debug
  checked: False
  nullable: Disable
  unsafe: False
  language-version: 8.0
}
2: 0x00000001 (Module)  Compilation Metadata References  6D-73-63-6F-72-6C-69-62-2E-64-6C-6C-00-00-01-D3-20-7D-5E-00-00-53-00-B5-26-EC-4B-C7-CB-15-47-84-...  
{
  -------------------------------------------------------------------------------------------------------
      FileName        Aliases  Flags     TimeStamp   FileSize    MVID                                  
  -------------------------------------------------------------------------------------------------------
  0: 'mscorlib.dll'  ''       Assembly  0x5E7D20D3  0x00530000  4bec26b5-cbc7-4715-8442-f1499e984732  
}
»«»
");
        }

        [Fact]
        public void Convert_Scopes()
        {
            VerifyPortablePdb(
                TestResources.Scopes.DllAndPdb(portable: true),
                TestResources.Scopes.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 8): 
==========================================================================================================
   Name            Language  HashAlgorithm  Hash                                                         
==========================================================================================================
1: 'C:\Scopes.cs'  C#        SHA-1          DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE  

MethodDebugInformation (index: 0x31, size: 20): 
================================================
   IL   
================================================
1: nil
2: 
{
  Document: #1

  IL_0000: (21, 5) - (21, 6)
  IL_0001: (54, 5) - (54, 6)
}
3: 
{
  Locals: «0x11000001»«0x1100001b» (StandAloneSig)
  Document: #1

  IL_0000: (57, 5) - (57, 6)
  IL_0001: (58, 9) - (58, 20)
  IL_0003: (59, 9) - (59, 10)
  IL_0004: (61, 13) - (61, 24)
  IL_0006: (62, 9) - (62, 10)
  IL_0007: (64, 9) - (64, 20)
  IL_0009: (65, 9) - (65, 10)
  IL_000A: (66, 13) - (66, 24)
  IL_000C: (67, 13) - (67, 14)
  IL_000D: (70, 17) - (70, 28)
  IL_0010: (71, 13) - (71, 14)
  IL_0011: (72, 9) - (72, 10)
  IL_0012: (73, 5) - (73, 6)
}
4: 
{
  Locals: «0x11000002»«0x1100001c» (StandAloneSig)
  Document: #1

  IL_0000: (76, 5) - (76, 6)
  IL_0001: (77, 9) - (77, 19)
  IL_0003: (78, 9) - (78, 10)
  IL_0004: (79, 13) - (79, 23)
  IL_0006: (80, 13) - (80, 14)
  IL_0007: (81, 17) - (81, 27)
  IL_0009: (82, 13) - (82, 14)
  IL_000A: (83, 13) - (83, 14)
  IL_000B: (84, 17) - (84, 27)
  IL_000D: (85, 13) - (85, 14)
  IL_000E: (86, 9) - (86, 10)
  IL_000F: (87, 9) - (87, 10)
  IL_0010: (88, 13) - (88, 23)
  IL_0013: (89, 13) - (89, 14)
  IL_0014: (90, 17) - (90, 18)
  IL_0015: (91, 21) - (91, 31)
  IL_0018: (92, 17) - (92, 18)
  IL_0019: (93, 17) - (93, 18)
  IL_001A: (94, 21) - (94, 31)
  IL_001D: (95, 21) - (95, 22)
  IL_001E: (96, 25) - (96, 35)
  IL_0021: (97, 25) - (97, 35)
  IL_0024: (98, 21) - (98, 22)
  IL_0025: (99, 17) - (99, 18)
  IL_0026: (100, 17) - (100, 18)
  IL_0027: (101, 21) - (101, 31)
  IL_002A: (102, 17) - (102, 18)
  IL_002B: (103, 13) - (103, 14)
  IL_002C: (104, 13) - (104, 14)
  IL_002D: (105, 17) - (105, 27)
  IL_0030: (106, 17) - (106, 27)
  IL_0033: (107, 13) - (107, 14)
  IL_0034: (108, 9) - (108, 10)
  IL_0035: (109, 5) - (109, 6)
}
5: nil

LocalScope (index: 0x32, size: 240): 
===========================================================================================================================
    Method                  ImportScope               Variables              Constants              StartOffset  Length  
===========================================================================================================================
 1: 0x06000002 (MethodDef)  0x35000002 (ImportScope)  nil                    0x34000001-0x3400001d  0x0000       2       
 2: 0x06000003 (MethodDef)  0x35000002 (ImportScope)  0x33000001-0x33000002  nil                    0x0000       19      
 3: 0x06000003 (MethodDef)  0x35000002 (ImportScope)  0x33000003-0x33000003  0x3400001e-0x3400001e  0x0003       4       
 4: 0x06000003 (MethodDef)  0x35000002 (ImportScope)  0x33000004-0x33000004  nil                    0x0009       9       
 5: 0x06000003 (MethodDef)  0x35000002 (ImportScope)  0x33000005-0x33000005  0x3400001f-0x34000020  0x000C       5       
 6: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x33000006-0x33000006  nil                    0x0000       54      
 7: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x33000007-0x33000007  nil                    0x0003       12      
 8: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x33000008-0x33000008  nil                    0x0006       4       
 9: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x33000009-0x33000009  nil                    0x000A       4       
 a: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x3300000a-0x3300000a  nil                    0x000F       38      
 b: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x3300000b-0x3300000b  nil                    0x0014       5       
 c: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x3300000c-0x3300000c  nil                    0x0019       13      
 d: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x3300000d-0x3300000e  nil                    0x001D       8       
 e: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x3300000f-0x3300000f  nil                    0x0026       5       
 f: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  0x33000010-0x33000011  nil                    0x002C       8       

LocalVariable (index: 0x33, size: 102): 
========================================
    Name  Index  Attributes  
========================================
 1: 'x0'  0      0        
 2: 'y0'  1      0        
 3: 'x1'  2      0        
 4: 'y1'  3      0        
 5: 'y2'  4      0        
 6: 'a'   0      0        
 7: 'b'   1      0        
 8: 'c'   2      0        
 9: 'd'   3      0        
 a: 'e'   4      0        
 b: 'f'   5      0        
 c: 'g'   6      0        
 d: 'h'   7      0        
 e: 'd'   8      0        
 f: 'i'   9      0        
10: 'j'   10     0        
11: 'd'   11     0        

LocalConstant (index: 0x34, size: 128): 
=================================================================================
    Name            Signature                                                      
=================================================================================
 1: 'B'             False [Boolean]                                                
 2: 'C'             '\u0000' [Char]                                                
 3: 'I1'            1 [SByte]                                                      
 4: 'U1'            2 [Byte]                                                       
 5: 'I2'            3 [Int16]                                                      
 6: 'U2'            4 [UInt16]                                                     
 7: 'I4'            5 [Int32]                                                      
 8: 'U4'            6 [UInt32]                                                     
 9: 'I8'            7 [Int64]                                                      
 a: 'U8'            8 [UInt64]                                                     
 b: 'R4'            9.1 [Single]                                                   
 c: 'R8'            10.2 [Double]                                                  
 d: 'EI1'           1 [«0x1b000001 (TypeSpec)»«Int16»]
 e: 'EU1'           2 [«0x1b000002 (TypeSpec)»«Int16»]                                      
 f: 'EI2'           3 [«0x1b000003 (TypeSpec)»«Int16»]                                      
10: 'EU2'           4 [«0x1b000004 (TypeSpec)»«UInt16»]                                      
11: 'EI4'           5 [«0x1b000005 (TypeSpec)»«Int32»]                                      
12: 'EU4'           6 [«0x1b000006 (TypeSpec)»«UInt32»]                                      
13: 'EI8'           7 [«0x1b000007 (TypeSpec)»«Int64»]                                      
14: 'EU8'           8 [«0x1b000008 (TypeSpec)»«UInt64»]                                      
15: 'StrWithNul'    '\u0000' [String]
16: 'EmptyStr'      '' [String]
17: 'NullStr'       null [String]
18: 'NullObject'    null [Object]
19: 'NullDynamic'   null [Object]
1a: 'NullTypeDef'   default [0x02000002 (TypeDef)]
1b: 'NullTypeRef'   default [0x01000006 (TypeRef)]
1c: 'NullTypeSpec'  «default [0x1b000009 (TypeSpec)]»«0 [Int32]»
1d: 'D'             02-4E-61-BC-00-00-00-00-00-00-00-00-00 [0x0100000a (TypeRef)]
1e: 'c1'            11 [Int32]
1f: 'c2'            'c2' [String]
20: 'd2'            'd2' [String]

ImportScope (index: 0x35, size: 8): 
====================================================================
   Parent                    Imports                                 
====================================================================
1: nil (ImportScope)         nil                                     
2: 0x35000001 (ImportScope)  'System', 'System.Collections.Generic'  

CustomDebugInformation (index: 0x37, size: 24): 
=============================================================================================================================================================
   Parent                      Kind                     Value                                                                                               
=============================================================================================================================================================
1: 0x06000003 (MethodDef)      EnC Local Slot Map       01-10-01-76-01-54-01-80-9A-01-81-24                                                                 
2: 0x06000004 (MethodDef)      EnC Local Slot Map       01-10-01-33-01-5E-01-80-98-01-80-D5-01-81-17-01-81-5D-01-81-98-01-81-BC-01-82-19-01-82-66-01-82-...    
3: 0x34000019 (LocalConstant)  Dynamic Local Variables  01                                                                                                  
4: 0x3400001c (LocalConstant)  Dynamic Local Variables  20");
        }

        [Fact]
        public void Convert_Async()
        {
            VerifyPortablePdb(
                TestResources.Async.DllAndPdb(portable: true),
                TestResources.Async.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 8): 
=========================================================================================================
   Name           Language  HashAlgorithm  Hash                                                         
=========================================================================================================
1: 'C:\Async.cs'  C#        SHA-1          DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE  

MethodDebugInformation (index: 0x31, size: 36): 
================================================
   IL   
================================================
1: nil
2: nil
3: nil
4: nil
5: 
{
  Kickoff Method: 0x06000001 (MethodDef)
  Locals: 0x11000003 (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_0007: <hidden>
  IL_0027: (8, 2) - (8, 3)
  IL_0028: (9, 3) - (9, 28)
  IL_0034: <hidden>
  IL_0090: (10, 3) - (10, 28)
  IL_009D: <hidden>
  IL_00FB: (11, 3) - (11, 28)
  IL_0108: <hidden>
  IL_0163: (13, 3) - (13, 12)
  IL_0167: <hidden>
  IL_0181: (14, 5) - (14, 6)
  IL_0189: <hidden>
}
6: nil
7: nil
8: 
{
  Kickoff Method: 0x06000002 (MethodDef)
  Locals: 0x11000004 (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_0007: <hidden>
  IL_000E: (17, 2) - (17, 3)
  IL_000F: (18, 3) - (18, 28)
  IL_001B: <hidden>
  IL_0076: <hidden>
  IL_008E: (19, 5) - (19, 6)
  IL_0096: <hidden>
}
9: nil

LocalScope (index: 0x32, size: 64): 
===================================================================================================
   Method                  ImportScope               Variables  Constants  StartOffset  Length  
===================================================================================================
1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       59      
2: 0x06000002 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       48      
3: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       407     
4: 0x06000008 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       163     

ImportScope (index: 0x35, size: 8): 
======================================================
   Parent                    Imports                   
======================================================
1: nil (ImportScope)         nil                       
2: 0x35000001 (ImportScope)  'System.Threading.Tasks'  

CustomDebugInformation (index: 0x37, size: 24): 
============================================================================================================================================================
   Parent                  Kind                               Value                                                                                         
============================================================================================================================================================
1: 0x06000005 (MethodDef)  Async Method Stepping Information  00-00-00-00-46-00-00-00-64-00-00-00-05-AF-00-00-00-CE-00-00-00-05-1A-01-00-00-36-01-00-00-05  
2: 0x06000005 (MethodDef)  EnC Local Slot Map                 1C-01-15-01-22-06-00-22-24-22-42-00                                                           
3: 0x06000008 (MethodDef)  Async Method Stepping Information  77-00-00-00-2D-00-00-00-48-00-00-00-08                                                        
4: 0x06000008 (MethodDef)  EnC Local Slot Map                 1C-01-22-06-00-00
");
        }

        [Fact]
        public void Convert_Iterator()
        {
            VerifyPortablePdb(
                TestResources.Iterator.DllAndPdb(portable: true),
                TestResources.Iterator.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 8): 
============================================================================================================
   Name              Language  HashAlgorithm  Hash                                                         
============================================================================================================
1: 'C:\Iterator.cs'  C#        SHA-1          DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE  

MethodDebugInformation (index: 0x31, size: 80): 
================================================
   IL   
================================================
1: nil
2: nil
3: nil
4: nil
5: nil
6: nil
7: 
{
  Kickoff Method: 0x06000002 (MethodDef)
  Locals: 0x11000001 (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_001F: (10, 9) - (10, 10)
  IL_0020: (11, 13) - (11, 23)
  IL_0027: (13, 18) - (13, 27)
  IL_002E: <hidden>
  IL_0030: (14, 13) - (14, 14)
  IL_0031: (16, 17) - (16, 27)
  IL_0038: (17, 17) - (17, 48)
  IL_005D: <hidden>
  IL_0064: (18, 13) - (18, 14)
  IL_0065: (13, 37) - (13, 40)
  IL_0075: (13, 29) - (13, 35)
  IL_0080: <hidden>
  IL_0083: (19, 9) - (19, 10)
}
8: nil
9: nil
a: nil
b: nil
c: nil
d: nil
e: nil
f: 
{
  Kickoff Method: 0x06000003 (MethodDef)
  Locals: «0x11000003»«0x11000004» (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_001F: (22, 9) - (22, 10)
  IL_0020: (23, 13) - (23, 28)
  IL_0030: <hidden>
  IL_0037: (24, 9) - (24, 10)
}
10: nil
11: nil
12: nil
13: nil
14: nil

LocalScope (index: 0x32, size: 96): 
===============================================================================================================
   Method                  ImportScope               Variables  Constants              StartOffset  Length  
===============================================================================================================
1: 0x06000002 (MethodDef)  0x35000002 (ImportScope)  nil        nil                    0x0000       15      
2: 0x06000003 (MethodDef)  0x35000002 (ImportScope)  nil        nil                    0x0000       15      
3: 0x06000007 (MethodDef)  0x35000002 (ImportScope)  nil        nil                    0x0000       133     
4: 0x06000007 (MethodDef)  0x35000002 (ImportScope)  nil        0x34000001-0x34000001  0x001F       102     
5: 0x06000007 (MethodDef)  0x35000002 (ImportScope)  nil        0x34000002-0x34000002  0x0030       53      
6: 0x0600000f (MethodDef)  0x35000002 (ImportScope)  nil        nil                    0x0000       57      

LocalConstant (index: 0x34, size: 8): 
======================================
   Name  Signature  
======================================
1: 'x'   1 [Int32]  
2: 'y'   2 [Int32]  

ImportScope (index: 0x35, size: 8): 
==========================================================
   Parent                    Imports                       
==========================================================
1: nil (ImportScope)         nil                           
2: 0x35000001 (ImportScope)  'System.Collections.Generic'  

CustomDebugInformation (index: 0x37, size: 24): 
========================================================================================================================================
   Parent                  Kind                                Value                                                                    
========================================================================================================================================
1: 0x06000002 (MethodDef)  EnC Local Slot Map                  01-14-01-4F-01-80-A9                                                     
2: 0x06000007 (MethodDef)  EnC Local Slot Map                  1C-01-00-02-46                                                           
3: 0x06000007 (MethodDef)  State Machine Hoisted Local Scopes  1F-00-00-00-66-00-00-00-27-00-00-00-5C-00-00-00-30-00-00-00-35-00-00-00  
4: 0x0600000f (MethodDef)  EnC Local Slot Map                  1C-01
");
        }

        [Fact]
        public void Convert_Imports()
        {
            VerifyPortablePdb(
                TestResources.Imports.DllAndPdb(portable: true),
                TestResources.Imports.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 8): 
===========================================================================================================
   Name             Language  HashAlgorithm  Hash                                                         
===========================================================================================================
1: 'C:\Imports.cs'  C#        SHA-1          DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE  

MethodDebugInformation (index: 0x31, size: 20): 
================================================
   IL   
================================================
1: 
{
  Document: #1

  IL_0000: (39, 14) - (39, 15)
  IL_0001: (39, 16) - (39, 17)
}
2: nil
3: 
{
  Document: #1

  IL_0000: (28, 22) - (28, 23)
  IL_0001: (28, 24) - (28, 25)
}
4: nil
5: nil

LocalScope (index: 0x32, size: 32): 
===================================================================================================
   Method                  ImportScope               Variables  Constants  StartOffset  Length  
===================================================================================================
1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       2       
2: 0x06000003 (MethodDef)  0x35000004 (ImportScope)  nil        nil        0x0000       2       

ImportScope (index: 0x35, size: 16): 
=============================================================================================================================================================================================================================================================================================================================================================================================================================================
   Parent                    Imports                                                                                                                                                                                                                                                                                                                                                                                                          
=============================================================================================================================================================================================================================================================================================================================================================================================================================================
1: nil (ImportScope)         'ExternAlias1' = 0x23000002 (AssemblyRef)                                                                                                                                                                                                                                                                                                                                                                        
2: 0x35000001 (ImportScope)  nil                                                                                                                                                                                                                                                                                                                                                                                                              
3: 0x35000002 (ImportScope)  Extern Alias 'ExternAlias1', 'System', 'System.Linq', 0x01000008 (TypeRef), 0x01000009 (TypeRef), 'AliasedNamespace1' = 'System', 'AliasedNamespace2' = 'System.IO', 'AliasedType1' = 0x0100000a (TypeRef), 'AliasedType2' = 0x0100000b (TypeRef), 'AliasedType3' = 0x02000005 (TypeDef), 'AliasedType4' = 0x1b000003 (TypeSpec), 'AliasedType5' = 0x0100000f (TypeRef), 'AliasedType6' = 0x1b000004 (TypeSpec)  
4: 0x35000003 (ImportScope)  'AliasedType7' = 0x1b000001 (TypeSpec), 'AliasedType8' = 0x1b000002 (TypeSpec)
");
        }

        [Fact]
        public void Convert_MethodBoundaries()
        {
            VerifyPortablePdb(
                TestResources.MethodBoundaries.DllAndPdb(portable: true),
                TestResources.MethodBoundaries.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 32): 
=========================================================================================================================================================
   Name                       Language  HashAlgorithm  Hash                                                         
=========================================================================================================================================================
1: 'C:\MethodBoundaries1.cs'  C#        SHA-1          DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE  
2: 'C:\MethodBoundaries2.cs'  C#        nil            nil                                                          
3: 'C:\MethodBoundaries3.cs'  C#        nil            nil               
4: '/_/MethodBoundaries.cs'   C#        SHA-256        62-AB-2C-7D-4E-39-5E-50-62-5B-A8-CF-EA-41-DD-5C-C0-81-A2-AB-B1-F7-14-EE-49-2C-51-F0-26-D7-DB-A9

MethodDebugInformation (index: 0x31, size: 64): 
================================================
    IL  
================================================
1: 
{
  Document: #1

  IL_0000: (5, 5) - (7, 17)
  IL_0011: (14, 5) - (14, 17)
  IL_001C: (9, 5) - (9, 15)
  IL_0023: (10, 5) - (10, 6)
  IL_0024: (11, 9) - (11, 13)
  IL_002A: (12, 5) - (12, 6)
}
2: 
{
  Locals: 0x11000001 (StandAloneSig)

  IL_0000: (17, 5) - (17, 6) [#1]
  IL_0001: (10, 9) - (10, 13) [#1]
  IL_0007: (5, 9) - (5, 13) [#1]
  IL_000D: (7, 9) - (7, 13) [#1]
  IL_0013: (8, 9) - (8, 13) [#1]
  IL_0019: (5, 9) - (5, 13) [#1]
  IL_001F: (1, 9) - (1, 13) [#2]
  IL_0025: (20, 9) - (20, 13) [#1]
  IL_002B: (22, 9) - (22, 18) [#1]
  IL_002F: (23, 5) - (23, 6) [#1]
}
3: 
{
  Locals: 0x11000001 (StandAloneSig)
  Document: #1

  IL_0000: (4, 5) - (4, 6)
  IL_0001: (5, 9) - (7, 11)
  IL_0007: (8, 9) - (8, 18)
  IL_000B: (9, 5) - (9, 6)
}
4: 
{
  Document: #2

  IL_0000: (5, 31) - (5, 34)
}
5: 
{
  Document: #2

  IL_0000: (7, 31) - (7, 34)
}
6: 
{
  Locals: 0x11000001 (StandAloneSig)
  Document: #2

  IL_0000: (4, 5) - (4, 6)
  IL_0001: (5, 9) - (8, 11)
  IL_0007: (9, 9) - (9, 18)
  IL_000B: (10, 5) - (10, 6)
}
7: 
{
  Document: #2

  IL_0000: (6, 31) - (6, 34)
}
8: 
{
  Document: #2

  IL_0000: (8, 5) - (9, 8)
}
9: 
{
  Document: #2

  IL_0000: (9, 5) - (9, 8)
}
a: 
{
  Document: #2

  IL_0000: (13, 5) - (13, 6)
  IL_0001: (14, 9) - (14, 13)
  IL_0007: (15, 5) - (15, 6)
}
b: 
{
  Locals: 0x11000001 (StandAloneSig)
  Document: #2

  IL_0000: (11, 5) - (11, 6)
  IL_0001: (12, 9) - (21, 11)
  IL_0007: (22, 9) - (22, 18)
  IL_000B: (23, 5) - (23, 6)
}
c: 
{
  Document: #2

  IL_0000: (16, 5) - (16, 6)
  IL_0001: (17, 9) - (17, 13)
  IL_0007: (28, 5) - (28, 6)
}
d: 
{
  Document: #3

  IL_0000: (1, 5) - (1, 6)
  IL_0001: (2, 9) - (11, 11)
  IL_0007: (12, 5) - (12, 6)
}
e: 
{
  Document: #3

  IL_0000: (3, 5) - (3, 6)
  IL_0001: (4, 9) - (10, 11)
  IL_0007: (11, 5) - (11, 6)
}
f: 
{
  Document: #3

  IL_0000: (5, 5) - (5, 6)
  IL_0001: (6, 9) - (9, 11)
  IL_0007: (10, 5) - (10, 6)
}
10: 
{
  Document: #3

  IL_0000: (7, 9) - (8, 10)
}

LocalScope (index: 0x32, size: 256): 
===================================================================================================
    Method                  ImportScope               Variables  Constants  StartOffset  Length  
===================================================================================================
 1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       43      
 2: 0x06000002 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       49      
 3: 0x06000003 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       13      
 4: 0x06000004 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       6       
 5: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       6       
 6: 0x06000006 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       13      
 7: 0x06000007 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       6       
 8: 0x06000008 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       12      
 9: 0x06000009 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       6       
 a: 0x0600000a (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       8       
 b: 0x0600000b (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       13      
 c: 0x0600000c (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       8       
 d: 0x0600000d (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       8       
 e: 0x0600000e (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       8       
 f: 0x0600000f (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       8       
10: 0x06000010 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       7       

ImportScope (index: 0x35, size: 8): 
=====================================
   Parent                    Imports  
=====================================
1: nil (ImportScope)         nil      
2: 0x35000001 (ImportScope)  nil      
«
CustomDebugInformation (index: 0x37, size: 36):
=================================================================================================================================================================
   Parent                  Kind                             Value                                                                                                
=================================================================================================================================================================
1: 0x00000001 (Module)     Compilation Options              63-6F-6D-70-69-6C-65-72-2D-76-65-72-73-69-6F-6E-00-33-2E-37-2E-30-2D-64-65-76-00-6C-61-6E-67-75-...  
{
  compiler-version: 3.7.0-dev
  language: C#
  portability-policy: 0
  runtime-version: 4.8.4180.0
  optimization: debug
  checked: False
  nullable: Disable
  unsafe: False
  language-version: 8.0
}
2: 0x00000001 (Module)     Compilation Metadata References  6D-73-63-6F-72-6C-69-62-2E-64-6C-6C-00-00-01-D3-20-7D-5E-00-00-53-00-B5-26-EC-4B-C7-CB-15-47-84-...  
{
  -------------------------------------------------------------------------------------------------------
     FileName        Aliases  Flags     TimeStamp   FileSize    MVID                                  
  -------------------------------------------------------------------------------------------------------
  0: 'mscorlib.dll'  ''       Assembly  0x5E7D20D3  0x00530000  4bec26b5-cbc7-4715-8442-f1499e984732  
}
3: 0x06000002 (MethodDef)  EnC Local Slot Map               16-01  
4: 0x06000003 (MethodDef)  EnC Local Slot Map               16-01  
5: 0x06000006 (MethodDef)  EnC Local Slot Map               16-01  
6: 0x0600000b (MethodDef)  EnC Local Slot Map               16-01
»«
CustomDebugInformation (index: 0x37, size: 24): 
======================================================
   Parent                  Kind                Value  
======================================================
1: 0x06000002 (MethodDef)  EnC Local Slot Map  16-01  
2: 0x06000003 (MethodDef)  EnC Local Slot Map  16-01  
3: 0x06000006 (MethodDef)  EnC Local Slot Map  16-01  
4: 0x0600000b (MethodDef)  EnC Local Slot Map  16-01
»
");
        }

        [Fact]
        public void Convert_LanguageOnlyTypes()
        {
            VerifyPortablePdb(
                TestResources.LanguageOnlyTypes.DllAndPdb(portable: true),
                TestResources.LanguageOnlyTypes.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 16): 
=========================================================================================================================================================
   Name                       Language  HashAlgorithm  Hash                                                         
=========================================================================================================================================================
1: '/_/LanguageOnlyTypes.cs'  C#        SHA-256        98-C7-6E-B0-6E-F0-49-67-5A-F3-73-86-AF-FE-24-EC-F9-D3-DC-C1-0A-09-99-52-32-9A-25-C0-0A-B5-E8-D0  
2: '/_/System.cs'             C#        SHA-256        5C-47-20-0B-0F-35-34-EA-05-59-9A-86-5A-49-38-C9-6D-90-BD-10-A0-4B-08-58-1F-18-0A-ED-BE-B7-2B-6D

MethodDebugInformation (index: 0x31, size: 56): 
================================================
   IL   
================================================
1: 
{
  Locals: 0x11000001 (StandAloneSig)
  Document: #1

  IL_0000: (8, 5) - (8, 6)
  IL_0001: (9, 9) - (9, 10)
  IL_0002: (10, 13) - (17, 125)
  IL_0004: (19, 13) - (25, 144)
  IL_000C: (29, 9) - (29, 10)
  IL_000D: (31, 9) - (31, 10)
  IL_000E: (32, 13) - (32, 99)
  IL_0010: (37, 9) - (37, 10)
  IL_0011: (38, 5) - (38, 6)
}
2: nil
3: nil
4: nil
5: nil
6: nil
7: nil
8: nil
9: nil
a: nil
b: nil
c: nil
d: nil
e: nil

LocalScope (index: 0x32, size: 48): 
===========================================================================================================================
   Method                  ImportScope               Variables              Constants              StartOffset  Length  
===========================================================================================================================
1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       18      
2: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  0x33000001-0x33000002  0x34000001-0x34000002  0x0001       12      
3: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  0x33000003-0x33000004  0x34000003-0x34000004  0x000D       4       

LocalVariable (index: 0x33, size: 24): 
=======================================
   Name  Index  Attributes  
=======================================
1: 'v1'  0      0        
2: 'v2'  1      0        
3: 'v1'  2      0        
4: 'v2'  3      0        

LocalConstant (index: 0x34, size: 16): 
=======================================«==»«»
   Name  Signature                        
=======================================«==»«»
1: 'c1'  «default [0x1b000002 (TypeSpec)]»«0 [Int32]»  
2: 'c2'  «default [0x1b000003 (TypeSpec)]»«0 [Int32]»  
3: 'c1'  «default [0x1b000004 (TypeSpec)]»«0 [Int32]»  
4: 'c2'  «default [0x1b000004 (TypeSpec)]»«0 [Int32]»  

ImportScope (index: 0x35, size: 8): 
======================================
   Parent                    Imports   
======================================
1: nil (ImportScope)         nil       
2: 0x35000001 (ImportScope)  'System'  
«
CustomDebugInformation (index: 0x37, size: 78): 
=====================================================================================================================================================================
   Parent                      Kind                             Value                                                                                               
=====================================================================================================================================================================
1: 0x06000001 (MethodDef)      EnC Local Slot Map               01-83-48-01-83-66-01-87-AA-01-88-08                                                                 
2: 0x00000001 (Module)         Compilation Options              63-6F-6D-70-69-6C-65-72-2D-76-65-72-73-69-6F-6E-00-33-2E-37-2E-30-2D-64-65-76-00-6C-61-6E-67-75-... 
{
  compiler-version: 3.7.0-dev
  language: C#
  portability-policy: 0
  runtime-version: 4.8.4180.0
  optimization: debug
  checked: False
  nullable: Disable
  unsafe: False
  language-version: 8.0
}
3: 0x00000001 (Module)         Compilation Metadata References  6D-73-63-6F-72-6C-69-62-2E-64-6C-6C-00-00-01-D3-20-7D-5E-00-00-53-00-B5-26-EC-4B-C7-CB-15-47-84-... 
{
  -------------------------------------------------------------------------------------------------------
     FileName        Aliases  Flags     TimeStamp   FileSize    MVID                                  
  -------------------------------------------------------------------------------------------------------
  0: 'mscorlib.dll'  ''       Assembly  0x5E7D20D3  0x00530000  4bec26b5-cbc7-4715-8442-f1499e984732  
}
4: 0x33000001 (LocalVariable)  Dynamic Local Variables          FC-EF-7F-FF-FB-DF-FF-FE-F7-3F                                                                       
5: 0x34000001 (LocalConstant)  Dynamic Local Variables          02                                                                                                  
6: 0x33000002 (LocalVariable)  Dynamic Local Variables          FC-DD-DF-FD-DD-DF-FD-DD-DF-01                                                                       
7: 0x33000002 (LocalVariable)  Tuple Element Names              61-30-00-61-31-00-61-32-00-61-33-00-61-34-00-61-35-00-6E-30-00-6E-31-00-6E-32-00-6E-33-00-6E-34-... 
8: 0x34000002 (LocalConstant)  Dynamic Local Variables          0C                                                                                                  
9: 0x33000003 (LocalVariable)  Dynamic Local Variables          D2-01                                                                                               
a: 0x34000003 (LocalConstant)  Dynamic Local Variables          0A                                                                                                  
b: 0x33000004 (LocalVariable)  Dynamic Local Variables          D2-01                                                                                               
c: 0x33000004 (LocalVariable)  Tuple Element Names              61-31-00-61-37-00-61-38-00-00-00-00-61-34-00-00                                                     
d: 0x34000004 (LocalConstant)  Dynamic Local Variables          0A
»«
CustomDebugInformation (index: 0x37, size: 54): 
=============================================================================================================================================================
   Parent                      Kind                     Value                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
=============================================================================================================================================================
1: 0x06000001 (MethodDef)      EnC Local Slot Map       01-83-48-01-83-66-01-87-AA-01-88-08                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      
2: 0x34000001 (LocalConstant)  Dynamic Local Variables  02                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
3: 0x33000002 (LocalVariable)  Tuple Element Names      61-30-00-61-31-00-61-32-00-61-33-00-61-34-00-61-35-00-6E-30-00-6E-31-00-6E-32-00-6E-33-00-6E-34-...
4: 0x34000002 (LocalConstant)  Dynamic Local Variables  0C                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
5: 0x33000003 (LocalVariable)  Dynamic Local Variables  D2-01                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
6: 0x34000003 (LocalConstant)  Dynamic Local Variables  0A                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
7: 0x33000004 (LocalVariable)  Tuple Element Names      61-31-00-61-37-00-61-38-00-00-00-00-61-34-00-00                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          
8: 0x33000004 (LocalVariable)  Dynamic Local Variables  D2-01                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
9: 0x34000004 (LocalConstant)  Dynamic Local Variables  0A
»
");
        }

        [Fact]
        public void Convert_VB()
        {
            VerifyPortablePdb(
                TestResources.VB.DllAndPdb(portable: true),
                TestResources.VB.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 8): 
===================================================
   Name        Language      HashAlgorithm  Hash  
===================================================
1: 'C:\VB.vb'  Visual Basic  nil            nil   

MethodDebugInformation (index: 0x31, size: 144): 
=================================================
    IL   
=================================================
1: nil
2: nil
3: nil
4: nil
5: 
{
  Locals: 0x11000002 (StandAloneSig)
  Document: #1

  IL_0000: (42, 5) - (42, 17)
  IL_0001: (43, 9) - (43, 43)
  IL_0015: <hidden>
  IL_001B: (44, 17) - (44, 68)
  IL_0023: (45, 9) - (45, 13)
  IL_0024: <hidden>
  IL_0028: <hidden>
  IL_0030: <hidden>
  IL_0034: (47, 9) - (47, 43)
  IL_004A: <hidden>
  IL_0053: (48, 17) - (48, 54)
  IL_005B: (49, 9) - (49, 13)
  IL_005C: <hidden>
  IL_0062: <hidden>
  IL_006C: <hidden>
  IL_0070: (50, 5) - (50, 12)
}
6: nil
7: nil
8: nil
9: nil
a: nil
b: nil
c: nil
d: 
{
  Kickoff Method: 0x06000002 (MethodDef)
  Locals: 0x11000005 (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_0041: (11, 9) - (11, 59)
  IL_0042: (12, 17) - (12, 23)
  IL_004E: (13, 13) - (13, 24)
  IL_0058: (15, 13) - (15, 41)
  IL_006B: <hidden>
  IL_0080: (16, 17) - (16, 24)
  IL_00A0: (17, 17) - (17, 24)
  IL_00C0: (18, 13) - (18, 17)
  IL_00C1: <hidden>
  IL_00CF: <hidden>
  IL_00E0: <hidden>
  IL_00E3: (20, 13) - (20, 47)
  IL_0101: <hidden>
  IL_0116: (21, 17) - (21, 24)
  IL_0136: (22, 17) - (22, 24)
  IL_0156: (23, 13) - (23, 17)
  IL_0157: <hidden>
  IL_0165: <hidden>
  IL_0176: <hidden>
  IL_0179: (25, 9) - (25, 21)
}
e: nil
f: nil
10: nil
11: nil
12: nil
13: nil
14: 
{
  Document: #1

  IL_0000: (32, 13) - (32, 20)
  IL_0001: (36, 13) - (36, 20)
}
15: nil
16: nil
17: 
{
  Kickoff Method: 0x06000009 (MethodDef)
  Locals: «0x11000006»«0x11000008» (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_0021: (65, 9) - (65, 59)
  IL_0022: (66, 17) - (66, 35)
  IL_0029: (67, 13) - (67, 20)
  IL_0044: (68, 13) - (68, 35)
  IL_0050: (69, 9) - (69, 21)
}
18: nil
19: nil
1a: nil
1b: nil
1c: nil
1d: nil
1e: nil
1f: 
{
  Kickoff Method: 0x0600000a (MethodDef)
  Locals: «0x11000006»«0x11000008» (StandAloneSig)
  Document: #1

  IL_0000: <hidden>
  IL_0021: (71, 9) - (71, 59)
  IL_0022: (72, 17) - (72, 36)
  IL_0029: (73, 17) - (73, 36)
  IL_0030: (74, 17) - (74, 36)
  IL_0037: (75, 17) - (75, 36)
  IL_003E: (76, 13) - (76, 20)
  IL_0059: (77, 13) - (77, 36)
  IL_0065: (78, 13) - (78, 36)
  IL_0071: (79, 13) - (79, 36)
  IL_007D: (80, 13) - (80, 36)
  IL_0089: (81, 9) - (81, 21)
}
20: nil
21: nil
22: nil
23: nil
24: nil

LocalScope (index: 0x32, size: 192): 
===========================================================================================================================
   Method                  ImportScope               Variables              Constants              StartOffset  Length  
===========================================================================================================================
1: 0x06000002 (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       17      
2: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       113     
3: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  0x33000001-0x33000001  nil                    0x0017       17      
4: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  0x33000002-0x33000002  nil                    0x001B       8       
5: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  0x33000003-0x33000003  nil                    0x004C       22      
6: 0x06000005 (MethodDef)  0x35000002 (ImportScope)  0x33000004-0x33000004  nil                    0x0053       8       
7: 0x06000009 (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       17      
8: 0x0600000a (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       17      
9: 0x0600000d (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       379     
a: 0x06000014 (MethodDef)  0x35000002 (ImportScope)  nil                    0x34000001-0x34000003  0x0000       2       
b: 0x06000017 (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       82      
c: 0x0600001f (MethodDef)  0x35000002 (ImportScope)  nil                    nil                    0x0000       139     

LocalVariable (index: 0x33, size: 24): 
=======================================
   Name  Index  Attributes  
=======================================
1: 'x'   2      0        
2: 'a'   3      0        
3: 'x'   7      0        
4: 'a'   8      0        

LocalConstant (index: 0x34, size: 12): 
=======================================================================
   Name  Signature                                                      
=======================================================================
1: 'D1'  00-00-00-00-00-00-00-00-00-00-00-00-00 [0x0100000f (TypeRef)]  
2: 'D2'  02-7B-00-00-00-00-00-00-00-00-00-00-00 [0x0100000f (TypeRef)]  
3: 'DT'  00-80-30-05-6D-F3-D1-08 [0x01000010 (TypeRef)]                 

ImportScope (index: 0x35, size: 8): 
============================================================================================================================================================================
   Parent                    Imports                                                                                                                                         
============================================================================================================================================================================
1: nil (ImportScope)         <'prjlevel1' = 'http://NewNamespace'>, 'A1' = 'System.Collections.Generic', 'A2' = 0x01000014 (TypeRef), 'System.Threading'                     
2: 0x35000001 (ImportScope)  <'file1' = 'http://stuff/fromFile'>, <nil = 'http://stuff/fromFile1'>, 'AliasE' = 0x02000009 (TypeDef), 'System', 'System.Collections.Generic'  

CustomDebugInformation (index: 0x37, size: 78):
========================================================================================================================================================================
   Parent                      Kind                                Value                                                                                                                                                                    
========================================================================================================================================================================
1: 0x00000001 (Module)         Default Namespace                   nil                                                                                                                                                                      
2: 0x06000002 (MethodDef)      EnC Local Slot Map                  01-05-07-3F-09-3F-01-48-07-80-AF-09-80-AF-01-80-B8                                                                                                                       
3: 0x33000002 (LocalVariable)  Tuple Element Names                 78-00-00-7A-00                                                                                                                                                           
4: 0x33000004 (LocalVariable)  Tuple Element Names                 75-00-00                                                                                                                                                                 
5: 0x06000005 (MethodDef)      EnC Local Slot Map                  07-01-09-01-01-0A-01-35-02-01-07-80-82-09-80-82-01-80-8B-01-80-B6-02-80-82                                                                                               
6: 0x06000009 (MethodDef)      EnC Local Slot Map                  01-05                                                                                                                                                                    
7: 0x0600000a (MethodDef)      EnC Local Slot Map                  01-05-01-2A-01-4F-01-74                                                                                                                                                  
8: 0x0600000d (MethodDef)      EnC Local Slot Map                  15-00-1C-00-02-3F-02-80-AF                                                                                                                                               
9: 0x0600000d (MethodDef)      State Machine Hoisted Local Scopes  41-00-00-00-3A-01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-6D-00-00-00-62-00-00-00-...
a: 0x06000017 (MethodDef)      EnC Local Slot Map                  15-00-1C-00                                                                                                                                                              
b: 0x06000017 (MethodDef)      State Machine Hoisted Local Scopes  21-00-00-00-31-00-00-00                                                                                                                                                  
c: 0x0600001f (MethodDef)      EnC Local Slot Map                  15-00-1C-00                                                                                                                                                              
d: 0x0600001f (MethodDef)      State Machine Hoisted Local Scopes  21-00-00-00-6A-00-00-00-21-00-00-00-6A-00-00-00-21-00-00-00-6A-00-00-00-21-00-00-00-6A-00-00-00
");
        }

        [Fact]
        public void Convert_Misc()
        {
            VerifyPortablePdb(
                TestResources.Misc.DllAndPdb(portable: true),
                TestResources.Misc.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 8): 
========================================================================================================
   Name          Language  HashAlgorithm  Hash                                                         
========================================================================================================
1: 'C:\Misc.cs'  C#        SHA-1          DB-EB-2A-06-7B-2F-0E-0D-67-8A-00-2C-58-7A-28-06-05-6C-3D-CE  

MethodDebugInformation (index: 0x31, size: 8): 
===============================================
   IL  
===============================================
1: 
{
  Locals: 0x11000001 (StandAloneSig)
  Document: #1

  IL_0000: (13, 5) - (13, 6)
  IL_0001: (14, 9) - (14, 34)
  IL_0007: (15, 9) - (15, 28)
  IL_0013: (16, 9) - (16, 18)
  IL_001C: (17, 5) - (17, 6)
}
2: 
{
  Document: #1

  IL_0000: (9, 5) - (9, 30)
  IL_0007: (10, 5) - (10, 28)
}

LocalScope (index: 0x32, size: 32): 
===============================================================================================================
   Method                  ImportScope               Variables              Constants  StartOffset  Length  
===============================================================================================================
1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  0x33000001-0x33000001  nil        0x0000       30      
2: 0x06000002 (MethodDef)  0x35000002 (ImportScope)  nil                    nil        0x0000       23      

LocalVariable (index: 0x33, size: 6): 
======================================
   Name  Index  Attributes  
======================================
1: 'c'   0      0        

ImportScope (index: 0x35, size: 8): 
=================================================================================================
   Parent                    Imports                                                              
=================================================================================================
1: nil (ImportScope)         nil                                                                  
2: 0x35000001 (ImportScope)  'System', 'System.Collections.Generic', 'X' = 0x1b000001 (TypeSpec)  

CustomDebugInformation (index: 0x37, size: 6): 
============================================================
   Parent                  Kind                Value        
============================================================
1: 0x06000001 (MethodDef)  EnC Local Slot Map  01-10-16-01
");
        }

        [Fact]
        public void ConvertSourceServerToSourceLinkData_Empty()
        {
            string data =
@"SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRV: source files ---------------------------------------
SRCSRV: end ------------------------------------------------
";
            Assert.Null(PdbConverterWindowsToPortable.ConvertSourceServerToSourceLinkData(data));
        }

        [Fact]
        public void ConvertSourceServerToSourceLinkData_AllInvalid()
        {
            string data =
@"SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRV: source files ---------------------------------------
*****
SRCSRV: end ------------------------------------------------
";
            Assert.Null(PdbConverterWindowsToPortable.ConvertSourceServerToSourceLinkData(data));
        }

        [Fact]
        public void ConvertSourceServerToSourceLinkData_SingleValidMapping()
        {
            string data =
@"SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRV: source files ---------------------------------------
C:\a\b\X.cs*X.cs
*ignored*
a*
*a
*
C:\a\b\Y.cs*Y.cs
SRCSRV: end ------------------------------------------------
";
            AssertEx.AssertLinesEqual(@"
{
  ""documents"": {
     ""C:\a\b\*"": ""http://server/*""
  }
}",
                PdbConverterWindowsToPortable.ConvertSourceServerToSourceLinkData(data));
        }

        [Fact]
        public void ConvertSourceServerToSourceLinkData_MultiMapping1()
        {
            string data =
@"SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRV: source files ---------------------------------------
C:\a\b\X.cs*X.cs
C:\a\b\Y.cs*c/Y.cs
C:\a\b\U.cs*Z.cs
SRCSRV: end ------------------------------------------------
";
            AssertEx.AssertLinesEqual(@"
{
  ""documents"": {
     ""C:\a\b\Y.cs"": ""http://server/c/Y.cs"",
     ""C:\a\b\U.cs"": ""http://server/Z.cs"",
     ""C:\a\b\*"": ""http://server/*""
  }
}",
                PdbConverterWindowsToPortable.ConvertSourceServerToSourceLinkData(data));
        }

        [Fact]
        public void ConvertSourceServerToSourceLinkData_MultiMapping2()
        {
            string data =
@"SRCSRV: variables ------------------------------------------
RAWURL=http://server/%var2%
SRCSRV: source files ---------------------------------------
C:\a\b\X.cs*X1.cs
C:\a\b\Y.cs*Y1.cs
SRCSRV: end ------------------------------------------------
";
            AssertEx.AssertLinesEqual(@"
{
  ""documents"": {
     ""C:\a\b\X.cs"": ""http://server/X1.cs"",
     ""C:\a\b\Y.cs"": ""http://server/Y1.cs""
  }
}",
                PdbConverterWindowsToPortable.ConvertSourceServerToSourceLinkData(data));
        }

        [Fact]
        public void Convert_EmbeddedSource()
        {
            VerifyPortablePdb(
                TestResources.EmbeddedSource.DllAndPdb(portable: true),
                TestResources.EmbeddedSource.DllAndPdb(portable: false), @"
Document (index: 0x30, size: 32): 
==================================================================================================================================
   Name                                    Language  HashAlgorithm  Hash                                                         
==================================================================================================================================
1: 'C:\EmbeddedSourceSmall.cs'             C#        SHA-1          48-30-92-B9-4A-92-50-A7-75-33-E8-05-0D-2E-DD-CD-3F-58-9F-7F  
2: 'C:\EmbeddedSource.cs'                  C#        SHA-1          1D-BB-A3-11-47-F6-42-7D-82-99-B4-31-E9-32-7D-6B-09-C3-59-EB  
3: 'C:\EmbeddedSourceNoCode.cs'            C#        SHA-1          4B-4C-47-82-20-10-AC-63-A9-24-1E-33-CE-BA-74-76-40-F3-33-BB  
4: 'C:\EmbeddedSourceNoSequencePoints.cs'  C#        SHA-1          8B-74-C4-70-25-F1-77-43-94-81-81-6A-D1-80-BA-F4-57-12-58-5E  

MethodDebugInformation (index: 0x31, size: 8): 
===============================================
   IL   
===============================================
1: 
{
  Document: #1

  IL_0000: (4, 5) - (4, 19)
  IL_0007: (4, 20) - (4, 21)
  IL_0008: (4, 21) - (4, 22)
}
2: 
{
  Locals: 0x11000001 (StandAloneSig)
  Document: #2

  IL_0000: (10, 9) - (10, 10)
  IL_0001: (11, 13) - (11, 27)
  IL_0006: <hidden>
  IL_0009: (12, 13) - (12, 14)
  IL_000A: (13, 17) - (13, 70)
  IL_0015: (16, 13) - (16, 40)
  IL_001C: (17, 9) - (17, 10)
}

LocalScope (index: 0x32, size: 32): 
===================================================================================================
   Method                  ImportScope               Variables  Constants  StartOffset  Length  
===================================================================================================
1: 0x06000001 (MethodDef)  0x35000002 (ImportScope)  nil        nil        0x0000       9       
2: 0x06000002 (MethodDef)  0x35000004 (ImportScope)  nil        nil        0x0000       30      

ImportScope (index: 0x35, size: 16): 
======================================
   Parent                    Imports   
======================================
1: nil (ImportScope)         nil       
2: 0x35000001 (ImportScope)  nil       
3: 0x35000001 (ImportScope)  'System'  
4: 0x35000003 (ImportScope)  nil       

CustomDebugInformation (index: 0x37, size: 30): 
====================================================================================================================================================
   Parent                  Kind                Value                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           
====================================================================================================================================================
1: 0x30000001 (Document)   Embedded Source     00-00-00-00-2F-2F-20-73-68-6F-75-6C-64-20-62-65-20-6C-65-73-73-20-74-68-61-6E-20-63-6F-6D-70-72-...
2: 0x06000002 (MethodDef)  EnC Local Slot Map  02-10-16-01                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
3: 0x30000002 (Document)   Embedded Source     88-01-00-00-65-90-C1-6A-C3-30-0C-86-EF-81-BC-83-8E-31-3D-34-94-DD-BA-4B-19-3B-96-42-BB-17-70-1D-...
4: 0x30000003 (Document)   Embedded Source     00-00-00-00-2F-2F-20-66-69-6C-65-20-77-69-74-68-20-6E-6F-20-63-6F-64-65-0D-0A                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
5: 0x30000004 (Document)   Embedded Source     00-00-00-00-2F-2F-20-66-69-6C-65-20-77-69-74-68-20-6E-6F-20-73-65-71-75-65-6E-63-65-20-70-6F-69-...
");
        }
    }
}
