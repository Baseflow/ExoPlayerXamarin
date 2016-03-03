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
	/// <summary>
	/// A <see cref="VideoPlayer.IRendererBuilder"/> for HLS.
	/// </summary>
	public class HlsRendererBuilder : VideoPlayer.IRendererBuilder
	{
		private const int BufferSegmentSize = 64*1024;
		private const int BufferSegments = 256;

		private readonly Context _context;
		private readonly string _userAgent;
		private readonly string _url;

		private AsyncRendererBuilder _currentAsyncBuilder;

		public HlsRendererBuilder(Context context, string userAgent, string url)
		{
			_context = context;
			_userAgent = userAgent;
			_url = url;
		}

		public void BuildRenderers(VideoPlayer player)
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
			private readonly VideoPlayer _player;
			private readonly ManifestFetcher _playlistFetcher;

			private bool _canceled;

			public AsyncRendererBuilder(Context context, string userAgent, string url, VideoPlayer player)
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
                var timestampAdjusterProvider = new PtsTimestampAdjusterProvider();
                
                var dataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var chunkSource = new HlsChunkSource(true
                    , dataSource
                    , _url
                    , manifest
                    , DefaultHlsTrackSelector.NewDefaultInstance(_context)
                    , bandwidthMeter
                    , timestampAdjusterProvider
                    , HlsChunkSource.AdaptiveModeSplice);
				var sampleSource = new HlsSampleSource(chunkSource
                    , loadControl
                    , BufferSegments*BufferSegmentSize
                    , mainHandler
                    , _player
                    , VideoPlayer.TypeVideo);
				var videoRenderer = new MediaCodecVideoTrackRenderer(_context
                    , sampleSource
                    , MediaCodecSelector.Default
                    , (int) VideoScalingMode.ScaleToFit
                    , 5000
                    , mainHandler
                    , _player
                    , 50);
				var audioRenderer = new MediaCodecAudioTrackRenderer(sampleSource
                    , MediaCodecSelector.Default
                    , null
                    , true
                    , _player.MainHandler
                    , _player
                    , AudioCapabilities.GetCapabilities(_context)
                    , (int) Stream.Music);
				// TODO: The Id3Parser is currently not part of the binding
				//MetadataTrackRenderer id3Renderer = new MetadataTrackRenderer(sampleSource, new Id3Parser(), player, mainHandler.getLooper());
				var closedCaptionRenderer = new Eia608TrackRenderer(sampleSource, _player,
					mainHandler.Looper);

				var renderers = new TrackRenderer[VideoPlayer.RendererCount];
				renderers[VideoPlayer.TypeVideo] = videoRenderer;
				renderers[VideoPlayer.TypeAudio] = audioRenderer;
				//renderers[DemoPlayer.TYPE_METADATA] = id3Renderer;
				renderers[VideoPlayer.TypeText] = closedCaptionRenderer;
				_player.OnRenderers(renderers, bandwidthMeter);
			}
		}
	}
}