using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Swizzle
{
    public static class CollectionExtensions
    {
        static readonly AsyncLocal<Random> s_random = new();

        static Random Rng()
            => s_random.Value ??= new(unchecked(Environment.TickCount * 31 +
                Thread.CurrentThread.ManagedThreadId));

        public static IReadOnlyList<T> Shuffle<T>(
            this IEnumerable<T> enumerable)
        {
            var copy = enumerable.ToArray();
            var n = copy.Length;
            while (n > 1)
            {
                n--;
                var index = Rng().Next(n + 1);
                var value = copy[index];
                copy[index] = copy[n];
                copy[n] = value;
            }
            return copy;
        }

        public static T Random<T>(
            this IReadOnlyList<T> list)
            => list[Rng().Next(0, list.Count)];
    }
}
