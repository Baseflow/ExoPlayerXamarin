using System;
using System.Collections.Generic;
using Android.Content;
using Android.Gms.Cast;
using Android.Gms.Cast.Framework;
using Android.Views;
using Com.Google.Android.Exoplayer2.Ext.Cast;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.UI;
using Java.Lang;
using static Com.Google.Android.Exoplayer2.CastDemo.DemoUtil;
using static Com.Google.Android.Exoplayer2.Timeline;
using android = Android;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming;
using Com.Google.Android.Exoplayer2.Source.Dash;
using Com.Google.Android.Exoplayer2.Source.Hls;

namespace Com.Google.Android.Exoplayer2.CastDemo
{
    /**
    * Manages players and an internal media queue for the ExoPlayer/Cast demo app.
    */
    /* package */
    internal class PlayerManager : Java.Lang.Object, IPlayerEventListener, CastPlayer.ISessionAvailabilityListener
    {

        /**
         * Listener for changes in the media queue playback position.
         */
        public interface IQueuePositionListener
        {

            /**
             * Called when the currently played item of the media queue changes.
             */
            void OnQueuePositionChanged(int previousIndex, int newIndex);

        }

        private static readonly string USER_AGENT = "ExoCastDemoPlayer";
        private static DefaultBandwidthMeter BANDWIDTH_METER = new DefaultBandwidthMeter();
        private static readonly DefaultHttpDataSourceFactory DATA_SOURCE_FACTORY = new DefaultHttpDataSourceFactory(USER_AGENT, BANDWIDTH_METER);

        private readonly PlayerView localPlayerView;
        private readonly PlayerControlView castControlView;
        private SimpleExoPlayer exoPlayer;
        private CastPlayer castPlayer;
        private List<DemoUtil.Sample> mediaQueue;
        private readonly IQueuePositionListener queuePositionListener;
        private ConcatenatingMediaSource concatenatingMediaSource;

        private bool castMediaQueueCreationPending;
        private int currentItemIndex;
        private IPlayer currentPlayer;

        /**
         * @param queuePositionListener A {@link QueuePositionListener} for queue position changes.
         * @param localPlayerView The {@link PlayerView} for local playback.
         * @param castControlView The {@link PlayerControlView} to control remote playback.
         * @param context A {@link Context}.
         * @param castContext The {@link CastContext}.
         */
        public static PlayerManager CreatePlayerManager(IQueuePositionListener queuePositionListener, PlayerView localPlayerView, PlayerControlView castControlView, Context context, CastContext castContext)
        {
            PlayerManager playerManager = new PlayerManager(queuePositionListener, localPlayerView, castControlView, context, castContext);
            playerManager.Init();
            return playerManager;
        }

        private PlayerManager(IQueuePositionListener queuePositionListener, PlayerView localPlayerView, PlayerControlView castControlView, Context context, CastContext castContext)
        {
            this.queuePositionListener = queuePositionListener;
            this.localPlayerView = localPlayerView;
            this.castControlView = castControlView;
            mediaQueue = new List<DemoUtil.Sample>();
            currentItemIndex = C.IndexUnset;
            concatenatingMediaSource = new ConcatenatingMediaSource();

            DefaultTrackSelector trackSelector = new DefaultTrackSelector(BANDWIDTH_METER);
            IRenderersFactory renderersFactory = new DefaultRenderersFactory(context);
            exoPlayer = ExoPlayerFactory.NewSimpleInstance(renderersFactory, trackSelector);
            exoPlayer.AddListener(this);
            localPlayerView.Player = exoPlayer;

            castPlayer = new CastPlayer(castContext);
            castPlayer.AddListener(this);
            castPlayer.SetSessionAvailabilityListener(this);
            castControlView.Player = castPlayer;
        }

        // Queue manipulation methods.

        /**
         * Plays a specified queue item in the current player.
         *
         * @param itemIndex The index of the item to play.
         */
        public void SelectQueueItem(int itemIndex)
        {
            setCurrentItem(itemIndex, C.TimeUnset, true);
        }

        /**
         * Returns the index of the currently played item.
         */
        public int GetCurrentItemIndex()
        {
            return currentItemIndex;
        }

        /**
         * Appends {@code sample} to the media queue.
         *
         * @param sample The {@link Sample} to append.
         */
        public void AddItem(Sample sample)
        {
            mediaQueue.Add(sample);
            concatenatingMediaSource.AddMediaSource(BuildMediaSource(sample));
            if (currentPlayer == castPlayer)
            {
                castPlayer.AddItems(buildMediaQueueItem(sample));
            }
        }

        /**
         * Returns the size of the media queue.
         */
        public int GetMediaQueueSize()
        {
            return mediaQueue.Count;
        }

        /**
         * Returns the item at the given index in the media queue.
         *
         * @param position The index of the item.
         * @return The item at the given index in the media queue.
         */
        public Sample GetItem(int position)
        {
            return mediaQueue[position];
        }

        /**
         * Removes the item at the given index from the media queue.
         *
         * @param itemIndex The index of the item to remove.
         * @return Whether the removal was successful.
         */
        public bool removeItem(int itemIndex)
        {
            concatenatingMediaSource.RemoveMediaSource(itemIndex);
            if (currentPlayer == castPlayer)
            {
                if (castPlayer.PlaybackState != Player.StateIdle)
                {
                    Timeline castTimeline = castPlayer.CurrentTimeline;
                    if (castTimeline.PeriodCount <= itemIndex)
                    {
                        return false;
                    }
                    castPlayer.RemoveItem((int)castTimeline.GetPeriod(itemIndex, new Period()).Id);
                }
            }
            mediaQueue.Remove(mediaQueue[itemIndex]);
            if (itemIndex == currentItemIndex && itemIndex == mediaQueue.Count)
            {
                MaybeSetCurrentItemAndNotify(C.IndexUnset);
            }
            else if (itemIndex < currentItemIndex)
            {
                MaybeSetCurrentItemAndNotify(currentItemIndex - 1);
            }
            return true;
        }

        /**
         * Moves an item within the queue.
         *
         * @param fromIndex The index of the item to move.
         * @param toIndex The target index of the item in the queue.
         * @return Whether the item move was successful.
         */
        public bool MoveItem(int fromIndex, int toIndex)
        {
            // Player update.
            concatenatingMediaSource.MoveMediaSource(fromIndex, toIndex);
            if (currentPlayer == castPlayer && castPlayer.PlaybackState != Player.StateIdle)
            {
                Timeline castTimeline = castPlayer.CurrentTimeline;
                int periodCount = castTimeline.PeriodCount;
                if (periodCount <= fromIndex || periodCount <= toIndex)
                {
                    return false;
                }
                int elementId = (int)castTimeline.GetPeriod(fromIndex, new Period()).Id;
                castPlayer.MoveItem(elementId, toIndex);
            }

            mediaQueue.Insert(toIndex, mediaQueue[fromIndex]);
            mediaQueue.Remove(mediaQueue[fromIndex]);

            // Index update.
            if (fromIndex == currentItemIndex)
            {
                MaybeSetCurrentItemAndNotify(toIndex);
            }
            else if (fromIndex < currentItemIndex && toIndex >= currentItemIndex)
            {
                MaybeSetCurrentItemAndNotify(currentItemIndex - 1);
            }
            else if (fromIndex > currentItemIndex && toIndex <= currentItemIndex)
            {
                MaybeSetCurrentItemAndNotify(currentItemIndex + 1);
            }

            return true;
        }

        // Miscellaneous methods.

        /**
         * Dispatches a given {@link KeyEvent} to the corresponding view of the current player.
         *
         * @param event The {@link KeyEvent}.
         * @return Whether the event was handled by the target view.
         */
        public bool DispatchKeyEvent(KeyEvent @event)
        {
            if (currentPlayer == exoPlayer)
            {
                return localPlayerView.DispatchKeyEvent(@event);
            }
            else /* currentPlayer == castPlayer */
            {
                return castControlView.DispatchKeyEvent(@event);
            }
        }

        /**
         * Releases the manager and the players that it holds.
         */
        public void Release()
        {
            currentItemIndex = C.IndexUnset;
            mediaQueue.Clear();
            concatenatingMediaSource.Clear();
            castPlayer.SetSessionAvailabilityListener(null);
            castPlayer.Release();
            localPlayerView.Player = null;
            exoPlayer.Release();
        }

        // Player.EventListener implementation.

        public override void OnPlayerStateChanged(bool playWhenReady, int playbackState)
        {
            UpdateCurrentItemIndex();
        }

        public override void OnPositionDiscontinuity(int reason)
        {
            UpdateCurrentItemIndex();
        }

        public void OnTimelineChanged(
                Timeline timeline, object manifest, int reason)
        {
            UpdateCurrentItemIndex();
            if (timeline.IsEmpty)
            {
                castMediaQueueCreationPending = true;
            }
        }

        // CastPlayer.SessionAvailabilityListener implementation.

        public void OnCastSessionAvailable()
        {
            setCurrentPlayer(castPlayer);
        }

        public void OnCastSessionUnavailable()
        {
            setCurrentPlayer(exoPlayer);
        }

        // Internal methods.

        private void Init()
        {
            setCurrentPlayer((castPlayer.IsCastSessionAvailable ? (IPlayer)castPlayer : exoPlayer));
        }

        private void UpdateCurrentItemIndex()
        {
            int playbackState = currentPlayer.PlaybackState;
            MaybeSetCurrentItemAndNotify(
                    playbackState != Player.StateIdle && playbackState != Player.StateEnded
                            ? currentPlayer.CurrentWindowIndex : C.IndexUnset);
        }

        private void setCurrentPlayer(IPlayer currentPlayer)
        {
            if (this.currentPlayer == currentPlayer)
            {
                return;
            }

            // View management.
            if (currentPlayer == exoPlayer)
            {
                localPlayerView.Visibility = ViewStates.Visible;
                castControlView.Hide();
            }
            else /* currentPlayer == castPlayer */
            {
                localPlayerView.Visibility = ViewStates.Gone;
                castControlView.Show();
            }

            // Player state management.
            long playbackPositionMs = C.TimeUnset;
            int windowIndex = C.IndexUnset;
            bool playWhenReady = false;
            if (this.currentPlayer != null)
            {
                int playbackState = this.currentPlayer.PlaybackState;
                if (playbackState != Player.StateEnded)
                {
                    playbackPositionMs = this.currentPlayer.CurrentPosition;
                    playWhenReady = this.currentPlayer.PlayWhenReady;
                    windowIndex = this.currentPlayer.CurrentWindowIndex;
                    if (windowIndex != currentItemIndex)
                    {
                        playbackPositionMs = C.TimeUnset;
                        windowIndex = currentItemIndex;
                    }
                }
                this.currentPlayer.Stop(true);
            }
            else
            {
                // This is the initial setup. No need to save any state.
            }

            this.currentPlayer = currentPlayer;

            // Media queue management.
            castMediaQueueCreationPending = currentPlayer == castPlayer;
            if (currentPlayer == exoPlayer)
            {
                exoPlayer.Prepare(concatenatingMediaSource);
            }

            // Playback transition.
            if (windowIndex != C.IndexUnset)
            {
                setCurrentItem(windowIndex, playbackPositionMs, playWhenReady);
            }
        }

        /**
         * Starts playback of the item at the given position.
         *
         * @param itemIndex The index of the item to play.
         * @param positionMs The position at which playback should start.
         * @param playWhenReady Whether the player should proceed when ready to do so.
         */
        private void setCurrentItem(int itemIndex, long positionMs, bool playWhenReady)
        {
            MaybeSetCurrentItemAndNotify(itemIndex);
            if (castMediaQueueCreationPending)
            {
                MediaQueueItem[] items = new MediaQueueItem[mediaQueue.Count];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = buildMediaQueueItem(mediaQueue[i]);
                }
                castMediaQueueCreationPending = false;
                castPlayer.LoadItems(items, itemIndex, positionMs, Player.RepeatModeOff);
            }
            else
            {
                currentPlayer.SeekTo(itemIndex, positionMs);
                currentPlayer.PlayWhenReady = playWhenReady;
            }
        }

        private void MaybeSetCurrentItemAndNotify(int currentItemIndex)
        {
            if (this.currentItemIndex != currentItemIndex)
            {
                int oldIndex = this.currentItemIndex;
                this.currentItemIndex = currentItemIndex;
                queuePositionListener.OnQueuePositionChanged(oldIndex, currentItemIndex);
            }
        }

        private static IMediaSource BuildMediaSource(Sample sample)
        {
            android.Net.Uri uri = android.Net.Uri.Parse(sample.uri);
            switch (sample.mimeType)
            {
                case DemoUtil.MIME_TYPE_SS:
                    return new SsMediaSource.Factory(new DefaultSsChunkSource.Factory(DATA_SOURCE_FACTORY), DATA_SOURCE_FACTORY).CreateMediaSource(uri);
                case DemoUtil.MIME_TYPE_DASH:
                    return new DashMediaSource.Factory(new DefaultDashChunkSource.Factory(DATA_SOURCE_FACTORY), DATA_SOURCE_FACTORY).CreateMediaSource(uri);
                case DemoUtil.MIME_TYPE_HLS:
                    return new HlsMediaSource.Factory(DATA_SOURCE_FACTORY).CreateMediaSource(uri);
                case DemoUtil.MIME_TYPE_VIDEO_MP4:
                    return new ExtractorMediaSource.Factory(DATA_SOURCE_FACTORY).CreateMediaSource(uri);
                case DemoUtil.MIME_TYPE_AUDIO:
                    return new ExtractorMediaSource.Factory(DATA_SOURCE_FACTORY).CreateMediaSource(uri);
                default:
                    {
                        throw new IllegalStateException("Unsupported type: " + sample.mimeType);
                    }
            }
        }

        private static MediaQueueItem buildMediaQueueItem(DemoUtil.Sample sample)
        {
            MediaMetadata movieMetadata = new MediaMetadata(MediaMetadata.MediaTypeMovie);
            movieMetadata.PutString(MediaMetadata.KeyTitle, sample.name);
            MediaInfo mediaInfo = new MediaInfo.Builder(sample.uri)
                    .SetStreamType(MediaInfo.StreamTypeBuffered).SetContentType(sample.mimeType)
                    .SetMetadata(movieMetadata).Build();
            return new MediaQueueItem.Builder(mediaInfo).Build();
        }

    }

}
