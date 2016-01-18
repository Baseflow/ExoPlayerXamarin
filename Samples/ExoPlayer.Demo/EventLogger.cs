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

using Android.Media;
using Android.Util;
using Com.Google.Android.Exoplayer.Chunk;
using Com.Google.Android.Exoplayer.Demo.Player;
using Com.Google.Android.Exoplayer.Util;
using Java.IO;
using Java.Lang;
using Java.Text;
using Java.Util;
using AudioTrack = Com.Google.Android.Exoplayer.Audio.AudioTrack;
using SystemClock = Android.OS.SystemClock;

namespace Com.Google.Android.Exoplayer.Demo
{
	/// <summary>
	/// Logs player events using {@link Log}.
	/// </summary>
	public class EventLogger : VideoPlayer.IListener, VideoPlayer.IInfoListener,
        VideoPlayer.IInternalErrorListener
    {

        private const string Tag = "EventLogger";

        private static readonly NumberFormat TimeFormat;

        static EventLogger()
        {
            TimeFormat = NumberFormat.GetInstance(Locale.Us);
            TimeFormat.MinimumFractionDigits = 2;
            TimeFormat.MaximumFractionDigits = 2;
        }

        private long _sessionStartTimeMs;
        private readonly long[] _loadStartTimeMs;
        private long[] _availableRangeValuesUs;

        public EventLogger()
        {
            _loadStartTimeMs = new long[VideoPlayer.RendererCount];
        }

        public void StartSession()
        {
            _sessionStartTimeMs = SystemClock.ElapsedRealtime();
            Log.Debug(Tag, "start [0]");
        }

        public void EndSession()
        {
            Log.Debug(Tag, "end [" + GetSessionTimeString() + "]");
        }

        // DemoPlayer.Listener

        public void OnStateChanged(bool playWhenReady, int state)
        {
            Log.Debug(Tag, "state [" + GetSessionTimeString() + ", " + playWhenReady + ", "
                           + GetStateString(state) + "]");
        }

        public void OnError(Exception e)
        {
            Log.Error(Tag, "playerFailed [" + GetSessionTimeString() + "]", e);
        }

        public void OnVideoSizeChanged(
            int width,
            int height,
            int unappliedRotationDegrees,
            float pixelWidthHeightRatio)
        {
            Log.Debug(Tag, "videoSizeChanged [" + width + ", " + height + ", " + unappliedRotationDegrees
                           + ", " + pixelWidthHeightRatio + "]");
        }

        // DemoPlayer.InfoListener

        public void OnBandwidthSample(int elapsedMs, long bytes, long bitrateEstimate)
        {
            Log.Debug(Tag, "bandwidth [" + GetSessionTimeString() + ", " + bytes + ", "
                           + GetTimeString(elapsedMs) + ", " + bitrateEstimate + "]");
        }

        public void OnDroppedFrames(int count, long elapsed)
        {
            Log.Debug(Tag, "droppedFrames [" + GetSessionTimeString() + ", " + count + "]");
        }

        public void OnLoadStarted(
            int sourceId,
            long length,
            int type,
            int trigger,
            Format format,
            long mediaStartTimeMs,
            long mediaEndTimeMs)
        {
            _loadStartTimeMs[sourceId] = SystemClock.ElapsedRealtime();
            if (VerboseLogUtil.IsTagEnabled(Tag))
            {
                Log.Verbose(Tag, "loadStart [" + GetSessionTimeString() + ", " + sourceId + ", " + type
                                 + ", " + mediaStartTimeMs + ", " + mediaEndTimeMs + "]");
            }
        }

        public void OnLoadCompleted(
            int sourceId,
            long bytesLoaded,
            int type,
            int trigger,
            Format format,
            long mediaStartTimeMs,
            long mediaEndTimeMs,
            long elapsedRealtimeMs,
            long loadDurationMs)
        {
            if (VerboseLogUtil.IsTagEnabled(Tag))
            {
                long downloadTime = SystemClock.ElapsedRealtime() - _loadStartTimeMs[sourceId];
                Log.Verbose(Tag, "loadEnd [" + GetSessionTimeString() + ", " + sourceId + ", " + downloadTime
                                 + "]");
            }
        }

        public void OnVideoFormatEnabled(Format format, int trigger, long mediaTimeMs)
        {
            Log.Debug(Tag, "videoFormat [" + GetSessionTimeString() + ", " + format.Id + ", "
                           + trigger.ToString() + "]");
        }

        public void OnAudioFormatEnabled(Format format, int trigger, long mediaTimeMs)
        {
            Log.Debug(Tag, "audioFormat [" + GetSessionTimeString() + ", " + format.Id + ", "
                           + trigger.ToString() + "]");
        }

        // DemoPlayer.InternalErrorListener

        public void OnLoadError(int sourceId, IOException e)
        {
            PrintInternalError("loadError", e);
        }

        public void OnRendererInitializationError(Exception e)
        {
            PrintInternalError("rendererInitError", e);
        }

        public void OnDrmSessionManagerError(Exception e)
        {
            PrintInternalError("drmSessionManagerError", e);
        }

        public void OnDecoderInitializationError(MediaCodecTrackRenderer.DecoderInitializationException e)
        {
            PrintInternalError("decoderInitializationError", e);
        }

        public void OnAudioTrackInitializationError(AudioTrack.InitializationException e)
        {
            PrintInternalError("audioTrackInitializationError", e);
        }

        public void OnAudioTrackWriteError(AudioTrack.WriteException e)
        {
            PrintInternalError("audioTrackWriteError", e);
        }

		public void OnAudioTrackUnderrun(int bufferSize, long bufferSizeMs, long elapsedSinceLastFeedMs)
		{
			PrintInternalError("audioTrackUnderrun [" + bufferSize + ", " + bufferSizeMs + ", "
			                   + elapsedSinceLastFeedMs + "]", null);
		}

		public void OnCryptoError(MediaCodec.CryptoException e)
        {
            PrintInternalError("cryptoError", e);
        }

        public void OnDecoderInitialized(
            string decoderName,
            long elapsedRealtimeMs,
            long initializationDurationMs)
        {
            Log.Debug(Tag, "decoderInitialized [" + GetSessionTimeString() + ", " + decoderName + "]");
        }

        public void OnAvailableRangeChanged(ITimeRange availableRange)
        {
            _availableRangeValuesUs = availableRange.GetCurrentBoundsUs(_availableRangeValuesUs);
            Log.Debug(Tag, "availableRange [" + availableRange.IsStatic + ", " + _availableRangeValuesUs[0]
                           + ", " + _availableRangeValuesUs[1] + "]");
        }

        private void PrintInternalError(string type, Exception e)
        {
            Log.Error(Tag, "internalError [" + GetSessionTimeString() + ", " + type + "]", e);
        }

        private string GetStateString(int state)
        {
            switch (state)
            {
                case ExoPlayer.StateBuffering:
                    return "B";
                case ExoPlayer.StateEnded:
                    return "E";
                case ExoPlayer.StateIdle:
                    return "I";
                case ExoPlayer.StatePreparing:
                    return "P";
                case ExoPlayer.StateReady:
                    return "R";
                default:
                    return "?";
            }
        }

        private string GetSessionTimeString()
        {
            return GetTimeString(SystemClock.ElapsedRealtime() - _sessionStartTimeMs);
        }

        private string GetTimeString(long timeMs)
        {
            return TimeFormat.Format((timeMs)/1000f);
        }
    }
}