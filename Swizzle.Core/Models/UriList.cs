using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Swizzle.Models
{
    public sealed class UriList : IReadOnlyList<string>
    {
        static readonly Regex s_skipRegex = new(
            @"(^\s*#)|(^\s*$)",
            RegexOptions.Compiled);

        readonly ImmutableList<string> _lines;

        UriList(ImmutableList<string> lines)
            => _lines = lines;

        public string this[int index] => _lines[index];
        public int Count => _lines.Count;

        public IEnumerator<string> GetEnumerator()
            => _lines.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _lines.GetEnumerator();

        public IEnumerable<string> Uris => _lines
            .Where(line => !s_skipRegex.IsMatch(line))
            .Select(line => line.Trim());

        public static UriList FromFile(string filePath)
            => new(ImmutableList.CreateRange<string>(
                File.ReadLines(filePath)));
    }
}
