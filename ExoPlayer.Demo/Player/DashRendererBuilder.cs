using System.Linq;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Chunk;
using Com.Google.Android.Exoplayer.Dash;
using Com.Google.Android.Exoplayer.Dash.Mpd;
using Com.Google.Android.Exoplayer.Drm;
using Com.Google.Android.Exoplayer.Text;
using Com.Google.Android.Exoplayer.Upstream;
using Com.Google.Android.Exoplayer.Util;
using Java.IO;
using Java.Lang;

namespace Com.Google.Android.Exoplayer.Demo.Player
{
/**
 * A {@link RendererBuilder} for DASH.
 */

	public class DashRendererBuilder : DemoPlayer.RendererBuilder
	{

		private const string TAG = "DashRendererBuilder";

		private const int BUFFER_SEGMENT_SIZE = 64*1024;
		private const int VIDEO_BUFFER_SEGMENTS = 200;
		private const int AUDIO_BUFFER_SEGMENTS = 54;
		private const int TEXT_BUFFER_SEGMENTS = 2;
		private const int LIVE_EDGE_LATENCY_MS = 30000;

		private const int SECURITY_LEVEL_UNKNOWN = -1;
		private const int SECURITY_LEVEL_1 = 1;
		private const int SECURITY_LEVEL_3 = 3;

		private readonly Context context;
		private readonly string userAgent;
		private readonly string url;
		private readonly IMediaDrmCallback drmCallback;

		private AsyncRendererBuilder currentAsyncBuilder;

		public DashRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback)
		{
			this.context = context;
			this.userAgent = userAgent;
			this.url = url;
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

		private class AsyncRendererBuilder : Object, ManifestFetcher.IManifestCallback, UtcTimingElementResolver.IUtcTimingCallback
		{

			private readonly Context context;
			private readonly string userAgent;
			private readonly IMediaDrmCallback drmCallback;
			private readonly DemoPlayer player;
			private readonly ManifestFetcher manifestFetcher;
			private readonly IUriDataSource manifestDataSource;

			private bool canceled;
			private MediaPresentationDescription manifest;
			private long elapsedRealtimeOffset;

			public AsyncRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback, DemoPlayer player)
			{
				this.context = context;
				this.userAgent = userAgent;
				this.drmCallback = drmCallback;
				this.player = player;
				var parser = new MediaPresentationDescriptionParser();
				manifestDataSource = new DefaultUriDataSource(context, userAgent);
				manifestFetcher = new ManifestFetcher(url, manifestDataSource, parser);
			}

			public void init()
			{
				manifestFetcher.SingleLoad(player.GetMainHandler().Looper, this);
			}

			public void cancel()
			{
				canceled = true;
			}

			public void OnSingleManifest(Object manifest)
			{
				if (canceled)
				{
					return;
				}

				// TODO: CRASH!
				this.manifest = manifest.JavaCast<MediaPresentationDescription>();
				if (this.manifest.Dynamic && this.manifest.UtcTiming != null)
				{
					UtcTimingElementResolver.ResolveTimingElement(manifestDataSource, this.manifest.UtcTiming,
						manifestFetcher.ManifestLoadCompleteTimestamp, this);
				}
				else
				{
					buildRenderers();
				}
			}

			public void OnSingleManifestError(IOException e)
			{
				if (canceled)
				{
					return;
				}

				player.OnRenderersError(e);
			}

			public void OnTimestampResolved(UtcTimingElement utcTiming, long elapsedRealtimeOffset)
			{
				if (canceled)
				{
					return;
				}

				this.elapsedRealtimeOffset = elapsedRealtimeOffset;
				buildRenderers();
			}

			public void OnTimestampError(UtcTimingElement utcTiming, IOException e)
			{
				if (canceled)
				{
					return;
				}

				Log.Error(TAG, "Failed to resolve UtcTiming element [" + utcTiming + "]", e);
				// Be optimistic and continue in the hope that the device clock is correct.
				buildRenderers();
			}

			private void buildRenderers()
			{
				Period period = manifest.GetPeriod(0);
				Handler mainHandler = player.GetMainHandler();
				ILoadControl loadControl = new DefaultLoadControl(new DefaultAllocator(BUFFER_SEGMENT_SIZE));
				DefaultBandwidthMeter bandwidthMeter = new DefaultBandwidthMeter(mainHandler, player);

				var hasContentProtection = false;
				var set = period.AdaptationSets
					.OfType<Object>()
					.Select(item => item.JavaCast<AdaptationSet>())
					.ToList();
				for (var i = 0; i < set.Count; i++)
				{
					AdaptationSet adaptationSet = set[i];
					if (adaptationSet.Type != AdaptationSet.TypeUnknown)
					{
						hasContentProtection |= adaptationSet.HasContentProtection;
					}
				}

				// Check drm support if necessary.
				bool filterHdContent = false;
				StreamingDrmSessionManager drmSessionManager = null;
				if (hasContentProtection)
				{
					if (Util.Util.SdkInt < 18)
					{
						player.OnRenderersError(
							new UnsupportedDrmException(UnsupportedDrmException.ReasonUnsupportedScheme));
						return;
					}
					try
					{
						drmSessionManager = StreamingDrmSessionManager.NewWidevineInstance(
							player.GetPlaybackLooper(), drmCallback, null, player.GetMainHandler(), player);
						filterHdContent = getWidevineSecurityLevel(drmSessionManager) != SECURITY_LEVEL_1;
					}
					catch (UnsupportedDrmException e)
					{
						player.OnRenderersError(e);
						return;
					}
				}

				// Build the video renderer.
				IDataSource videoDataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				IChunkSource videoChunkSource = new DashChunkSource(manifestFetcher,
					DefaultDashTrackSelector.NewVideoInstance(context, true, filterHdContent),
					videoDataSource, new FormatEvaluatorAdaptiveEvaluator(bandwidthMeter), LIVE_EDGE_LATENCY_MS,
					elapsedRealtimeOffset, mainHandler, player);
				ChunkSampleSource videoSampleSource = new ChunkSampleSource(videoChunkSource, loadControl,
					VIDEO_BUFFER_SEGMENTS*BUFFER_SEGMENT_SIZE, mainHandler, player,
					DemoPlayer.TYPE_VIDEO);
				TrackRenderer videoRenderer = new MediaCodecVideoTrackRenderer(context, videoSampleSource,
					(int) MediaCodec.VideoScalingModeScaleToFit, 5000, drmSessionManager, true,
					mainHandler, player, 50);

				// Build the audio renderer.
				IDataSource audioDataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				IChunkSource audioChunkSource = new DashChunkSource(manifestFetcher,
					DefaultDashTrackSelector.NewAudioInstance(), audioDataSource, null, LIVE_EDGE_LATENCY_MS,
					elapsedRealtimeOffset, mainHandler, player);
				ChunkSampleSource audioSampleSource = new ChunkSampleSource(audioChunkSource, loadControl,
					AUDIO_BUFFER_SEGMENTS*BUFFER_SEGMENT_SIZE, mainHandler, player,
					DemoPlayer.TYPE_AUDIO);
				TrackRenderer audioRenderer = new MediaCodecAudioTrackRenderer(audioSampleSource,
					drmSessionManager, true, mainHandler, player, AudioCapabilities.GetCapabilities(context));

				// Build the text renderer.
				IDataSource textDataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				IChunkSource textChunkSource = new DashChunkSource(manifestFetcher,
					DefaultDashTrackSelector.NewTextInstance(), textDataSource, null, LIVE_EDGE_LATENCY_MS,
					elapsedRealtimeOffset, mainHandler, player);
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

			private static int getWidevineSecurityLevel(StreamingDrmSessionManager sessionManager)
			{
				string securityLevelProperty = sessionManager.GetPropertyString("securityLevel");
				return securityLevelProperty.Equals("L1")
					? SECURITY_LEVEL_1
					: securityLevelProperty
						.Equals("L3")
						? SECURITY_LEVEL_3
						: SECURITY_LEVEL_UNKNOWN;
			}
		}
	}
}