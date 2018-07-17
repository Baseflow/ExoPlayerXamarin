/*
 * Copyright (C) 2016 The Android Open Source Project
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

using Android.OS;
using Android.Util;
using Com.Google.Android.Exoplayer2.Audio;
using Com.Google.Android.Exoplayer2.Decoder;
using Com.Google.Android.Exoplayer2.Drm;
using Com.Google.Android.Exoplayer2.Metadata;
using Com.Google.Android.Exoplayer2.Metadata.Emsg;
using Com.Google.Android.Exoplayer2.Metadata.Id3;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Video;
using Java.IO;
using Java.Text;
using Java.Util;
using Java.Lang;
using MetadataObj = Com.Google.Android.Exoplayer2.Metadata.Metadata;
using Android.Views;

namespace Com.Google.Android.Exoplayer2.Demo
{
	/**
	 * Logs player events using {@link Log}.
	 */
	internal sealed class EventLogger : Object, IPlayerEventListener, IAudioRendererEventListener,
		IVideoRendererEventListener, IAdaptiveMediaSourceEventListener,
		ExtractorMediaSource.IEventListener, DefaultDrmSessionManager.IEventListener,
		MetadataRenderer.IOutput
	{

		private const string TAG = "EventLogger";
		private const int MAX_TIMELINE_ITEM_LINES = 3;
		private static readonly NumberFormat TIME_FORMAT;
		static EventLogger()
		{
			TIME_FORMAT = NumberFormat.GetInstance(Locale.Us);
			TIME_FORMAT.MinimumFractionDigits = 2;
			TIME_FORMAT.MaximumFractionDigits = 2;
			TIME_FORMAT.GroupingUsed = false;
		}

		private readonly MappingTrackSelector trackSelector;
		private readonly Timeline.Window window;
		private readonly Timeline.Period period;
		private readonly long startTimeMs;

		public EventLogger(MappingTrackSelector trackSelector)
		{
			this.trackSelector = trackSelector;
			window = new Timeline.Window();
			period = new Timeline.Period();
			startTimeMs = SystemClock.ElapsedRealtime();
		}

		// Player.EventListener

		public void OnLoadingChanged(bool isLoading)
		{
			Log.Debug(TAG, "loading [" + isLoading + "]");
		}

		public void OnPlayerStateChanged(bool playWhenReady, int state)
		{
			Log.Debug(TAG, "state [" + getSessionTimeString() + ", " + playWhenReady + ", "
				+ getStateString(state) + "]");
		}

	    public void OnPositionDiscontinuity(int reason)
	    {
	        Log.Debug(TAG, "discontinuity [" + getSessionTimeString() + ", " + reason + "]");
	    }

        public void OnRepeatModeChanged(int repeatMode)
		{
			Log.Debug(TAG, "repeatMode [" + getRepeatModeString(repeatMode) + "]");
		}

	    public void OnSeekProcessed()
	    {
	        Log.Debug(TAG, "seek [" + getSessionTimeString() + "]");
	    }

        public void OnShuffleModeEnabledChanged(bool enabled)
	    {
	        Log.Debug(TAG, "shuffle [" + getSessionTimeString() + ", " + enabled + "]");
	    }

		public void OnPlaybackParametersChanged(PlaybackParameters playbackParameters)
		{
			Log.Debug(TAG, "playbackParameters " + String.Format(
				"[speed=%.2f, pitch=%.2f]", playbackParameters.Speed, playbackParameters.Pitch));
		}

		public void OnTimelineChanged(Timeline timeline, Object manifest)
		{
			int periodCount = timeline.PeriodCount;
			int windowCount = timeline.WindowCount;
			Log.Debug(TAG, "sourceInfo [periodCount=" + periodCount + ", windowCount=" + windowCount);
			for (int i = 0; i < Math.Min(periodCount, MAX_TIMELINE_ITEM_LINES); i++)
			{
				timeline.GetPeriod(i, period);
				Log.Debug(TAG, "  " + "period [" + getTimeString(period.DurationMs) + "]");
			}
			if (periodCount > MAX_TIMELINE_ITEM_LINES)
			{
				Log.Debug(TAG, "  ...");
			}
			for (int i = 0; i < Math.Min(windowCount, MAX_TIMELINE_ITEM_LINES); i++)
			{
				timeline.GetWindow(i, window);
				Log.Debug(TAG, "  " + "window [" + getTimeString(window.DurationMs) + ", "
					+ window.IsSeekable + ", " + window.IsDynamic + "]");
			}
			if (windowCount > MAX_TIMELINE_ITEM_LINES)
			{
				Log.Debug(TAG, "  ...");
			}
			Log.Debug(TAG, "]");
		}

		public void OnPlayerError(ExoPlaybackException e)
		{
			Log.Error(TAG, "playerFailed [" + getSessionTimeString() + "]", e);
		}

		public void OnTracksChanged(TrackGroupArray ignored, TrackSelectionArray trackSelections)
		{
			var mappedTrackInfo = trackSelector.CurrentMappedTrackInfo;
			if (mappedTrackInfo == null)
			{
				Log.Debug(TAG, "Tracks []");
				return;
			}
			Log.Debug(TAG, "Tracks [");
			// Log tracks associated to renderers.
			for (var rendererIndex = 0; rendererIndex < mappedTrackInfo.Length; rendererIndex++)
			{
				var rendererTrackGroups = mappedTrackInfo.GetTrackGroups(rendererIndex);
				var trackSelection = trackSelections.Get(rendererIndex);
				if (rendererTrackGroups.Length > 0)
				{
					Log.Debug(TAG, "  Renderer:" + rendererIndex + " [");
					for (int groupIndex = 0; groupIndex < rendererTrackGroups.Length; groupIndex++)
					{
						TrackGroup trackGroup = rendererTrackGroups.Get(groupIndex);
						var adaptiveSupport = getAdaptiveSupportString(trackGroup.Length,
							mappedTrackInfo.GetAdaptiveSupport(rendererIndex, groupIndex, false));
						Log.Debug(TAG, "    Group:" + groupIndex + ", adaptive_supported=" + adaptiveSupport + " [");
						for (int trackIndex = 0; trackIndex < trackGroup.Length; trackIndex++)
						{
							var status = getTrackStatusString(trackSelection, trackGroup, trackIndex);
							var formatSupport = getFormatSupportString(
								mappedTrackInfo.GetTrackFormatSupport(rendererIndex, groupIndex, trackIndex));
							Log.Debug(TAG, "      " + status + " Track:" + trackIndex + ", "
								+ Format.ToLogString(trackGroup.GetFormat(trackIndex))
								+ ", supported=" + formatSupport);
						}
						Log.Debug(TAG, "    ]");
					}
					// Log metadata for at most one of the tracks selected for the renderer.
					if (trackSelection != null)
					{
						for (var selectionIndex = 0; selectionIndex < trackSelection.Length(); selectionIndex++)
						{
							var metadata = trackSelection.GetFormat(selectionIndex).Metadata;
							if (metadata != null)
							{
								Log.Debug(TAG, "    Metadata [");
								printMetadata(metadata, "      ");
								Log.Debug(TAG, "    ]");
								break;
							}
						}
					}
					Log.Debug(TAG, "  ]");
				}
			}
			// Log tracks not associated with a renderer.
			TrackGroupArray unassociatedTrackGroups = mappedTrackInfo.UnassociatedTrackGroups;
			if (unassociatedTrackGroups.Length > 0)
			{
				Log.Debug(TAG, "  Renderer:None [");
				for (int groupIndex = 0; groupIndex < unassociatedTrackGroups.Length; groupIndex++)
				{
					Log.Debug(TAG, "    Group:" + groupIndex + " [");
					var trackGroup = unassociatedTrackGroups.Get(groupIndex);
					for (int trackIndex = 0; trackIndex < trackGroup.Length; trackIndex++)
					{
						var status = getTrackStatusString(false);
						var formatSupport = getFormatSupportString(
							RendererCapabilities.FormatUnsupportedType);
						Log.Debug(TAG, "      " + status + " Track:" + trackIndex + ", "
							+ Format.ToLogString(trackGroup.GetFormat(trackIndex))
							+ ", supported=" + formatSupport);
					}
					Log.Debug(TAG, "    ]");
				}
				Log.Debug(TAG, "  ]");
			}
			Log.Debug(TAG, "]");
		}

		// MetadataRenderer.Output

		public void OnMetadata(MetadataObj metadata)
		{
			Log.Debug(TAG, "onMetadata [");
			printMetadata(metadata, "  ");
			Log.Debug(TAG, "]");
		}

		// AudioRendererEventListener

		public void OnAudioEnabled(DecoderCounters counters)
		{
			Log.Debug(TAG, "audioEnabled [" + getSessionTimeString() + "]");
		}

		public void OnAudioSessionId(int audioSessionId)
		{
			Log.Debug(TAG, "audioSessionId [" + audioSessionId + "]");
		}

	    public void OnAudioSinkUnderrun(int p0, long p1, long p2)
	    {
	        throw new System.NotImplementedException();
	    }

	    public void OnAudioDecoderInitialized(string decoderName, long elapsedRealtimeMs,
			long initializationDurationMs)
		{
			Log.Debug(TAG, "audioDecoderInitialized [" + getSessionTimeString() + ", " + decoderName + "]");
		}

		public void OnAudioInputFormatChanged(Format format)
		{
			Log.Debug(TAG, "audioFormatChanged [" + getSessionTimeString() + ", " + Format.ToLogString(format)
				+ "]");
		}

		public void OnAudioDisabled(DecoderCounters counters)
		{
			Log.Debug(TAG, "audioDisabled [" + getSessionTimeString() + "]");
		}

		public void OnAudioTrackUnderrun(int bufferSize, long bufferSizeMs, long elapsedSinceLastFeedMs)
		{
			printInternalError("audioTrackUnderrun [" + bufferSize + ", " + bufferSizeMs + ", "
				+ elapsedSinceLastFeedMs + "]", null);
		}

		// VideoRendererEventListener

		public void OnVideoEnabled(DecoderCounters counters)
		{
			Log.Debug(TAG, "videoEnabled [" + getSessionTimeString() + "]");
		}

		public void OnVideoDecoderInitialized(string decoderName, long elapsedRealtimeMs,
			long initializationDurationMs)
		{
			Log.Debug(TAG, "videoDecoderInitialized [" + getSessionTimeString() + ", " + decoderName + "]");
		}

		public void OnVideoInputFormatChanged(Format format)
		{
			Log.Debug(TAG, "videoFormatChanged [" + getSessionTimeString() + ", " + Format.ToLogString(format)
				+ "]");
		}

		public void OnVideoDisabled(DecoderCounters counters)
		{
			Log.Debug(TAG, "videoDisabled [" + getSessionTimeString() + "]");
		}

		public void OnDroppedFrames(int count, long elapsed)
		{
			Log.Debug(TAG, "droppedFrames [" + getSessionTimeString() + ", " + count + "]");
		}

		public void OnVideoSizeChanged(int width, int height, int unappliedRotationDegrees,
			float pixelWidthHeightRatio)
		{
			Log.Debug(TAG, "videoSizeChanged [" + width + ", " + height + "]");
		}

		public void OnRenderedFirstFrame(Surface surface)
		{
			Log.Debug(TAG, "renderedFirstFrame [" + surface + "]");
		}

		// DefaultDrmSessionManager.EventListener

		public void OnDrmSessionManagerError(Exception e)
		{
			printInternalError("drmSessionManagerError", e);
		}

		public void OnDrmKeysRestored()
		{
			Log.Debug(TAG, "drmKeysRestored [" + getSessionTimeString() + "]");
		}

		public void OnDrmKeysRemoved()
		{
			Log.Debug(TAG, "drmKeysRemoved [" + getSessionTimeString() + "]");
		}

		public void OnDrmKeysLoaded()
		{
			Log.Debug(TAG, "drmKeysLoaded [" + getSessionTimeString() + "]");
		}

		// ExtractorMediaSource.EventListener

		public void OnLoadError(IOException error)
		{
			printInternalError("loadError", error);
		}

		// AdaptiveMediaSourceEventListener

		public void OnLoadStarted(DataSpec dataSpec, int dataType, int trackType, Format trackFormat,
			int trackSelectionReason, Object trackSelectionData, long mediaStartTimeMs,
			long mediaEndTimeMs, long elapsedRealtimeMs)
		{
			// Do nothing.
		}

		public void OnLoadError(DataSpec dataSpec, int dataType, int trackType, Format trackFormat,
			int trackSelectionReason, Object trackSelectionData, long mediaStartTimeMs,
			long mediaEndTimeMs, long elapsedRealtimeMs, long loadDurationMs, long bytesLoaded,
			IOException error, bool wasCanceled)
		{
			printInternalError("loadError", error);
		}

		public void OnLoadCanceled(DataSpec dataSpec, int dataType, int trackType, Format trackFormat,
			int trackSelectionReason, Object trackSelectionData, long mediaStartTimeMs,
			long mediaEndTimeMs, long elapsedRealtimeMs, long loadDurationMs, long bytesLoaded)
		{
			// Do nothing.
		}

		public void OnLoadCompleted(DataSpec dataSpec, int dataType, int trackType, Format trackFormat,
			int trackSelectionReason, Object trackSelectionData, long mediaStartTimeMs,
			long mediaEndTimeMs, long elapsedRealtimeMs, long loadDurationMs, long bytesLoaded)
		{
			// Do nothing.
		}

		public void OnUpstreamDiscarded(int trackType, long mediaStartTimeMs, long mediaEndTimeMs)
		{
			// Do nothing.
		}

		public void OnDownstreamFormatChanged(int trackType, Format trackFormat, int trackSelectionReason,
			Object trackSelectionData, long mediaTimeMs)
		{
			// Do nothing.
		}

		// Internal methods

		private void printInternalError(string type, Exception e)
		{
			Log.Error(TAG, "internalError [" + getSessionTimeString() + ", " + type + "]", e);
		}

		private void printMetadata(MetadataObj metadata, string prefix)
		{
			for (var i = 0; i < metadata.Length(); i++)
			{
				var entry = metadata.Get(i);
				if (entry is TextInformationFrame)
				{
					var textInformationFrame = (TextInformationFrame)entry;
					Log.Debug(TAG, prefix + String.Format("%s: value=%s", textInformationFrame.Id,
						textInformationFrame.Value));
				}
				else if (entry is UrlLinkFrame)
				{
					var urlLinkFrame = (UrlLinkFrame)entry;
					Log.Debug(TAG, prefix + String.Format("%s: url=%s", urlLinkFrame.Id, urlLinkFrame.Url));
				}
				else if (entry is PrivFrame)
				{
					var privFrame = (PrivFrame)entry;
					Log.Debug(TAG, prefix + String.Format("%s: owner=%s", PrivFrame.Id, privFrame.Owner));
				}
				else if (entry is GeobFrame)
				{
					var geobFrame = (GeobFrame)entry;
					Log.Debug(TAG, prefix + String.Format("%s: mimeType=%s, filename=%s, description=%s",
						GeobFrame.Id, geobFrame.MimeType, geobFrame.Filename, geobFrame.Description));
				}
				else if (entry is ApicFrame)
				{
					var apicFrame = (ApicFrame)entry;
					Log.Debug(TAG, prefix + String.Format("%s: mimeType=%s, description=%s",
						ApicFrame.Id, apicFrame.MimeType, apicFrame.Description));
				}
				else if (entry is CommentFrame)
				{
					var commentFrame = (CommentFrame)entry;
					Log.Debug(TAG, prefix + String.Format("%s: language=%s, description=%s", CommentFrame.Id,
						commentFrame.Language, commentFrame.Description));
				}
				else if (entry is Id3Frame)
				{
					var id3Frame = (Id3Frame)entry;
					Log.Debug(TAG, prefix + String.Format("%s", id3Frame.Id));
				}
				else if (entry is EventMessage)
				{
					EventMessage eventMessage = (EventMessage)entry;
					Log.Debug(TAG, prefix + String.Format("EMSG: scheme=%s, id=%d, value=%s",
						eventMessage.SchemeIdUri, eventMessage.Id, eventMessage.Value));
				}
			}
		}

		private string getSessionTimeString()
		{
			return getTimeString(SystemClock.ElapsedRealtime() - startTimeMs);
		}

		private static string getTimeString(long timeMs)
		{
			return timeMs == C.TimeUnset ? "?" : TIME_FORMAT.Format((timeMs) / 1000f);
		}

		private static string getStateString(int state)
		{
			switch (state)
			{
				case Player.StateBuffering:
					return "B";
				case Player.StateEnded:
					return "E";
				case Player.StateIdle:
					return "I";
				case Player.StateReady:
					return "R";
				default:
					return "?";
			}
		}

		private static string getFormatSupportString(int formatSupport)
		{
			switch (formatSupport)
			{
				case RendererCapabilities.FormatHandled:
					return "YES";
				case RendererCapabilities.FormatExceedsCapabilities:
					return "NO_EXCEEDS_CAPABILITIES";
				case RendererCapabilities.FormatUnsupportedSubtype:
					return "NO_UNSUPPORTED_TYPE";
				case RendererCapabilities.FormatUnsupportedType:
					return "NO";
				default:
					return "?";
			}
		}

		private static string getAdaptiveSupportString(int trackCount, int adaptiveSupport)
		{
			if (trackCount < 2)
			{
				return "N/A";
			}
			switch (adaptiveSupport)
			{
				case RendererCapabilities.AdaptiveSeamless:
					return "YES";
				case RendererCapabilities.AdaptiveNotSeamless:
					return "YES_NOT_SEAMLESS";
				case RendererCapabilities.AdaptiveNotSupported:
					return "NO";
				default:
					return "?";
			}
		}

		private static string getTrackStatusString(ITrackSelection selection, TrackGroup group,
			int trackIndex)
		{
			return getTrackStatusString(selection != null && selection.TrackGroup == group
				&& selection.IndexOf(trackIndex) != C.IndexUnset);
		}

		private static string getTrackStatusString(bool enabled)
		{
			return enabled ? "[X]" : "[ ]";
		}

		private static string getRepeatModeString(int repeatMode)
		{
			switch (repeatMode)
			{
				case Player.RepeatModeOff:
					return "OFF";
				case Player.RepeatModeOne:
					return "ONE";
				case Player.RepeatModeAll:
					return "ALL";
				default:
					return "?";
			}
		}
	}
}