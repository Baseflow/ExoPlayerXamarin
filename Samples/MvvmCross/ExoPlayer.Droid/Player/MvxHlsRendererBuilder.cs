/*
 * Copyright (C) 2014 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Linq;
using Android.Content;
using Android.Media;
using Android.Runtime;
using Com.Google.Android.Exoplayer;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Chunk;
using Com.Google.Android.Exoplayer.Hls;
using Com.Google.Android.Exoplayer.Text.Eia608;
using Com.Google.Android.Exoplayer.Upstream;
using Com.Google.Android.Exoplayer.Util;
using Java.IO;
using Java.Lang;

namespace MvvmCross.ExoPlayer.Droid.Player
{
	/// <summary>
	/// A <see cref="MvxVideoPlayer.IRendererBuilder"/> for HLS.
	/// </summary>
	[Register("mvvmcross.exoplayer.droid.player.MvxHlsRendererBuilder")]
	public class MvxHlsRendererBuilder : MvxVideoPlayer.IRendererBuilder
	{
		private const int BufferSegmentSize = 64*1024;
		private const int BufferSegments = 256;

		private readonly Context _context;
		private readonly string _userAgent;
		private readonly string _url;

		private AsyncRendererBuilder _currentAsyncBuilder;

		public MvxHlsRendererBuilder(Context context, string userAgent, string url)
		{
			_context = context;
			_userAgent = userAgent;
			_url = url;
		}

		public void BuildRenderers(MvxVideoPlayer player)
		{
			_currentAsyncBuilder = new AsyncRendererBuilder(_context, _userAgent, _url, player);
			_currentAsyncBuilder.Init();
		}

		public void Cancel()
		{
			if (_currentAsyncBuilder != null)
			{
				_currentAsyncBuilder.cancel();
				_currentAsyncBuilder = null;
			}
		}

		private class AsyncRendererBuilder : Object, ManifestFetcher.IManifestCallback
		{
			private readonly Context _context;
			private readonly string _userAgent;
			private readonly string _url;
			private readonly MvxVideoPlayer _player;
			private readonly ManifestFetcher _playlistFetcher;

			private bool _canceled;

			public AsyncRendererBuilder(Context context, string userAgent, string url, MvxVideoPlayer player)
			{
				_context = context;
				_userAgent = userAgent;
				_url = url;
				_player = player;
				var parser = new HlsPlaylistParser();
				_playlistFetcher = new ManifestFetcher(url, new DefaultUriDataSource(context, userAgent),
					parser);
			}

			public void Init()
			{
				_playlistFetcher.SingleLoad(_player.MainHandler.Looper, this);
			}

			public void cancel()
			{
				_canceled = true;
			}

			public void OnSingleManifestError(IOException e)
			{
				if (_canceled)
				{
					return;
				}

				_player.OnRenderersError(e);
			}

			public void OnSingleManifest(Object obj)
			{
				var manifest = obj.JavaCast<HlsPlaylist>();
				if (_canceled)
				{
					return;
				}

				var mainHandler = _player.MainHandler;
				var loadControl = new DefaultLoadControl(new DefaultAllocator(BufferSegmentSize));
				var bandwidthMeter = new DefaultBandwidthMeter();

				int[] variantIndices = null;
				if (manifest is HlsMasterPlaylist)
				{
					var masterPlaylist = (HlsMasterPlaylist) manifest;
					try
					{
						variantIndices = VideoFormatSelectorUtil.SelectVideoFormatsForDefaultDisplay(
							_context, masterPlaylist.Variants.Cast<IFormatWrapper>().ToList(), null, false);
					}
					catch (MediaCodecUtil.DecoderQueryException e)
					{
						_player.OnRenderersError(e);
						return;
					}
					if (variantIndices.Length == 0)
					{
						_player.OnRenderersError(new IllegalStateException($"No variants selected. Possible reason: your video's resolution could be too high. This device maximum H264 framesize is {MediaCodecUtil.MaxH264DecodableFrameSize()}."));
						return;
					}
				}

				var dataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var chunkSource = new HlsChunkSource(dataSource, _url, manifest, bandwidthMeter,
					variantIndices, HlsChunkSource.AdaptiveModeSplice);
				var sampleSource = new HlsSampleSource(chunkSource, loadControl,
					BufferSegments*BufferSegmentSize, mainHandler, _player, MvxVideoPlayer.TypeVideo);
				var videoRenderer = new MediaCodecVideoTrackRenderer(_context,
					sampleSource, (int) VideoScalingMode.ScaleToFit, 5000, mainHandler, _player, 50);
				var audioRenderer = new MediaCodecAudioTrackRenderer(sampleSource,
					null, true, _player.MainHandler, _player, AudioCapabilities.GetCapabilities(_context));
				// TODO: The Id3Parser is currently not part of the binding
				//MetadataTrackRenderer id3Renderer = new MetadataTrackRenderer(sampleSource, new Id3Parser(), player, mainHandler.getLooper());
				var closedCaptionRenderer = new Eia608TrackRenderer(sampleSource, _player,
					mainHandler.Looper);

				var renderers = new TrackRenderer[MvxVideoPlayer.RendererCount];
				renderers[MvxVideoPlayer.TypeVideo] = videoRenderer;
				renderers[MvxVideoPlayer.TypeAudio] = audioRenderer;
				//renderers[DemoPlayer.TYPE_METADATA] = id3Renderer;
				renderers[MvxVideoPlayer.TypeText] = closedCaptionRenderer;
				_player.OnRenderers(renderers, bandwidthMeter);
			}
		}
	}
}