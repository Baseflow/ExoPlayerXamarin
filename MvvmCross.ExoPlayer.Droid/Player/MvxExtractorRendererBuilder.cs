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
using Android.Net;
using Android.Runtime;
using Com.Google.Android.Exoplayer;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Extractor;
using Com.Google.Android.Exoplayer.Text;
using Com.Google.Android.Exoplayer.Upstream;

namespace MvvmCross.ExoPlayer.Droid.Player
{
	/// <summary>
	///  A <see cref="Extractor"/> that can be read using an <see cref="MvxVideoPlayer"/>.
	/// </summary>
	[Register("mvvmcross.exoplayer.droid.player.MvxExtractorRendererBuilder")]
	public class MvxExtractorRendererBuilder : MvxVideoPlayer.IRendererBuilder
	{
		private const int BufferSegmentSize = 64*1024;
		private const int BufferSegmentCount = 256;

		private readonly Context _context;
		private readonly string _userAgent;
		private readonly Uri _uri;

		public MvxExtractorRendererBuilder(Context context, string userAgent, Uri uri)
		{
			_context = context;
			_userAgent = userAgent;
			_uri = uri;
		}

		public void BuildRenderers(MvxVideoPlayer player)
		{
			var allocator = new DefaultAllocator(BufferSegmentSize);

			// Build the video and audio renderers.
			var bandwidthMeter = new DefaultBandwidthMeter(player.MainHandler, null);
			var dataSource = new DefaultUriDataSource(_context, bandwidthMeter, _userAgent);
			var sampleSource = new ExtractorSampleSource(_uri, dataSource, allocator,
				BufferSegmentCount*BufferSegmentSize);
			var videoRenderer = new MediaCodecVideoTrackRenderer(_context,
				sampleSource, (int) VideoScalingMode.ScaleToFit, 5000, player.MainHandler,
				player, 50);
			var audioRenderer = new MediaCodecAudioTrackRenderer(sampleSource,
				null, true, player.MainHandler, player, AudioCapabilities.GetCapabilities(_context));
			var textRenderer = new TextTrackRenderer(sampleSource, player,
				player.MainHandler.Looper);

			// Invoke the callback.
			var renderers = new TrackRenderer[MvxVideoPlayer.RendererCount];
			renderers[MvxVideoPlayer.TypeVideo] = videoRenderer;
			renderers[MvxVideoPlayer.TypeAudio] = audioRenderer;
			renderers[MvxVideoPlayer.TypeText] = textRenderer;
			player.OnRenderers(renderers, bandwidthMeter);
		}

		public void Cancel()
		{
			// Do nothing.
		}
	}
}