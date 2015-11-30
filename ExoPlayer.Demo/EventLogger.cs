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

/**
 * Logs player events using {@link Log}.
 */

    public class EventLogger : DemoPlayer.Listener, DemoPlayer.InfoListener,
        DemoPlayer.InternalErrorListener
    {

        private const string TAG = "EventLogger";
        private static NumberFormat TIME_FORMAT;

        static EventLogger()
        {
            TIME_FORMAT = NumberFormat.GetInstance(Locale.Us);
            TIME_FORMAT.MinimumFractionDigits = 2;
            TIME_FORMAT.MaximumFractionDigits = 2;
        }

        private long sessionStartTimeMs;
        private long[] loadStartTimeMs;
        private long[] availableRangeValuesUs;

        public EventLogger()
        {
            loadStartTimeMs = new long[DemoPlayer.RENDERER_COUNT];
        }

        public void startSession()
        {
            sessionStartTimeMs = SystemClock.ElapsedRealtime();
            Log.Debug(TAG, "start [0]");
        }

        public void endSession()
        {
            Log.Debug(TAG, "end [" + getSessionTimeString() + "]");
        }

        // DemoPlayer.Listener

        public void onStateChanged(bool playWhenReady, int state)
        {
            Log.Debug(TAG, "state [" + getSessionTimeString() + ", " + playWhenReady + ", "
                           + getStateString(state) + "]");
        }

        public void onError(Exception e)
        {
            Log.Error(TAG, "playerFailed [" + getSessionTimeString() + "]", e);
        }

        public void onVideoSizeChanged(
            int width,
            int height,
            int unappliedRotationDegrees,
            float pixelWidthHeightRatio)
        {
            Log.Debug(TAG, "videoSizeChanged [" + width + ", " + height + ", " + unappliedRotationDegrees
                           + ", " + pixelWidthHeightRatio + "]");
        }

        // DemoPlayer.InfoListener

        public void onBandwidthSample(int elapsedMs, long bytes, long bitrateEstimate)
        {
            Log.Debug(TAG, "bandwidth [" + getSessionTimeString() + ", " + bytes + ", "
                           + getTimeString(elapsedMs) + ", " + bitrateEstimate + "]");
        }

        public void onDroppedFrames(int count, long elapsed)
        {
            Log.Debug(TAG, "droppedFrames [" + getSessionTimeString() + ", " + count + "]");
        }

        public void onLoadStarted(
            int sourceId,
            long length,
            int type,
            int trigger,
            Format format,
            long mediaStartTimeMs,
            long mediaEndTimeMs)
        {
            loadStartTimeMs[sourceId] = SystemClock.ElapsedRealtime();
            if (VerboseLogUtil.IsTagEnabled(TAG))
            {
                Log.Verbose(TAG, "loadStart [" + getSessionTimeString() + ", " + sourceId + ", " + type
                                 + ", " + mediaStartTimeMs + ", " + mediaEndTimeMs + "]");
            }
        }

        public void onLoadCompleted(
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
            if (VerboseLogUtil.IsTagEnabled(TAG))
            {
                long downloadTime = SystemClock.ElapsedRealtime() - loadStartTimeMs[sourceId];
                Log.Verbose(TAG, "loadEnd [" + getSessionTimeString() + ", " + sourceId + ", " + downloadTime
                                 + "]");
            }
        }

        public void onVideoFormatEnabled(Format format, int trigger, long mediaTimeMs)
        {
            Log.Debug(TAG, "videoFormat [" + getSessionTimeString() + ", " + format.Id + ", "
                           + trigger.ToString() + "]");
        }

        public void onAudioFormatEnabled(Format format, int trigger, long mediaTimeMs)
        {
            Log.Debug(TAG, "audioFormat [" + getSessionTimeString() + ", " + format.Id + ", "
                           + trigger.ToString() + "]");
        }

        // DemoPlayer.InternalErrorListener

        public void onLoadError(int sourceId, IOException e)
        {
            printInternalError("loadError", e);
        }

        public void onRendererInitializationError(Exception e)
        {
            printInternalError("rendererInitError", e);
        }

        public void onDrmSessionManagerError(Exception e)
        {
            printInternalError("drmSessionManagerError", e);
        }

        public void onDecoderInitializationError(MediaCodecTrackRenderer.DecoderInitializationException e)
        {
            printInternalError("decoderInitializationError", e);
        }

        public void onAudioTrackInitializationError(AudioTrack.InitializationException e)
        {
            printInternalError("audioTrackInitializationError", e);
        }

        public void onAudioTrackWriteError(AudioTrack.WriteException e)
        {
            printInternalError("audioTrackWriteError", e);
        }

        public void onCryptoError(MediaCodec.CryptoException e)
        {
            printInternalError("cryptoError", e);
        }

        public void onDecoderInitialized(
            string decoderName,
            long elapsedRealtimeMs,
            long initializationDurationMs)
        {
            Log.Debug(TAG, "decoderInitialized [" + getSessionTimeString() + ", " + decoderName + "]");
        }

        public void onAvailableRangeChanged(ITimeRange availableRange)
        {
            availableRangeValuesUs = availableRange.GetCurrentBoundsUs(availableRangeValuesUs);
            Log.Debug(TAG, "availableRange [" + availableRange.IsStatic + ", " + availableRangeValuesUs[0]
                           + ", " + availableRangeValuesUs[1] + "]");
        }

        private void printInternalError(string type, Exception e)
        {
            Log.Error(TAG, "internalError [" + getSessionTimeString() + ", " + type + "]", e);
        }

        private string getStateString(int state)
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

        private string getSessionTimeString()
        {
            return getTimeString(SystemClock.ElapsedRealtime() - sessionStartTimeMs);
        }

        private string getTimeString(long timeMs)
        {
            return TIME_FORMAT.Format((timeMs)/1000f);
        }
    }
}