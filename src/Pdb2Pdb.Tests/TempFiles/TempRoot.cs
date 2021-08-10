// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class TempRoot : IDisposable
    {
        private readonly List<IDisposable> _temps = new List<IDisposable>();
        public static readonly string Root;

        static TempRoot()
        {
            Root = Path.Combine(Path.GetTempPath(), "RoslynTests");
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            if (_temps != null)
            {
                DisposeAll(_temps);
                _temps.Clear();
            }
        }

        private static void DisposeAll(IEnumerable<IDisposable> temps)
        {
            foreach (var temp in temps)
            {
                try
                {
                    if (temp != null)
                    {
                        temp.Dispose();
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        public TempDirectory CreateDirectory()
        {
            var dir = new DisposableDirectory(this);
            _temps.Add(dir);
            return dir;
        }

        public TempFile CreateFile(string? prefix = null, string? extension = null, string? directory = null, [CallerFilePath]string callerSourcePath = null, [CallerLineNumber]int callerLineNumber = 0)
        {
            return AddFile(new DisposableFile(prefix, extension, directory, callerSourcePath, callerLineNumber));
        }

        public DisposableFile AddFile(DisposableFile file)
        {
            _temps.Add(file);
            return file;
        }

        internal static void CreateStream(string fullPath)
        {
            using (var file = new FileStream(fullPath, FileMode.CreateNew)) { }
        }
    }
}
