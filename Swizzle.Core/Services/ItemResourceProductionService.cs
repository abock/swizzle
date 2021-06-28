using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Swizzle.Models;

namespace Swizzle.Services
{
    public static class ItemResourceProductionService
    {
        static readonly Dictionary<
            ItemResourceKind,
            List<(ItemResourceKind Kind, Action<string, string> Converter)>> s_map = new()
        {
            [ItemResourceKind.Gif] = new()
            {
                (ItemResourceKind.Jpeg, FfmpegThumbnail),
                (ItemResourceKind.Mp4, FfmpegTranscode),
                (ItemResourceKind.Ogv, FfmpegTranscode),
            },
            [ItemResourceKind.Mp4] = new()
            {
                (ItemResourceKind.Jpeg, FfmpegThumbnail),
                (ItemResourceKind.Gif, FfmpegTranscode),
                (ItemResourceKind.Ogv, FfmpegTranscode)
            },
            [ItemResourceKind.Ogv] = new()
            {
                (ItemResourceKind.Jpeg, FfmpegThumbnail),
                (ItemResourceKind.Gif, FfmpegTranscode),
                (ItemResourceKind.Mp4, FfmpegTranscode)
            }
        };

        public static bool ConvertItemResource(
            ItemResourceKind fromKind,
            string fromPath,
            ItemResourceKind toKind,
            string toPath,
            CancellationToken cancellationToken)
        {
            if (!s_map.TryGetValue(fromKind, out var viableToKinds))
                return false;

            foreach (var viableToKind in viableToKinds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (viableToKind.Kind == toKind)
                {
                    viableToKind.Converter(fromPath, toPath);
                    return true;
                }
            }

            return false;
        }

        static void FfmpegThumbnail(string fromPath, string toPath)
            => FfmpegConvert(
                fromPath,
                toPath,
                "-vframes", "1",
                "-vsync", "vfr",
                "-an");

        static void FfmpegTranscode(string fromPath, string toPath)
            => FfmpegConvert(
                fromPath,
                toPath,
                "-movflags", "faststart",
                "-pix_fmt", "yuv420p",
                "-vf", "\"scale=trunc(iw/2)*2:trunc(ih/2)*2\"");

        static void FfmpegConvert(
            string fromPath,
            string toPath,
            params string[] options)
        {
            static string Quote(string p)
                => $"\"{p.Replace("\"", "\\\"")}\"";

            var proc = Process.Start(
                "ffmpeg",
                "-i " + Quote(fromPath) + " " +
                string.Join(" ", options) + " " +
                Quote(toPath));

            proc.WaitForExit();

            if (proc.ExitCode != 0)
            {
                try
                {
                    File.Delete(toPath);
                }
                catch
                {
                }

                throw new Exception(
                    $"Unable to convert {fromPath} to {toPath}");
            }
        }
    }
}
