using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Chunk;
using Com.Google.Android.Exoplayer.Drm;
using Com.Google.Android.Exoplayer.Smoothstreaming;
using Com.Google.Android.Exoplayer.Text;
using Com.Google.Android.Exoplayer.Upstream;
using Com.Google.Android.Exoplayer.Util;
using Java.IO;
using Java.Lang;

namespace Com.Google.Android.Exoplayer.Demo.Player
{
/**
 * A {@link RendererBuilder} for SmoothStreaming.
 */

	public class SmoothStreamingRendererBuilder : DemoPlayer.RendererBuilder
	{

		private const int BUFFER_SEGMENT_SIZE = 64*1024;
		private const int VIDEO_BUFFER_SEGMENTS = 200;
		private const int AUDIO_BUFFER_SEGMENTS = 54;
		private const int TEXT_BUFFER_SEGMENTS = 2;
		private const int LIVE_EDGE_LATENCY_MS = 30000;

		private readonly Context context;
		private readonly string userAgent;
		private readonly string url;
		private readonly IMediaDrmCallback drmCallback;

		private AsyncRendererBuilder currentAsyncBuilder;

		public SmoothStreamingRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback)
		{
			this.context = context;
			this.userAgent = userAgent;
			this.url = Util.Util.ToLowerInvariant(url).EndsWith("/manifest") ? url : url + "/Manifest";
			this.drmCallback = drmCallback;
		}

		public void buildRenderers(DemoPlayer player)
		{
			currentAsyncBuilder = new AsyncRendererBuilder(context, userAgent, url, drmCallback, player);
			currentAsyncBuilder.init();
		}

		public void cancel()
		{
			if (currentAsyncBuilder != null)
			{
				currentAsyncBuilder.cancel();
				currentAsyncBuilder = null;
			}
		}

		private class AsyncRendererBuilder : Object, ManifestFetcher.IManifestCallback
		{

			private readonly Context context;
			private readonly string userAgent;
			private readonly IMediaDrmCallback drmCallback;
			private readonly DemoPlayer player;
			private readonly ManifestFetcher manifestFetcher;

			private bool canceled;

			public AsyncRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback, DemoPlayer player)
			{
				this.context = context;
				this.userAgent = userAgent;
				this.drmCallback = drmCallback;
				this.player = player;
				SmoothStreamingManifestParser parser = new SmoothStreamingManifestParser();
				manifestFetcher = new ManifestFetcher(url, new DefaultHttpDataSource(userAgent, null), parser);
			}

			public void init()
			{
				manifestFetcher.SingleLoad(player.GetMainHandler().Looper, this);
			}

			public void cancel()
			{
				canceled = true;
			}

			public void OnSingleManifestError(IOException exception)
			{
				if (canceled)
				{
					return;
				}

				player.OnRenderersError(exception);
			}

			public void OnSingleManifest(Object obj)
			{
				var manifest = obj.JavaCast<SmoothStreamingManifest>();
				if (canceled)
				{
					return;
				}

				Handler mainHandler = player.GetMainHandler();
				ILoadControl loadControl = new DefaultLoadControl(new DefaultAllocator(BUFFER_SEGMENT_SIZE));
				DefaultBandwidthMeter bandwidthMeter = new DefaultBandwidthMeter(mainHandler, player);

				// Check drm support if necessary.
				IDrmSessionManager drmSessionManager = null;
				if (manifest.ProtectionElement != null)
				{
					if (Util.Util.SdkInt < 18)
					{
						player.OnRenderersError(
							new UnsupportedDrmException(UnsupportedDrmException.ReasonUnsupportedScheme));
						return;
					}
					try
					{
						drmSessionManager = new StreamingDrmSessionManager(manifest.ProtectionElement.Uuid,
							player.GetPlaybackLooper(), drmCallback, null, player.GetMainHandler(), player);
					}
					catch (Exception e)
					{
						player.OnRenderersError(e);
						return;
					}
				}

				// Build the video renderer.
				IDataSource videoDataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				IChunkSource videoChunkSource = new SmoothStreamingChunkSource(manifestFetcher,
					new DefaultSmoothStreamingTrackSelector(context, SmoothStreamingManifest.StreamElement.TypeVideo),
					videoDataSource, new FormatEvaluatorAdaptiveEvaluator(bandwidthMeter), LIVE_EDGE_LATENCY_MS);
				ChunkSampleSource videoSampleSource = new ChunkSampleSource(videoChunkSource, loadControl,
					VIDEO_BUFFER_SEGMENTS*BUFFER_SEGMENT_SIZE, mainHandler, player,
					DemoPlayer.TYPE_VIDEO);
				TrackRenderer videoRenderer = new MediaCodecVideoTrackRenderer(context, videoSampleSource,
					(int) MediaCodec.VideoScalingModeScaleToFit, 5000, drmSessionManager, true, mainHandler,
					player, 50);

				// Build the audio renderer.
				IDataSource audioDataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				IChunkSource audioChunkSource = new SmoothStreamingChunkSource(manifestFetcher,
					new DefaultSmoothStreamingTrackSelector(context, SmoothStreamingManifest.StreamElement.TypeAudio),
					audioDataSource, null, LIVE_EDGE_LATENCY_MS);
				ChunkSampleSource audioSampleSource = new ChunkSampleSource(audioChunkSource, loadControl,
					AUDIO_BUFFER_SEGMENTS*BUFFER_SEGMENT_SIZE, mainHandler, player,
					DemoPlayer.TYPE_AUDIO);
				TrackRenderer audioRenderer = new MediaCodecAudioTrackRenderer(audioSampleSource,
					drmSessionManager, true, mainHandler, player, AudioCapabilities.GetCapabilities(context));

				// Build the text renderer.
				IDataSource textDataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				IChunkSource textChunkSource = new SmoothStreamingChunkSource(manifestFetcher,
					new DefaultSmoothStreamingTrackSelector(context, SmoothStreamingManifest.StreamElement.TypeText),
					textDataSource, null, LIVE_EDGE_LATENCY_MS);
				ChunkSampleSource textSampleSource = new ChunkSampleSource(textChunkSource, loadControl,
					TEXT_BUFFER_SEGMENTS*BUFFER_SEGMENT_SIZE, mainHandler, player,
					DemoPlayer.TYPE_TEXT);
				TrackRenderer textRenderer = new TextTrackRenderer(textSampleSource, player,
					mainHandler.Looper);

				// Invoke the callback.
				TrackRenderer[] renderers = new TrackRenderer[DemoPlayer.RENDERER_COUNT];
				renderers[DemoPlayer.TYPE_VIDEO] = videoRenderer;
				renderers[DemoPlayer.TYPE_AUDIO] = audioRenderer;
				renderers[DemoPlayer.TYPE_TEXT] = textRenderer;
				player.OnRenderers(renderers, bandwidthMeter);
			}
		}
	}
}