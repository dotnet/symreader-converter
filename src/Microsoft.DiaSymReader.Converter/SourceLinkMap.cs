﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Microsoft.DiaSymReader.Tools
{
    internal sealed class SourceLinkMap
    {
        private readonly List<(FilePathPattern key, UriPattern value)> _entries;

        private SourceLinkMap(List<(FilePathPattern key, UriPattern value)> entries)
        {
            _entries = entries;
        }

        private readonly struct FilePathPattern
        {
            public readonly string Path;
            public readonly bool IsPrefix;

            public FilePathPattern(string path, bool isPrefix)
            {
                Path = path;
                IsPrefix = isPrefix;
            }
        }

        private readonly struct UriPattern
        {
            public readonly string Prefix;
            public readonly string Suffix;

            public UriPattern(string prefix, string suffix)
            {
                Prefix = prefix;
                Suffix = suffix;
            }
        }

        /// <summary>
        /// Parses Source Link JSON string.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
        /// <exception cref="InvalidDataException">The JSON does not follow Source Link specification.</exception>
        /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON string.</exception>
        public static SourceLinkMap Parse(string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var list = new List<(FilePathPattern key, UriPattern value)>();

            var root = JsonDocument.Parse(json, new JsonDocumentOptions() { AllowTrailingCommas = true }).RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException();
            }

            foreach (var rootEntry in root.EnumerateObject())
            {
                if (!rootEntry.NameEquals("documents"))
                {
                    // potential future extensibility
                    continue;
                }

                if (rootEntry.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException();
                }

                foreach (var documentsEntry in rootEntry.Value.EnumerateObject())
                {
                    if (documentsEntry.Value.ValueKind != JsonValueKind.String ||
                        !TryParseEntry(documentsEntry.Name, documentsEntry.Value.GetString(), out var path, out var uri))
                    {
                        throw new InvalidDataException();
                    }

                    list.Add((path, uri));
                }
            }

            // Sort the map by decreasing file path length. This ensures that the most specific paths will checked before the least specific
            // and that absolute paths will be checked before a wildcard path with a matching base
            list.Sort((left, right) => -left.key.Path.Length.CompareTo(right.key.Path.Length));

            return new SourceLinkMap(list);
        }

        private static bool TryParseEntry(string key, string value, out FilePathPattern path, out UriPattern uri)
        {
            path = default;
            uri = default;

            // VALIDATION RULES
            // 1. The only acceptable wildcard is one and only one '*', which if present will be replaced by a relative path
            // 2. If the filepath does not contain a *, the uri cannot contain a * and if the filepath contains a * the uri must contain a *
            // 3. If the filepath contains a *, it must be the final character
            // 4. If the uri contains a *, it may be anywhere in the uri

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

            path = new FilePathPattern(key, isPrefix: filePathStar >= 0);
            uri = new UriPattern(uriPrefix, uriSuffix);
            return true;
        }

        public string? GetUri(string path)
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
                        var escapedPath = string.Join("/", path.Substring(file.Path.Length).Split(new[] { '/', '\\' }).Select(Uri.EscapeDataString));
                        return uri.Prefix + escapedPath + uri.Suffix;
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
