// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Adapted from https://github.com/xamarin/mirepoix/blob/master/src/Xamarin.Helpers/PathHelpers.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Swizzle
{
    /// <summary>
    /// Various utilities that should be provided via System.IO.Path.
    /// </summary>
    public static partial class PathHelpers
    {
        [DllImport("libc")]
        static extern IntPtr realpath(IntPtr path, IntPtr resolvedName);

        [DllImport("libc")]
        static extern void free(IntPtr ptr);

        static volatile bool s_haveRealpath = Environment.OSVersion.Platform == PlatformID.Unix;

        /// <summary>
        /// A more exhaustive version of <see cref="System.IO.Path.GetFullPath(string)"/>
        /// that additionally resolves symlinks via `realpath` on Unix systems. The path
        /// returned is also trimmed of any trailing directory separator characters.
        /// </summary>
        /// <param name="pathComponents">
        /// The path components to resolve to a full path. If multiple path compnents are
        /// specified, they are combined with <see cref="Path.Combine"/> before resolving.
        /// </param>
        public static string ResolveFullPath(params string[] pathComponents)
        {
            if (pathComponents is null || pathComponents.Length == 0)
                throw new ArgumentException(
                    "must have at least one path component",
                    nameof(pathComponents));

            var path = pathComponents.Length == 1
                ? pathComponents[0]
                : Path.Combine(pathComponents);

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException(
                    "invalid path (must not be empty)",
                    nameof(pathComponents));

            var fullPath = Path.GetFullPath(NormalizePath(path));

            if (s_haveRealpath)
            {
                var fullPathPtr = Marshal.StringToCoTaskMemUTF8(fullPath);
                try
                {
                    // Path.GetFullPath expands the path, but on Unix systems
                    // does not resolve symlinks. Always attempt to resolve
                    // symlinks via realpath.
                    var realPathPtr = realpath(fullPathPtr, IntPtr.Zero);
                    if (realPathPtr != IntPtr.Zero)
                    {
                        try
                        {
                            var realPath = Marshal.PtrToStringUTF8(realPathPtr);
                            if (!string.IsNullOrEmpty(realPath))
                                fullPath = realPath;
                        }
                        finally
                        {
                            free(realPathPtr);
                        }
                    }
                }
                catch
                {
                    s_haveRealpath = false;
                }
                finally
                {
                    Marshal.ZeroFreeCoTaskMemUTF8(fullPathPtr);
                }
            }

            if (fullPath.Length == 0)
                throw new ArgumentException(
                    "invalid path (resolves to empty)",
                    nameof(pathComponents));

            if (fullPath.Length > 1 && fullPath[^1] == Path.DirectorySeparatorChar)
                return fullPath.TrimEnd(Path.DirectorySeparatorChar);

            if (fullPath.Length > 1 && fullPath[^1] == Path.AltDirectorySeparatorChar)
                return fullPath.TrimEnd(Path.AltDirectorySeparatorChar);

            return fullPath;
        }

        /// <summary>
        /// Returns a path with / and \ directory separators
        /// normalized to <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        public static string NormalizePath(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            return path
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Locates the path of a program in the system, first searching in <paramref name="preferPaths"/>
        /// if specified, then falling back to the paths in `PATH` environment variable. The first search
        /// path to yield a match for <paramref name="programName"/> will be used to resolve the absolute
        /// path of the program.
        /// </summary>
        /// <remarks>
        /// The functionality is analogous to the `which` command on Unix systems, and works on Windows
        /// as well. On Windows, `PATHEXT` is also respected. On Unix, files ending with `.exe` will
        /// also be returned if found - that is, one should not specify an extension at all in
        /// <paramref name="programName"/>. Finally, <paramref name="programName"/> comparison is _case
        /// insensitive_! For example, `msbuild` may yield `/path/to/MSBuild.exe`.
        /// </remarks>
        /// <param name="programName">
        /// The name of the program to find. Do not specify a file extension. See remarks.
        /// </param>
        /// <param name="preferPaths">
        /// Paths to search in preference over the `PATH` environment variable.
        /// </param>
        public static string FindProgramPath(
            string programName,
            IEnumerable<string>? preferPaths = null)
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            var extensions = new List<string>(Environment
                .GetEnvironmentVariable("PATHEXT")
                ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    ?? Array.Empty<string>());

            if (extensions.Count == 0)
                extensions.Add(".exe");

            extensions.Insert(0, string.Empty);

            var preferPathsArray = preferPaths?.ToArray()
                ?? Array.Empty<string>();

            var searchPaths = preferPathsArray.Concat(Environment
                .GetEnvironmentVariable("PATH")
                ?.Split(isWindows ? ';' : ':') ?? Array.Empty<string>());

            var filesToCheck = searchPaths
                .Where(Directory.Exists)
                .Select(p => new DirectoryInfo(p))
                .SelectMany(p => p.EnumerateFiles());

            foreach (var file in filesToCheck)
            {
                foreach (var extension in extensions)
                {
                    if (string.Equals(
                        file.Name,
                        programName + extension,
                        StringComparison.OrdinalIgnoreCase))
                        return ResolveFullPath(file.FullName);
                }
            }

            string? flattenedPreferPaths = null;
            var message = "Unable to find '{0}' in ";
            if (preferPathsArray.Length > 0)
            {
                message += "specified PreferPaths ({1}) nor PATH";
                flattenedPreferPaths = string.Join(
                    ", ", preferPathsArray.Select(p => $"'{p}'"));
            }
            else
            {
                message += "PATH";
            }

            throw new FileNotFoundException(message);
        }
    }
}
