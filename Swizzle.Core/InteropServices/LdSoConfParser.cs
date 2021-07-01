using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Swizzle.InteropServices
{
    public sealed class LdSoConfParser
    {
        public static IEnumerable<string> ParseSystemDefault()
            => new LdSoConfParser().Parse();

        readonly string _ldsoconfPath;
        readonly DirectoryInfoWrapper _rootDirectory;

        public LdSoConfParser(
            string ldsoconfPath = "/etc/ld.so.conf",
            string? rootDirectory = "/")
        {
            _ldsoconfPath = ldsoconfPath;
            _rootDirectory = new DirectoryInfoWrapper(
                new DirectoryInfo(rootDirectory ?? "/"));
        }

        public IEnumerable<string> Parse()
        {
            foreach (var configPath in Parse(new(), _ldsoconfPath))
                yield return configPath;

            // The following MAY be "built-in" to ldconfig, as
            // "trusted directories", but any distro may simply strip them
            // out (like possibly Ubuntu) of their ldconfig build.
            yield return "/lib";
            yield return "/lib64";
            yield return "/usr/lib";
            yield return "/usr/lib64";
        }

        // note: this was checked against ldconfig.c from glibc, and
        // verified on openSUSE 15.1 and Ubuntu 20.04 with:
        //   /sbin/ldconfig -vN 2>/dev/null | grep '^/'
        IEnumerable<string> Parse(
            HashSet<string> visitedConfigFilePaths,
            string configFilePath)
        {
            string ReRootPath(string path)
            {
                if (Path.GetPathRoot(path) is string root)
                    path = path[root.Length..];

                return Path.Combine(_rootDirectory.FullName, path);
            }

            configFilePath = PathHelpers.ResolveFullPath(
                ReRootPath(configFilePath));

            if (!visitedConfigFilePaths.Add(configFilePath))
                yield break;

            foreach (var rawLine in File.ReadLines(
                configFilePath,
                Encoding.UTF8))
            {
                // Ignore empty lines and comment-only lines
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                // Strip trailing comments
                var ofs = line.IndexOf('#');
                if (ofs >= 0)
                {
                    line = line[..ofs].TrimEnd();
                    if (line.Length == 0) // Should not happen
                        continue;
                }

                // Path lines always start with /
                if (line[0] == '/')
                    yield return line;

                // Recurse into include paths/globs
                if (line.Length > 8 &&
                    line.StartsWith("include", StringComparison.Ordinal) &&
                    char.IsWhiteSpace(line[7]))
                {
                    var matcher = new Matcher();
                    matcher.AddInclude(line[7..].Trim());

                    // globs are sorted by name; important for precedence
                    foreach (var match in matcher
                        .Execute(_rootDirectory)
                        .Files
                        .OrderBy(f => f.Path))
                    {
                        foreach (var libPath in Parse(
                            visitedConfigFilePaths,
                            match.Path))
                            yield return libPath;
                    }
                }

                // Ignore everything else
            }
        }
    }
}
