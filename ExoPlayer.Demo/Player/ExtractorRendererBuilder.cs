using Android.Content;
using Android.Media;
using Android.Net;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Extractor;
using Com.Google.Android.Exoplayer.Text;
using Com.Google.Android.Exoplayer.Upstream;

namespace Com.Google.Android.Exoplayer.Demo.Player
{
/**
 * A {@link RendererBuilder} for streams that can be read using an {@link Extractor}.
 */
    public class ExtractorRendererBuilder : DemoPlayer.RendererBuilder
    {

        private const int BUFFER_SEGMENT_SIZE = 64*1024;
        private const int BUFFER_SEGMENT_COUNT = 256;

        private readonly Context context;
        private readonly string userAgent;
        private readonly Uri uri;

        public ExtractorRendererBuilder(Context context, string userAgent, Uri uri)
        {
            this.context = context;
            this.userAgent = userAgent;
            this.uri = uri;
        }

        public void buildRenderers(DemoPlayer player)
        {
            IAllocator allocator = new DefaultAllocator(BUFFER_SEGMENT_SIZE);

            // Build the video and audio renderers.
            DefaultBandwidthMeter bandwidthMeter = new DefaultBandwidthMeter(player.GetMainHandler(), null);
            IDataSource dataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
            ExtractorSampleSource sampleSource = new ExtractorSampleSource(uri, dataSource, allocator,
                BUFFER_SEGMENT_COUNT*BUFFER_SEGMENT_SIZE);
            MediaCodecVideoTrackRenderer videoRenderer = new MediaCodecVideoTrackRenderer(context,
                sampleSource, (int) MediaCodec.VideoScalingModeScaleToFit, 5000, player.GetMainHandler(),
                player, 50);
            MediaCodecAudioTrackRenderer audioRenderer = new MediaCodecAudioTrackRenderer(sampleSource,
                null, true, player.GetMainHandler(), player, AudioCapabilities.GetCapabilities(context));
            TrackRenderer textRenderer = new TextTrackRenderer(sampleSource, player,
                player.GetMainHandler().Looper);

            // Invoke the callback.
            TrackRenderer[] renderers = new TrackRenderer[DemoPlayer.RENDERER_COUNT];
            renderers[DemoPlayer.TYPE_VIDEO] = videoRenderer;
            renderers[DemoPlayer.TYPE_AUDIO] = audioRenderer;
            renderers[DemoPlayer.TYPE_TEXT] = textRenderer;
            player.OnRenderers(renderers, bandwidthMeter);
        }

        public void cancel()
        {
            // Do nothing.
        }

    }
}