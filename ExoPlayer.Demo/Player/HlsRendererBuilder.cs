using System.Linq;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Chunk;
using Com.Google.Android.Exoplayer.Hls;
using Com.Google.Android.Exoplayer.Text.Eia608;
using Com.Google.Android.Exoplayer.Upstream;
using Com.Google.Android.Exoplayer.Util;
using Java.IO;
using Java.Lang;

namespace Com.Google.Android.Exoplayer.Demo.Player
{
	/**
 * A {@link RendererBuilder} for HLS.
 */

	public class HlsRendererBuilder : DemoPlayer.RendererBuilder
	{

		private const int BUFFER_SEGMENT_SIZE = 64*1024;
		private const int BUFFER_SEGMENTS = 256;

		private readonly Context context;
		private readonly string userAgent;
		private readonly string url;

		private AsyncRendererBuilder currentAsyncBuilder;

		public HlsRendererBuilder(Context context, string userAgent, string url)
		{
			this.context = context;
			this.userAgent = userAgent;
			this.url = url;
		}

		public void buildRenderers(DemoPlayer player)
		{
			currentAsyncBuilder = new AsyncRendererBuilder(context, userAgent, url, player);
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
			private readonly string url;
			private readonly DemoPlayer player;
			private readonly ManifestFetcher playlistFetcher;

			private bool canceled;

			public AsyncRendererBuilder(Context context, string userAgent, string url, DemoPlayer player)
			{
				this.context = context;
				this.userAgent = userAgent;
				this.url = url;
				this.player = player;
				HlsPlaylistParser parser = new HlsPlaylistParser();
				playlistFetcher = new ManifestFetcher(url, new DefaultUriDataSource(context, userAgent),
					parser);
			}

			public void init()
			{
				playlistFetcher.SingleLoad(player.GetMainHandler().Looper, this);
			}

			public void cancel()
			{
				canceled = true;
			}

			public void OnSingleManifestError(IOException e)
			{
				if (canceled)
				{
					return;
				}

				player.OnRenderersError(e);
			}

			public void OnSingleManifest(Object obj)
			{
				var manifest = obj.JavaCast<HlsPlaylist>();
				if (canceled)
				{
					return;
				}

				Handler mainHandler = player.GetMainHandler();
				ILoadControl loadControl = new DefaultLoadControl(new DefaultAllocator(BUFFER_SEGMENT_SIZE));
				DefaultBandwidthMeter bandwidthMeter = new DefaultBandwidthMeter();

				int[] variantIndices = null;
				if (manifest is HlsMasterPlaylist)
				{
					HlsMasterPlaylist masterPlaylist = (HlsMasterPlaylist) manifest;
					try
					{
						variantIndices = VideoFormatSelectorUtil.SelectVideoFormatsForDefaultDisplay(
							context, masterPlaylist.Variants.Cast<IFormatWrapper>().ToList(), null, false);
					}
					catch (MediaCodecUtil.DecoderQueryException e)
					{
						player.OnRenderersError(e);
						return;
					}
					if (variantIndices.Length == 0)
					{
						player.OnRenderersError(new IllegalStateException("No variants selected."));
						return;
					}
				}

				IDataSource dataSource = new DefaultUriDataSource(context, bandwidthMeter, userAgent);
				HlsChunkSource chunkSource = new HlsChunkSource(dataSource, url, manifest, bandwidthMeter,
					variantIndices, HlsChunkSource.AdaptiveModeSplice);
				HlsSampleSource sampleSource = new HlsSampleSource(chunkSource, loadControl,
					BUFFER_SEGMENTS*BUFFER_SEGMENT_SIZE, mainHandler, player, DemoPlayer.TYPE_VIDEO);
				MediaCodecVideoTrackRenderer videoRenderer = new MediaCodecVideoTrackRenderer(context,
					sampleSource, (int) MediaCodec.VideoScalingModeScaleToFit, 5000, mainHandler, player, 50);
				MediaCodecAudioTrackRenderer audioRenderer = new MediaCodecAudioTrackRenderer(sampleSource,
					null, true, player.GetMainHandler(), player, AudioCapabilities.GetCapabilities(context));
				//MetadataTrackRenderer id3Renderer = new MetadataTrackRenderer(sampleSource, new Id3Parser(), player, mainHandler.getLooper());
				Eia608TrackRenderer closedCaptionRenderer = new Eia608TrackRenderer(sampleSource, player,
					mainHandler.Looper);

				TrackRenderer[] renderers = new TrackRenderer[DemoPlayer.RENDERER_COUNT];
				renderers[DemoPlayer.TYPE_VIDEO] = videoRenderer;
				renderers[DemoPlayer.TYPE_AUDIO] = audioRenderer;
				//renderers[DemoPlayer.TYPE_METADATA] = id3Renderer;
				renderers[DemoPlayer.TYPE_TEXT] = closedCaptionRenderer;
				player.OnRenderers(renderers, bandwidthMeter);
			}

		}

	}
}