using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Swizzle.InteropServices
{
    static class NativeLibraryLoader
    {
        static readonly string[] s_librarySearchPaths;
        static readonly string[] s_libraryPrefixes;
        static readonly string[] s_libraryExtensions;
        static readonly bool s_isLinux;
        static readonly bool s_isMac;

        static NativeLibraryLoader()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                s_isLinux = true;
                s_librarySearchPaths = LdSoConfParser
                    .ParseSystemDefault()
                    .ToArray();
                s_libraryPrefixes = new[] { "", "lib" };
                s_libraryExtensions = new[] { ".so" };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                s_isMac = true;
                s_librarySearchPaths = new[]
                {
                    "/usr/local/lib",
                    "/usr/lib",
                };
                s_libraryPrefixes = new[] { "", "lib" };
                s_libraryExtensions = new[] { "", ".dylib", ".so" };
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "no idea how to use ffmpeg on this platform");
            }
        }

        static IEnumerable<string> ResolveAllPaths(
            string libraryName,
            params int[] libraryVersions)
        {
            foreach (var searchPath in s_librarySearchPaths)
            {
                foreach (var prefix in s_libraryPrefixes)
                {
                    foreach (var extension in s_libraryExtensions)
                    {
                        var prefixedFileName = prefix + libraryName;

                        foreach (var version in libraryVersions)
                        {
                            if (s_isLinux)
                                yield return Path.Combine(
                                    searchPath,
                                    $"{prefixedFileName}{extension}.{version}");
                            else if (s_isMac)
                                yield return Path.Combine(
                                    searchPath,
                                    $"{prefixedFileName}.{version}{extension}");
                        }

                        yield return Path.Combine(
                            searchPath,
                            prefix + libraryName + extension);
                    }
                }
            }
        }

        public static IntPtr LoadLibrary(
            string libraryName,
            params int[] libraryVersions)
        {
            foreach (var path in ResolveAllPaths(libraryName, libraryVersions))
            {
                if (File.Exists(path) &&
                    NativeLibrary.TryLoad(path, out var handle))
                    return handle;
            }

            throw new DllNotFoundException(
                $"Unable to load '{libraryName}' at any of these paths: " +
                string.Join(", ", ResolveAllPaths(libraryName)
                    .Select(p => $"'{p}'")));
        }
    }
}
