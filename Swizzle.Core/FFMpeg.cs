using System;
using System.Runtime.InteropServices;

using Swizzle.InteropServices;

#pragma warning disable IDE1006
#pragma warning disable CA2101

namespace Swizzle
{
    public static class FFMpeg
    {
        public readonly struct VideoStreamMetadata
        {
            public readonly int Width;
            public readonly int Height;
            public readonly TimeSpan Duration;

            public VideoStreamMetadata(
                int width,
                int height,
                TimeSpan duration)
            {
                Width = width;
                Height = height;
                Duration = duration;
            }
        }

        const int AVFormatContext_field_offset__nb_streams = 44;
        const int AVFormatContext_field_offset__streams = 48;
        const int AVFormatContext_field_offset__duration = 1096;
        const int AVStream_field_offset__codecpar = 208;
        const int AVCodecParameters_field_offset__codec_type = 0;
        const int AVCodecParameters_field_offset__codec_id = 4;
        const int AVCodecParameters_field_offset__width = 56;
        const int AVCodecParameters_field_offset__height = 60;

        enum AVMediaType
        {
            UNKNOWN = -1,  ///< Usually treated as AVMEDIA_TYPE_DATA
            VIDEO,
            AUDIO,
            DATA,          ///< Opaque data information usually continuous
            SUBTITLE,
            ATTACHMENT,    ///< Opaque data information usually sparse
            NB
        }

        enum AVCodecID
        {
            NONE = 0,
            MJPEG = 7,
            H264 = 27,
            THEORA = 30,
            PNG = 61,
            GIF = 97
        }

        delegate IntPtr /* AVFormatContext* */ avformat_alloc_context_fn();
        static readonly avformat_alloc_context_fn? avformat_alloc_context;

        delegate void avformat_free_context_fn(
            IntPtr /* AVFormatContext* */ context);
        static readonly avformat_free_context_fn? avformat_free_context;

        delegate int avformat_open_input_fn(
            in IntPtr /* AVFormatContext** */ ps,
            IntPtr /* const char* */ url,
            IntPtr /* const AVInputFormat* */ fmt,
            IntPtr /* AVDictionary** */ options);
        static readonly avformat_open_input_fn? avformat_open_input;

        delegate int avformat_close_input_fn(
            in IntPtr /* AVFormatContext** */ s);
        static readonly avformat_close_input_fn? avformat_close_input;

        delegate int avformat_find_stream_info_fn(
            IntPtr /* AVFormatContext* */ ic,
            IntPtr /* AVDictionary** */ options);
        static readonly avformat_find_stream_info_fn? avformat_find_stream_info;

        static FFMpeg()
        {
            IntPtr handle;
            try
            {
                handle = NativeLibraryLoader.LoadLibrary("avformat");
            }
            catch (DllNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Error.WriteLine(e);
                Console.ResetColor();
                return;
            }

            T Dlsym<T>(string symbol)
                => Marshal.GetDelegateForFunctionPointer<T>(
                    NativeLibrary.GetExport(handle, symbol));

            avformat_alloc_context = Dlsym<avformat_alloc_context_fn>(
                nameof(avformat_alloc_context));

            avformat_free_context = Dlsym<avformat_free_context_fn>(
                nameof(avformat_free_context));

            avformat_open_input = Dlsym<avformat_open_input_fn>(
                nameof(avformat_open_input));

            avformat_close_input = Dlsym<avformat_close_input_fn>(
                nameof(avformat_close_input));

            avformat_find_stream_info = Dlsym<avformat_find_stream_info_fn>(
                nameof(avformat_find_stream_info));
        }

        static bool swizzle_ffmpeg_read_metadata(
            string filename,
            out int width,
            out int height,
            out TimeSpan duration)
        {
            width = 0;
            height = 0;
            duration = TimeSpan.Zero;

            if (avformat_alloc_context is null ||
                avformat_open_input is null ||
                avformat_close_input is null ||
                avformat_find_stream_info is null ||
                avformat_free_context is null)
                return false;

            var codecId = AVCodecID.NONE;

            var filenameUtf8 = Marshal.StringToCoTaskMemUTF8(filename);
            if (filenameUtf8 == IntPtr.Zero)
                return false;

            var formatContext = avformat_alloc_context();
            var fileOpened = false;

            try
            {
                if (avformat_open_input(
                    in formatContext,
                    filenameUtf8,
                    IntPtr.Zero,
                    IntPtr.Zero) != 0)
                    return false;

                fileOpened = true;

                if (avformat_find_stream_info(formatContext, IntPtr.Zero) < 0)
                    return false;

                var streamCount = Marshal.ReadInt32(
                    formatContext,
                    AVFormatContext_field_offset__nb_streams);

                if (streamCount <= 0)
                    return false;

                var streams = Marshal.ReadIntPtr(
                    formatContext,
                    AVFormatContext_field_offset__streams);

                for (var i = 0; i < streamCount; i++)
                {
                    var stream = Marshal.ReadIntPtr(
                        streams,
                        IntPtr.Size * i);

                    if (stream == IntPtr.Zero)
                        continue;

                    var codecParameters = Marshal.ReadIntPtr(
                        stream,
                        AVStream_field_offset__codecpar);

                    if (codecParameters == IntPtr.Zero)
                        continue;

                    var codecType = (AVMediaType)Marshal.ReadInt32(
                        codecParameters,
                        AVCodecParameters_field_offset__codec_type);

                    codecId = (AVCodecID)Marshal.ReadInt32(
                        codecParameters,
                        AVCodecParameters_field_offset__codec_id);

                    if (codecType != AVMediaType.VIDEO)
                        continue;

                    width = Marshal.ReadInt32(
                        codecParameters,
                        AVCodecParameters_field_offset__width);

                    height = Marshal.ReadInt32(
                        codecParameters,
                        AVCodecParameters_field_offset__height);

                    break;
                }

                if (codecId == AVCodecID.NONE)
                    return false;

                if (codecId is not (AVCodecID.MJPEG or AVCodecID.PNG))
                {
                    // Durations in ffmpeg are in microseconds
                    var ticks = Marshal.ReadInt64(
                        formatContext,
                        AVFormatContext_field_offset__duration) * 10;

                    // Truncate to 10ms resolution; ffmpeg does this in
                    // ffprobe, which is what we'd get if we parsed console
                    // output from ffprobe...
                    var roundedTicks = ticks - (ticks % 100000);

                    duration = TimeSpan.FromTicks(roundedTicks);
                }

                return true;
            }
            finally
            {
                if (fileOpened)
                    avformat_close_input(in formatContext);

                if (filenameUtf8 != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(filenameUtf8);

                if (formatContext != IntPtr.Zero)
                    avformat_free_context(formatContext);
            }
        }

        public static bool TryGetVideoStreamMetadata(
            string fileName,
            out VideoStreamMetadata streamMetadata)
        {
            if (swizzle_ffmpeg_read_metadata(
                fileName,
                out var width,
                out var height,
                out var duration))
            {
                streamMetadata = new VideoStreamMetadata(
                    width,
                    height,
                    duration);
                return true;
            }

            streamMetadata = default;
            return false;
        }
    }
}
