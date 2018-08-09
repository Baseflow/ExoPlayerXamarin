using System.Collections.Generic;
using Com.Google.Android.Exoplayer2.Util;

namespace Com.Google.Android.Exoplayer2.CastDemo
{
    /**
 * Utility methods and constants for the Cast demo application.
 */
    /* package */
    internal class DemoUtil
    {

        public const string MIME_TYPE_DASH = MimeTypes.ApplicationMpd;
        public const string MIME_TYPE_HLS = MimeTypes.ApplicationM3u8;
        public const string MIME_TYPE_SS = MimeTypes.ApplicationSs;
        public const string MIME_TYPE_VIDEO_MP4 = MimeTypes.VideoMp4;
        public const string MIME_TYPE_AUDIO = MimeTypes.AudioAac;

        /**
         * The list of samples available in the cast demo app.
         */
        public static readonly List<Sample> SAMPLES = new List<Sample> {
            new Sample("https://storage.googleapis.com/wvmedia/clear/h264/tears/tears.mpd", "DASH (clear,MP4,H264)", MIME_TYPE_DASH),
            new Sample("https://commondatastorage.googleapis.com/gtv-videos-bucket/CastVideos/hls/TearsOfSteel.m3u8", "Tears of Steel (HLS)", MIME_TYPE_HLS),
            new Sample("https://html5demos.com/assets/dizzy.mp4", "Dizzy (MP4)", MIME_TYPE_VIDEO_MP4),
            new Sample("https://storage.googleapis.com/exoplayer-test-media-1/ogg/play.ogg", "Google Play (Ogg/Vorbis Audio)", MIME_TYPE_AUDIO)
        };

        /**
         * Represents a media sample.
         */
        public class Sample
        {

            /**
             * The uri from which the media sample is obtained.
             */
            public string uri;
            /**
             * A descriptive name for the sample.
             */
            public string name;
            /**
             * The mime type of the media sample, as required by {@link MediaInfo#setContentType}.
             */
            public string mimeType;

            /**
             * @param uri See {@link #uri}.
             * @param name See {@link #name}.
             * @param mimeType See {@link #mimeType}.
             */
            public Sample(string uri, string name, string mimeType)
            {
                this.uri = uri;
                this.name = name;
                this.mimeType = mimeType;
            }

            public override string ToString()
            {
                return name;
            }

        }

        private DemoUtil() { }

    }
}
