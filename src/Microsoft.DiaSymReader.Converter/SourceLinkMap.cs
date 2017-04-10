// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed class SourceLinkMap
    {
        private readonly List<(FilePath key, Uri value)> _entries;

        public SourceLinkMap(List<(FilePath key, Uri value)> entries)
        {
            Debug.Assert(entries != null);
            _entries = entries;
        }

        internal struct FilePath
        {
            public readonly string Path;
            public readonly bool IsPrefix;

            public FilePath(string path, bool isPrefix)
            {
                Debug.Assert(path != null);

                Path = path;
                IsPrefix = isPrefix;
            }
        }

        internal struct Uri
        {
            public readonly string Prefix;
            public readonly string Suffix;

            public Uri(string prefix, string suffix)
            {
                Debug.Assert(prefix != null);
                Debug.Assert(suffix != null);

                Prefix = prefix;
                Suffix = suffix;
            }
        }

        internal static SourceLinkMap Parse(string json)
        {
            var list = new List<(FilePath key, Uri value)>();
            try
            {
                var root = JObject.Parse(json);
                foreach (var token in root["documents"])
                {
                    if (!(token is JProperty property))
                    {
                        // TODO: report error
                        // Bad source link format
                        continue;
                    }

                    string value;
                    try
                    {
                        value = property.Value.Value<string>();
                    }
                    catch (FormatException)
                    {
                        // TODO: report error
                        // Bad source link format
                        continue;
                    }

                    if (TryParseEntry(property.Name, value, out var path, out var uri))
                    {
                        list.Add((path, uri));
                    }
                    else
                    {
                        // TODO: report error
                        // Bad source link format
                        continue;
                    }
                }
            }
            catch (JsonReaderException)
            {
                // TODO: report error
                return null;
            }

            // Sort the map by decreasing file path length. This ensures that the most specific paths will checked before the least specific
            // and that absolute paths will be checked before a wildcard path with a matching base
            list.Sort((left, right) => -left.key.Path.Length.CompareTo(right.key.Path.Length));

            return new SourceLinkMap(list);
        }

        private static bool TryParseEntry(string key, string value, out FilePath path, out Uri uri)
        {
            path = default(FilePath);
            uri = default(Uri);

            // VALIDATION RULES
            // 1. The only acceptable wildcard is one and only one '*', which if present will be replaced by a relative path
            // 2. If the filepath does not contain a *, the uri cannot contain a * and if the filepath contains a * the uri must contain a *
            // 3. If the filepath contains a *, it must be the final character
            // 4. If the uri contains a *, it may be anwhere in the uri

            int filePathStar = key.IndexOf('*');
            if (filePathStar == key.Length - 1)
            {
                key = key.Substring(0, filePathStar);

                if (key.IndexOf('*') >= 0)
                {
                    return false;
                }
            }
            else if (filePathStar >= 0 || key.Length == 0)
            {
                return false;
            }

            string uriPrefix, uriSuffix;
            int uriStar = value.IndexOf('*');
            if (uriStar >= 0)
            {
                if (filePathStar < 0)
                {
                    return false;
                }

                uriPrefix = value.Substring(0, uriStar);
                uriSuffix = value.Substring(uriStar + 1);

                if (uriSuffix.IndexOf('*') >= 0)
                {
                    return false;
                }
            }
            else
            {
                uriPrefix = value;
                uriSuffix = "";
            }

            path = new FilePath(key, isPrefix: filePathStar >= 0);
            uri = new Uri(uriPrefix, uriSuffix);
            return true;
        }

        public string GetUri(string path)
        {
            if (path.IndexOf('*') >= 0)
            {
                return null;
            }

            // Note: the mapping function is case-insensitive.

            foreach (var (file, uri) in _entries)
            {
                if (file.IsPrefix)
                {
                    if (path.StartsWith(file.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        return uri.Prefix + path.Substring(file.Path.Length).Replace('\\', '/') + uri.Suffix;
                    }
                }
                else if (string.Equals(path, file.Path, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Assert(uri.Suffix.Length == 0);
                    return uri.Prefix;
                }
            }

            return null;
        }
    }
}
