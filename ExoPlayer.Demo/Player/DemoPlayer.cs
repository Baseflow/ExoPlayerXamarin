using System.Collections.Generic;
using Android.Media;
using Android.OS;
using Android.Views;
using Com.Google.Android.Exoplayer.Chunk;
using Com.Google.Android.Exoplayer.Dash;
using Com.Google.Android.Exoplayer.Drm;
using Com.Google.Android.Exoplayer.Hls;
using Com.Google.Android.Exoplayer.Metadata;
using Com.Google.Android.Exoplayer.Text;
using Com.Google.Android.Exoplayer.Upstream;
using Com.Google.Android.Exoplayer.Util;
using Java.IO;
using Java.Lang;

namespace Com.Google.Android.Exoplayer.Demo.Player
{
/**
 * A wrapper around {@link ExoPlayer} that provides a higher level interface. It can be prepared
 * with one of a number of {@link RendererBuilder} classes to suit different use cases (e.g. DASH,
 * SmoothStreaming and so on).
 */

    public class DemoPlayer : Object, IExoPlayerListener, ChunkSampleSource.IEventListener,
        HlsSampleSource.IEventListener, IBandwidthMeterEventListener,
        MediaCodecVideoTrackRenderer.IEventListener, MediaCodecAudioTrackRenderer.IEventListener,
        StreamingDrmSessionManager.IEventListener, DashChunkSource.IEventListener, ITextRenderer,
        MetadataTrackRenderer.IMetadataRenderer, DebugTextViewHelper.IProvider
    {

        /**
   * Builds renderers for the player.
   */

        public interface RendererBuilder
        {
            /**
     * Builds renderers for playback.
     *
     * @param player The player for which renderers are being built. {@link DemoPlayer#onRenderers}
     *     should be invoked once the renderers have been built. If building fails,
     *     {@link DemoPlayer#onRenderersError} should be invoked.
     */
            void buildRenderers(DemoPlayer player);
            /**
     * Cancels the current build operation, if there is one. Else does nothing.
     * <p>
     * A canceled build operation must not invoke {@link DemoPlayer#onRenderers} or
     * {@link DemoPlayer#onRenderersError} on the player, which may have been released.
     */
            void cancel();
        }

        /**
   * A listener for core events.
   */

        public interface Listener
        {
            void onStateChanged(bool playWhenReady, int playbackState);
            void onError(Exception e);

            void onVideoSizeChanged(
                int width,
                int height,
                int unappliedRotationDegrees,
                float pixelWidthHeightRatio);
        }

        /**
   * A listener for internal errors.
   * <p>
   * These errors are not visible to the user, and hence this listener is provided for
   * informational purposes only. Note however that an internal error may cause a fatal
   * error if the player fails to recover. If this happens, {@link Listener#onError(Exception)}
   * will be invoked.
   */

        public interface InternalErrorListener
        {
            void onRendererInitializationError(Exception e);
            void onAudioTrackInitializationError(Audio.AudioTrack.InitializationException e);
            void onAudioTrackWriteError(Audio.AudioTrack.WriteException e);
            void onDecoderInitializationError(MediaCodecTrackRenderer.DecoderInitializationException e);
            void onCryptoError(MediaCodec.CryptoException e);
            void onLoadError(int sourceId, IOException e);
            void onDrmSessionManagerError(Exception e);
        }

        /**
   * A listener for debugging information.
   */

        public interface InfoListener
        {
            void onVideoFormatEnabled(Format format, int trigger, long mediaTimeMs);
            void onAudioFormatEnabled(Format format, int trigger, long mediaTimeMs);
            void onDroppedFrames(int count, long elapsed);
            void onBandwidthSample(int elapsedMs, long bytes, long bitrateEstimate);

            void onLoadStarted(
                int sourceId,
                long length,
                int type,
                int trigger,
                Format format,
                long mediaStartTimeMs,
                long mediaEndTimeMs);

            void onLoadCompleted(
                int sourceId,
                long bytesLoaded,
                int type,
                int trigger,
                Format format,
                long mediaStartTimeMs,
                long mediaEndTimeMs,
                long elapsedRealtimeMs,
                long loadDurationMs);

            void onDecoderInitialized(
                string decoderName,
                long elapsedRealtimeMs,
                long initializationDurationMs);

            void onAvailableRangeChanged(ITimeRange availableRange);
        }

        /**
   * A listener for receiving notifications of timed text.
   */

        public interface CaptionListener
        {
            void onCues(IList<Cue> cues);
        }

        /**
   * A listener for receiving ID3 metadata parsed from the media stream.
   */

        public interface Id3MetadataListener
        {
            void onId3Metadata(object metadata);
        }

        // Constants pulled into this class for convenience.
        public const int STATE_IDLE = ExoPlayer.StateIdle;
        public const int STATE_PREPARING = ExoPlayer.StatePreparing;
        public const int STATE_BUFFERING = ExoPlayer.StateBuffering;
        public const int STATE_READY = ExoPlayer.StateReady;
        public const int STATE_ENDED = ExoPlayer.StateEnded;
        public const int TRACK_DISABLED = ExoPlayer.TrackDisabled;
        public const int TRACK_DEFAULT = ExoPlayer.TrackDefault;

        public const int RENDERER_COUNT = 4;
        public const int TYPE_VIDEO = 0;
        public const int TYPE_AUDIO = 1;
        public const int TYPE_TEXT = 2;
        public const int TYPE_METADATA = 3;

        private const int RENDERER_BUILDING_STATE_IDLE = 1;
        private const int RENDERER_BUILDING_STATE_BUILDING = 2;
        private const int RENDERER_BUILDING_STATE_BUILT = 3;

        private RendererBuilder rendererBuilder;
        private IExoPlayer player;
        private PlayerControl playerControl;
        private Handler mainHandler;
        private IList<Listener> listeners;

        private int rendererBuildingState;
        private int lastReportedPlaybackState;
        private bool lastReportedPlayWhenReady;

        private Surface surface;
        private TrackRenderer videoRenderer;
        private CodecCounters codecCounters;
        private Format videoFormat;
        private int videoTrackToRestore;

        private IBandwidthMeter bandwidthMeter;
        private bool backgrounded;

        private CaptionListener captionListener;
        private Id3MetadataListener id3MetadataListener;
        private InternalErrorListener internalErrorListener;
        private InfoListener infoListener;

        public DemoPlayer(RendererBuilder rendererBuilder)
        {
            this.rendererBuilder = rendererBuilder;
            player = ExoPlayerFactory.NewInstance(RENDERER_COUNT, 1000, 5000);
            player.AddListener(this);
            playerControl = new PlayerControl(player);
            mainHandler = new Handler();
            listeners = new List<Listener>();
            lastReportedPlaybackState = STATE_IDLE;
            rendererBuildingState = RENDERER_BUILDING_STATE_IDLE;
            // Disable text initially.
            player.SetSelectedTrack(TYPE_TEXT, TRACK_DISABLED);
        }

        public PlayerControl getPlayerControl()
        {
            return playerControl;
        }

        public void addListener(Listener listener)
        {
            listeners.Add(listener);
        }

        public void removeListener(Listener listener)
        {
            listeners.Remove(listener);
        }

        public void setInternalErrorListener(InternalErrorListener listener)
        {
            internalErrorListener = listener;
        }

        public void setInfoListener(InfoListener listener)
        {
            infoListener = listener;
        }

        public void setCaptionListener(CaptionListener listener)
        {
            captionListener = listener;
        }

        public void setMetadataListener(Id3MetadataListener listener)
        {
            id3MetadataListener = listener;
        }

        public void setSurface(Surface surface)
        {
            this.surface = surface;
            PushSurface(false);
        }

        public Surface getSurface()
        {
            return surface;
        }

        public void blockingClearSurface()
        {
            surface = null;
            PushSurface(true);
        }

        public int getTrackCount(int type)
        {
            return player.GetTrackCount(type);
        }

        public MediaFormat getTrackFormat(int type, int index)
        {
            return player.GetTrackFormat(type, index);
        }

        public int getSelectedTrack(int type)
        {
            return player.GetSelectedTrack(type);
        }

        public void setSelectedTrack(int type, int index)
        {
            player.SetSelectedTrack(type, index);
            if (type == TYPE_TEXT && index < 0 && captionListener != null)
            {
                captionListener.onCues(new List<Cue>());
            }
        }

        public bool getBackgrounded()
        {
            return backgrounded;
        }

        public void setBackgrounded(bool backgrounded)
        {
            if (this.backgrounded == backgrounded)
            {
                return;
            }
            this.backgrounded = backgrounded;
            if (backgrounded)
            {
                videoTrackToRestore = getSelectedTrack(TYPE_VIDEO);
                setSelectedTrack(TYPE_VIDEO, TRACK_DISABLED);
                blockingClearSurface();
            }
            else
            {
                setSelectedTrack(TYPE_VIDEO, videoTrackToRestore);
            }
        }

        public void prepare()
        {
            if (rendererBuildingState == RENDERER_BUILDING_STATE_BUILT)
            {
                player.Stop();
            }
            rendererBuilder.cancel();
            videoFormat = null;
            videoRenderer = null;
            rendererBuildingState = RENDERER_BUILDING_STATE_BUILDING;
            MaybeReportPlayerState();
            rendererBuilder.buildRenderers(this);
        }

        /**
   * Invoked with the results from a {@link RendererBuilder}.
   *
   * @param renderers Renderers indexed by {@link DemoPlayer} TYPE_* constants. An individual
   *     element may be null if there do not exist tracks of the corresponding type.
   * @param bandwidthMeter Provides an estimate of the currently available bandwidth. May be null.
   */

        internal void OnRenderers(TrackRenderer[] renderers, IBandwidthMeter bandwidthMeter)
        {
            for (int i = 0; i < RENDERER_COUNT; i++)
            {
                if (renderers[i] == null)
                {
                    // Convert a null renderer to a dummy renderer.
                    renderers[i] = new DummyTrackRenderer();
                }
            }
            // Complete preparation.
            this.videoRenderer = renderers[TYPE_VIDEO];
            this.codecCounters = videoRenderer is MediaCodecTrackRenderer
                ? ((MediaCodecTrackRenderer) videoRenderer).CodecCounters
                : renderers[TYPE_AUDIO] is MediaCodecTrackRenderer
                    ? ((MediaCodecTrackRenderer) renderers[TYPE_AUDIO]).CodecCounters
                    : null;
            this.bandwidthMeter = bandwidthMeter;
            PushSurface(false);
            player.Prepare(renderers);
            rendererBuildingState = RENDERER_BUILDING_STATE_BUILT;
        }

        /**
   * Invoked if a {@link RendererBuilder} encounters an error.
   *
   * @param e Describes the error.
   */

        internal void OnRenderersError(Exception e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onRendererInitializationError(e);
            }
            foreach (var listener in listeners)
            {
                listener.onError(e);
            }
            rendererBuildingState = RENDERER_BUILDING_STATE_IDLE;
            MaybeReportPlayerState();
        }

        public void SetPlayWhenReady(bool playWhenReady)
        {
            player.PlayWhenReady = playWhenReady;
        }

        public void SeekTo(long positionMs)
        {
            player.SeekTo(positionMs);
        }

        public void Release()
        {
            rendererBuilder.cancel();
            rendererBuildingState = RENDERER_BUILDING_STATE_IDLE;
            surface = null;
            player.Release();
        }

        public int GetPlaybackState()
        {
            if (rendererBuildingState == RENDERER_BUILDING_STATE_BUILDING)
            {
                return STATE_PREPARING;
            }
            int playerState = player.PlaybackState;
            if (rendererBuildingState == RENDERER_BUILDING_STATE_BUILT && playerState == STATE_IDLE)
            {
                // This is an edge case where the renderers are built, but are still being passed to the
                // player's playback thread.
                return STATE_PREPARING;
            }
            return playerState;
        }

        public Format Format
        {
            get { return videoFormat; }
        }

        public IBandwidthMeter BandwidthMeter
        {
            get { return bandwidthMeter; }
        }

        public CodecCounters CodecCounters
        {
            get { return codecCounters; }
        }

        public long CurrentPosition
        {
            get { return player.CurrentPosition; }
        }

        public long GetDuration()
        {
            return player.Duration;
        }

        public int GetBufferedPercentage()
        {
            return player.BufferedPercentage;
        }

        public bool GetPlayWhenReady()
        {
            return player.PlayWhenReady;
        }

        internal Looper GetPlaybackLooper()
        {
            return player.PlaybackLooper;
        }

        internal Handler GetMainHandler()
        {
            return mainHandler;
        }

        public void OnPlayerStateChanged(bool playWhenReady, int state)
        {
            MaybeReportPlayerState();
        }

        public void OnPlayerError(ExoPlaybackException exception)
        {
            rendererBuildingState = RENDERER_BUILDING_STATE_IDLE;
            foreach (var listener  in listeners)
            {
                listener.onError(exception);
            }
        }

        public void OnVideoSizeChanged(
            int width,
            int height,
            int unappliedRotationDegrees,
            float pixelWidthHeightRatio)
        {
            foreach (var listener in listeners)
            {
                listener.onVideoSizeChanged(width, height, unappliedRotationDegrees, pixelWidthHeightRatio);
            }
        }

        public void OnDroppedFrames(int count, long elapsed)
        {
            if (infoListener != null)
            {
                infoListener.onDroppedFrames(count, elapsed);
            }
        }

        public void OnBandwidthSample(int elapsedMs, long bytes, long bitrateEstimate)
        {
            if (infoListener != null)
            {
                infoListener.onBandwidthSample(elapsedMs, bytes, bitrateEstimate);
            }
        }

        public void OnDownstreamFormatChanged(
            int sourceId,
            Format format,
            int trigger,
            long mediaTimeMs)
        {
            if (infoListener == null)
            {
                return;
            }
            if (sourceId == TYPE_VIDEO)
            {
                videoFormat = format;
                infoListener.onVideoFormatEnabled(format, trigger, mediaTimeMs);
            }
            else if (sourceId == TYPE_AUDIO)
            {
                infoListener.onAudioFormatEnabled(format, trigger, mediaTimeMs);
            }
        }

        public void OnDrmKeysLoaded()
        {
            // Do nothing.
        }

        public void OnDrmSessionManagerError(Exception e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onDrmSessionManagerError(e);
            }
        }

        public void OnDecoderInitializationError(MediaCodecTrackRenderer.DecoderInitializationException e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onDecoderInitializationError(e);
            }
        }

        public void OnAudioTrackInitializationError(Audio.AudioTrack.InitializationException e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onAudioTrackInitializationError(e);
            }
        }

        public void OnAudioTrackWriteError(Audio.AudioTrack.WriteException e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onAudioTrackWriteError(e);
            }
        }

        public void OnCryptoError(MediaCodec.CryptoException e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onCryptoError(e);
            }
        }

        public void OnDecoderInitialized(
            string decoderName,
            long elapsedRealtimeMs,
            long initializationDurationMs)
        {
            if (infoListener != null)
            {
                infoListener.onDecoderInitialized(decoderName, elapsedRealtimeMs, initializationDurationMs);
            }
        }

        public void OnLoadError(int sourceId, IOException e)
        {
            if (internalErrorListener != null)
            {
                internalErrorListener.onLoadError(sourceId, e);
            }
        }

        public void OnCues(IList<Cue> cues)
        {
            if (captionListener != null && getSelectedTrack(TYPE_TEXT) != TRACK_DISABLED)
            {
                captionListener.onCues(cues);
            }
        }

        public void OnMetadata(Object metadata)
        {
            if (id3MetadataListener != null && getSelectedTrack(TYPE_METADATA) != TRACK_DISABLED)
            {
                id3MetadataListener.onId3Metadata(metadata);
            }
        }

        public void OnAvailableRangeChanged(ITimeRange availableRange)
        {
            if (infoListener != null)
            {
                infoListener.onAvailableRangeChanged(availableRange);
            }
        }

        public void OnPlayWhenReadyCommitted()
        {
            // Do nothing.
        }

        public void OnDrawnToSurface(Surface surface)
        {
            // Do nothing.
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
            if (infoListener != null)
            {
                infoListener.onLoadStarted(sourceId, length, type, trigger, format, mediaStartTimeMs,
                    mediaEndTimeMs);
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
            if (infoListener != null)
            {
                infoListener.onLoadCompleted(sourceId, bytesLoaded, type, trigger, format, mediaStartTimeMs,
                    mediaEndTimeMs, elapsedRealtimeMs, loadDurationMs);
            }
        }

        public void OnLoadCanceled(int sourceId, long bytesLoaded)
        {
            // Do nothing.
        }

        public void OnUpstreamDiscarded(int sourceId, long mediaStartTimeMs, long mediaEndTimeMs)
        {
            // Do nothing.
        }

        private void MaybeReportPlayerState()
        {
            var playWhenReady = player.PlayWhenReady;
            var playbackState = GetPlaybackState();
            if (lastReportedPlayWhenReady != playWhenReady || lastReportedPlaybackState != playbackState)
            {
                foreach (var listener in listeners)
                {
                    listener.onStateChanged(playWhenReady, playbackState);
                }
                lastReportedPlayWhenReady = playWhenReady;
                lastReportedPlaybackState = playbackState;
            }
        }

        private void PushSurface(bool blockForSurfacePush)
        {
            if (videoRenderer == null)
            {
                return;
            }

            if (blockForSurfacePush)
            {
                player.BlockingSendMessage(
                    videoRenderer, MediaCodecVideoTrackRenderer.MsgSetSurface, surface);
            }
            else
            {
                player.SendMessage(
                    videoRenderer, MediaCodecVideoTrackRenderer.MsgSetSurface, surface);
            }
        }
    }
}