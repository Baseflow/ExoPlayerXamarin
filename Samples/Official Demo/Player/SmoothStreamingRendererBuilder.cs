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

using Android.Content;
using Android.Media;
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
	/// <summary>
	/// A <see cref="VideoPlayer.IRendererBuilder"/> for SmoothStreaming.
	/// </summary>
	public class SmoothStreamingRendererBuilder : VideoPlayer.IRendererBuilder
	{
		private const int BufferSegmentSize = 64*1024;
		private const int VideoBufferSegments = 200;
		private const int AudioBufferSegments = 54;
		private const int TextBufferSegments = 2;
		private const int LiveEdgeLatencyMs = 30000;

		private readonly Context _context;
		private readonly string _userAgent;
		private readonly string _url;
		private readonly IMediaDrmCallback _drmCallback;

		private AsyncRendererBuilder _currentAsyncBuilder;

		public SmoothStreamingRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback)
		{
			_context = context;
			_userAgent = userAgent;
			_url = ExoPlayerUtil.ToLowerInvariant(url).EndsWith("/manifest") ? url : url + "/Manifest";
			_drmCallback = drmCallback;
		}

		public void BuildRenderers(VideoPlayer player)
		{
			_currentAsyncBuilder = new AsyncRendererBuilder(_context, _userAgent, _url, _drmCallback, player);
			_currentAsyncBuilder.Init();
		}

		public void Cancel()
		{
			if (_currentAsyncBuilder != null)
			{
				_currentAsyncBuilder.Cancel();
				_currentAsyncBuilder = null;
			}
		}

		private class AsyncRendererBuilder : Object, ManifestFetcher.IManifestCallback
		{

			private readonly Context _context;
			private readonly string _userAgent;
			private readonly IMediaDrmCallback _drmCallback;
			private readonly VideoPlayer _player;
			private readonly ManifestFetcher _manifestFetcher;

			private bool _canceled;

			public AsyncRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback, VideoPlayer player)
			{
				_context = context;
				_userAgent = userAgent;
				_drmCallback = drmCallback;
				_player = player;
				var parser = new SmoothStreamingManifestParser();
				_manifestFetcher = new ManifestFetcher(url, new DefaultHttpDataSource(userAgent, null), parser);
			}

			public void Init()
			{
				_manifestFetcher.SingleLoad(_player.MainHandler.Looper, this);
			}

			public void Cancel()
			{
				_canceled = true;
			}

			public void OnSingleManifestError(IOException exception)
			{
				if (_canceled)
				{
					return;
				}

				_player.OnRenderersError(exception);
			}

			public void OnSingleManifest(Object obj)
			{
				var manifest = obj.JavaCast<SmoothStreamingManifest>();
				if (_canceled)
				{
					return;
				}

				var mainHandler = _player.MainHandler;
				var loadControl = new DefaultLoadControl(new DefaultAllocator(BufferSegmentSize));
				var bandwidthMeter = new DefaultBandwidthMeter(mainHandler, _player);

				// Check drm support if necessary.
				IDrmSessionManager drmSessionManager = null;
				if (manifest.ProtectionElement != null)
				{
					if (ExoPlayerUtil.SdkInt < 18)
					{
						_player.OnRenderersError(
							new UnsupportedDrmException(UnsupportedDrmException.ReasonUnsupportedScheme));
						return;
					}
					try
					{
						drmSessionManager = new StreamingDrmSessionManager(manifest.ProtectionElement.Uuid,
							_player.PlaybackLooper, _drmCallback, null, _player.MainHandler, _player);
					}
					catch (Exception e)
					{
						_player.OnRenderersError(e);
						return;
					}
				}

				// Build the video renderer.
				var videoDataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var videoChunkSource = new SmoothStreamingChunkSource(_manifestFetcher
                    , DefaultSmoothStreamingTrackSelector.NewVideoInstance(_context, true, false)
                    , videoDataSource
                    , new FormatEvaluatorAdaptiveEvaluator(bandwidthMeter)
                    , LiveEdgeLatencyMs);
				var videoSampleSource = new ChunkSampleSource(videoChunkSource
                    , loadControl
                    , VideoBufferSegments*BufferSegmentSize
                    , mainHandler
                    , _player
                    , VideoPlayer.TypeVideo);
				var videoRenderer = new MediaCodecVideoTrackRenderer(_context
                    , videoSampleSource
                    , MediaCodecSelector.Default
                    , (int)VideoScalingMode.ScaleToFit
                    , 5000
                    , drmSessionManager
                    , true
                    , mainHandler
                    , _player
                    , 50);

				// Build the audio renderer.
				var audioDataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var audioChunkSource = new SmoothStreamingChunkSource(_manifestFetcher
                    , DefaultSmoothStreamingTrackSelector.NewAudioInstance()
                    , audioDataSource
                    , null
                    , LiveEdgeLatencyMs);
				var audioSampleSource = new ChunkSampleSource(audioChunkSource
                    , loadControl
                    , AudioBufferSegments*BufferSegmentSize
                    , mainHandler
                    , _player
                    , VideoPlayer.TypeAudio);
				var audioRenderer = new MediaCodecAudioTrackRenderer(audioSampleSource
                    , MediaCodecSelector.Default
                    , drmSessionManager
                    , true
                    , mainHandler
                    , _player
                    , AudioCapabilities.GetCapabilities(_context)
                    , (int) Stream.Music);

				// Build the text renderer.
				var textDataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
			    var textChunkSource = new SmoothStreamingChunkSource(_manifestFetcher
			        , DefaultSmoothStreamingTrackSelector.NewTextInstance()
                    , textDataSource
                    , null
                    , LiveEdgeLatencyMs);
				var textSampleSource = new ChunkSampleSource(textChunkSource
                    , loadControl
                    , TextBufferSegments*BufferSegmentSize
                    , mainHandler
                    , _player
                    , VideoPlayer.TypeText);
				var textRenderer = new TextTrackRenderer(textSampleSource
                    , _player
                    , mainHandler.Looper);

				// Invoke the callback.
				var renderers = new TrackRenderer[VideoPlayer.RendererCount];
				renderers[VideoPlayer.TypeVideo] = videoRenderer;
				renderers[VideoPlayer.TypeAudio] = audioRenderer;
				renderers[VideoPlayer.TypeText] = textRenderer;
				_player.OnRenderers(renderers, bandwidthMeter);
			}
		}
	}
}