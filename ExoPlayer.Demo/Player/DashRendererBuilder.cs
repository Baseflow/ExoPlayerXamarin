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
	/// <summary>
	/// A <see cref="VideoPlayer.IRendererBuilder"/> for DASH.
	/// </summary>
	public class DashRendererBuilder : VideoPlayer.IRendererBuilder
	{
		private const string Tag = "DashRendererBuilder";

		private const int BufferSegmentSize = 64*1024;
		private const int VideoBufferSegments = 200;
		private const int AudioBufferSegments = 54;
		private const int TextBufferSegments = 2;
		private const int LiveEdgeLatencyMs = 30000;

		private const int SecurityLevelUnknown = -1;
		private const int SecurityLevel1 = 1;
		private const int SecurityLevel3 = 3;

		private readonly Context _context;
		private readonly string _userAgent;
		private readonly string _url;
		private readonly IMediaDrmCallback _drmCallback;

		private AsyncRendererBuilder _currentAsyncBuilder;

		public DashRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback)
		{
			_context = context;
			_userAgent = userAgent;
			_url = url;
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

		private class AsyncRendererBuilder : Object, ManifestFetcher.IManifestCallback, UtcTimingElementResolver.IUtcTimingCallback
		{

			private readonly Context _context;
			private readonly string _userAgent;
			private readonly IMediaDrmCallback _drmCallback;
			private readonly VideoPlayer _player;
			private readonly ManifestFetcher _manifestFetcher;
			private readonly IUriDataSource _manifestDataSource;

			private bool _canceled;
			private MediaPresentationDescription _manifest;
			private long _elapsedRealtimeOffset;

			public AsyncRendererBuilder(Context context, string userAgent, string url, IMediaDrmCallback drmCallback, VideoPlayer player)
			{
				_context = context;
				_userAgent = userAgent;
				_drmCallback = drmCallback;
				_player = player;
				var parser = new MediaPresentationDescriptionParser();
				_manifestDataSource = new DefaultUriDataSource(context, userAgent);
				_manifestFetcher = new ManifestFetcher(url, _manifestDataSource, parser);
			}

			public void Init()
			{
				_manifestFetcher.SingleLoad(_player.MainHandler.Looper, this);
			}

			public void Cancel()
			{
				_canceled = true;
			}

			public void OnSingleManifest(Object manifest)
			{
				if (_canceled)
				{
					return;
				}

				_manifest = manifest.JavaCast<MediaPresentationDescription>();
				if (_manifest.Dynamic && _manifest.UtcTiming != null)
				{
					UtcTimingElementResolver.ResolveTimingElement(_manifestDataSource, _manifest.UtcTiming,
						_manifestFetcher.ManifestLoadCompleteTimestamp, this);
				}
				else
				{
					BuildRenderers();
				}
			}

			public void OnSingleManifestError(IOException e)
			{
				if (_canceled)
				{
					return;
				}

				_player.OnRenderersError(e);
			}

			public void OnTimestampResolved(UtcTimingElement utcTiming, long elapsedRealtimeOffset)
			{
				if (_canceled)
				{
					return;
				}

				_elapsedRealtimeOffset = elapsedRealtimeOffset;
				BuildRenderers();
			}

			public void OnTimestampError(UtcTimingElement utcTiming, IOException e)
			{
				if (_canceled)
				{
					return;
				}

				Log.Error(Tag, "Failed to resolve UtcTiming element [" + utcTiming + "]", e);
				// Be optimistic and continue in the hope that the device clock is correct.
				BuildRenderers();
			}

			private void BuildRenderers()
			{
				var period = _manifest.GetPeriod(0);
				var mainHandler = _player.MainHandler;
				var loadControl = new DefaultLoadControl(new DefaultAllocator(BufferSegmentSize));
				var bandwidthMeter = new DefaultBandwidthMeter(mainHandler, _player);

				var hasContentProtection = false;
				var sets = period.AdaptationSets
					.OfType<Object>()
					.Select(item => item.JavaCast<AdaptationSet>())
					.ToList();
				foreach (var set in sets)
				{
					if (set.Type != AdaptationSet.TypeUnknown)
					{
						hasContentProtection |= set.HasContentProtection;
					}
				}

				// Check drm support if necessary.
				var filterHdContent = false;
				StreamingDrmSessionManager drmSessionManager = null;
				if (hasContentProtection)
				{
					if (Util.Util.SdkInt < 18)
					{
						_player.OnRenderersError(new UnsupportedDrmException(UnsupportedDrmException.ReasonUnsupportedScheme));
						return;
					}
					try
					{
						drmSessionManager = StreamingDrmSessionManager.NewWidevineInstance(_player.PlaybackLooper, _drmCallback, null, _player.MainHandler, _player);
						filterHdContent = GetWidevineSecurityLevel(drmSessionManager) != SecurityLevel1;
					}
					catch (UnsupportedDrmException e)
					{
						_player.OnRenderersError(e);
						return;
					}
				}

				// Build the video renderer.
				var videoDataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var videoChunkSource = new DashChunkSource(_manifestFetcher,
					DefaultDashTrackSelector.NewVideoInstance(_context, true, filterHdContent),
					videoDataSource, new FormatEvaluatorAdaptiveEvaluator(bandwidthMeter), LiveEdgeLatencyMs,
					_elapsedRealtimeOffset, mainHandler, _player);
				var videoSampleSource = new ChunkSampleSource(videoChunkSource, loadControl,
					VideoBufferSegments*BufferSegmentSize, mainHandler, _player,
					VideoPlayer.TypeVideo);
				var videoRenderer = new MediaCodecVideoTrackRenderer(_context, videoSampleSource,
					(int) VideoScalingMode.ScaleToFit, 5000, drmSessionManager, true,
					mainHandler, _player, 50);

				// Build the audio renderer.
				var audioDataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var audioChunkSource = new DashChunkSource(_manifestFetcher,
					DefaultDashTrackSelector.NewAudioInstance(), audioDataSource, null, LiveEdgeLatencyMs,
					_elapsedRealtimeOffset, mainHandler, _player);
				var audioSampleSource = new ChunkSampleSource(audioChunkSource, loadControl,
					AudioBufferSegments*BufferSegmentSize, mainHandler, _player,
					VideoPlayer.TypeAudio);
				var audioRenderer = new MediaCodecAudioTrackRenderer(audioSampleSource,
					drmSessionManager, true, mainHandler, _player, AudioCapabilities.GetCapabilities(_context));

				// Build the text renderer.
				var textDataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
				var textChunkSource = new DashChunkSource(_manifestFetcher,
					DefaultDashTrackSelector.NewTextInstance(), textDataSource, null, LiveEdgeLatencyMs,
					_elapsedRealtimeOffset, mainHandler, _player);
				var textSampleSource = new ChunkSampleSource(textChunkSource, loadControl,
					TextBufferSegments*BufferSegmentSize, mainHandler, _player,
					VideoPlayer.TypeText);
				var textRenderer = new TextTrackRenderer(textSampleSource, _player,
					mainHandler.Looper);

				// Invoke the callback.
				var renderers = new TrackRenderer[VideoPlayer.RendererCount];
				renderers[VideoPlayer.TypeVideo] = videoRenderer;
				renderers[VideoPlayer.TypeAudio] = audioRenderer;
				renderers[VideoPlayer.TypeText] = textRenderer;
				_player.OnRenderers(renderers, bandwidthMeter);
			}

			private static int GetWidevineSecurityLevel(StreamingDrmSessionManager sessionManager)
			{
				switch (sessionManager.GetPropertyString("securityLevel"))
				{
					case "L1":
						return SecurityLevel1;
					case "L3":
						return SecurityLevel3;
					default:
						return SecurityLevelUnknown;
				}
			}
		}
	}
}