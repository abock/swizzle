// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Adapted from https://github.com/xamarin/mirepoix/blob/master/src/Xamarin.Helpers/PathHelpers.cs

using System;
using System.IO;
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
    }
}
