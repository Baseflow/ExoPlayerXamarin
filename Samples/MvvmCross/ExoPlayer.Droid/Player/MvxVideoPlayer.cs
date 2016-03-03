using System.Collections.Generic;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Com.Google.Android.Exoplayer;
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
using MediaFormat = Com.Google.Android.Exoplayer.MediaFormat;

namespace MvvmCross.ExoPlayer.Droid.Player
{
	/// <summary>
	/// Adaptiert aus dem Exoplayer Demo-Projekt.
	/// 
	/// A wrapper around <see cref="ExoPlayer"/> that provides a higher level interface. It can be prepared
	/// with one of a number of <see cref="IRendererBuilder"/> classes to suit different use cases(e.g. DASH,
	/// SmoothStreaming and so on).
	/// </summary>
	[Register("mvvmcross.exoplayer.droid.player.MvxVideoPlayer")]
	public class MvxVideoPlayer : Object,
		IExoPlayerListener,
		ChunkSampleSource.IEventListener,
		HlsSampleSource.IEventListener,
		IBandwidthMeterEventListener,
		MediaCodecVideoTrackRenderer.IEventListener,
	MediaCodecAudioTrackRenderer.IEventListener,
		StreamingDrmSessionManager.IEventListener,
		DashChunkSource.IEventListener, ITextRenderer,
		MetadataTrackRenderer.IMetadataRenderer,
		DebugTextViewHelper.IProvider
	{
		/// <summary>
		/// Builds renderers for the player.
		/// </summary>
		public interface IRendererBuilder
		{
			/// <summary>
			/// Builds renderers for playback.
			/// </summary>
			/// <param name="player">The player for which renderers are being built. <c>DemoPlayer#onRenderers</c>
			/// should be invoked once the renderers have been built. If building fails,
			/// <c>DemoPlayer#onRenderersError</c> should be invoked.</param>
			void BuildRenderers(MvxVideoPlayer player);

			/// <summary>
			/// Cancels the current build operation, if there is one. Else does nothing.
			/// 
			/// A canceled build operation must not invoke <c>DemoPlayer#onRenderers</c> or
			/// <c>DemoPlayer#onRenderersError</c> on the player, which may have been released.
			/// </summary>
			void Cancel();
		}

		/// <summary>
		/// A listener for core events.
		/// </summary>
		public interface IListener
		{
			void OnStateChanged(bool playWhenReady, int playbackState);
			void OnError(Exception e);

			void OnVideoSizeChanged(
				int width,
				int height,
				int unappliedRotationDegrees,
				float pixelWidthHeightRatio);
		}

		/// <summary>
		/// A listener for internal errors.
		/// These errors are not visible to the user, and hence this listener is provided for
		/// informational purposes only.Note however that an internal error may cause a fatal
		/// error if the player fails to recover.If this happens, <c>Listener#onError(Exception)</c>
		/// will be invoked.
		/// </summary>
		public interface IInternalErrorListener
		{
			void OnRendererInitializationError(Exception e);
			void OnAudioTrackInitializationError(Com.Google.Android.Exoplayer.Audio.AudioTrack.InitializationException e);
			void OnAudioTrackWriteError(Com.Google.Android.Exoplayer.Audio.AudioTrack.WriteException e);
			void OnDecoderInitializationError(MediaCodecTrackRenderer.DecoderInitializationException e);
			void OnCryptoError(MediaCodec.CryptoException e);
			void OnLoadError(int sourceId, IOException e);
			void OnDrmSessionManagerError(Exception e);
		}

		/// <summary>
		/// A listener for debugging information.
		/// </summary>
		public interface IInfoListener
		{
			void OnVideoFormatEnabled(Format format, int trigger, long mediaTimeMs);
			void OnAudioFormatEnabled(Format format, int trigger, long mediaTimeMs);
			void OnDroppedFrames(int count, long elapsed);
			void OnBandwidthSample(int elapsedMs, long bytes, long bitrateEstimate);

			void OnLoadStarted(
				int sourceId,
				long length,
				int type,
				int trigger,
				Format format,
				long mediaStartTimeMs,
				long mediaEndTimeMs);

			void OnLoadCompleted(
				int sourceId,
				long bytesLoaded,
				int type,
				int trigger,
				Format format,
				long mediaStartTimeMs,
				long mediaEndTimeMs,
				long elapsedRealtimeMs,
				long loadDurationMs);

			void OnDecoderInitialized(
				string decoderName,
				long elapsedRealtimeMs,
				long initializationDurationMs);

			void OnAvailableRangeChanged(ITimeRange availableRange);
		}

		/// <summary>
		/// A listener for receiving notifications of timed text.
		/// </summary>
		public interface ICaptionListener
		{
			void OnCues(IList<Cue> cues);
		}

		/// <summary>
		/// A listener for receiving ID3 metadata parsed from the media stream.
		/// </summary>
		public interface ID3MetadataListener
		{
			void OnId3Metadata(object metadata);
		}

		// Constants pulled into this class for convenience.
		public const int StateIdle = Com.Google.Android.Exoplayer.ExoPlayer.StateIdle;
		public const int StatePreparing = Com.Google.Android.Exoplayer.ExoPlayer.StatePreparing;
		public const int StateBuffering = Com.Google.Android.Exoplayer.ExoPlayer.StateBuffering;
		public const int StateReady = Com.Google.Android.Exoplayer.ExoPlayer.StateReady;
		public const int StateEnded = Com.Google.Android.Exoplayer.ExoPlayer.StateEnded;
		public const int TrackDisabled = Com.Google.Android.Exoplayer.ExoPlayer.TrackDisabled;
		public const int TrackDefault = Com.Google.Android.Exoplayer.ExoPlayer.TrackDefault;

		public const int RendererCount = 4;
		public const int TypeVideo = 0;
		public const int TypeAudio = 1;
		public const int TypeText = 2;
		public const int TypeMetadata = 3;

		private const int RendererBuildingStateIdle = 1;
		private const int RendererBuildingStateBuilding = 2;
		private const int RendererBuildingStateBuilt = 3;

		private readonly IRendererBuilder _rendererBuilder;
		private readonly IExoPlayer _player;
		private readonly PlayerControl _playerControl;
		private readonly Handler _mainHandler;
		private readonly IList<IListener> _listeners;

		private int _rendererBuildingState;
		private int _lastReportedPlaybackState;
		private bool _lastReportedPlayWhenReady;

		private Surface _surface;
		private TrackRenderer _videoRenderer;
		private CodecCounters _codecCounters;
		private Format _videoFormat;
		private int _videoTrackToRestore;

		private IBandwidthMeter _bandwidthMeter;
		private bool _backgrounded;

		private ICaptionListener _captionListener;
		private ID3MetadataListener _id3MetadataListener;
		private IInternalErrorListener _internalErrorListener;
		private IInfoListener _infoListener;

		public MvxVideoPlayer(IRendererBuilder rendererBuilder)
		{
			_rendererBuilder = rendererBuilder;
			_player = ExoPlayerFactory.NewInstance(RendererCount, 1000, 5000);
			_player.AddListener(this);
			_playerControl = new PlayerControl(_player);
			_mainHandler = new Handler();
			_listeners = new List<IListener>();
			_lastReportedPlaybackState = StateIdle;
			_rendererBuildingState = RendererBuildingStateIdle;
			// Disable text initially.
			_player.SetSelectedTrack(TypeText, TrackDisabled);
		}

		public PlayerControl PlayerControl
		{
			get { return _playerControl; }
		}

		public void AddListener(IListener listener)
		{
			_listeners.Add(listener);
		}

		public void RemoveListener(IListener listener)
		{
			_listeners.Remove(listener);
		}

		public void SetInternalErrorListener(IInternalErrorListener listener)
		{
			_internalErrorListener = listener;
		}

		public void SetInfoListener(IInfoListener listener)
		{
			_infoListener = listener;
		}

		public void SetCaptionListener(ICaptionListener listener)
		{
			_captionListener = listener;
		}

		public void SetMetadataListener(ID3MetadataListener listener)
		{
			_id3MetadataListener = listener;
		}

		public Surface Surface
		{
			get { return _surface; }
			set
			{
				_surface = value;
				PushSurface(false);
			}
		}

		public void BlockingClearSurface()
		{
			_surface = null;
			PushSurface(true);
		}

		public int GetTrackCount(int type)
		{
			return _player.GetTrackCount(type);
		}

		public MediaFormat GetTrackFormat(int type, int index)
		{
			return _player.GetTrackFormat(type, index);
		}

		public int GetSelectedTrack(int type)
		{
			return _player.GetSelectedTrack(type);
		}

		public void SetSelectedTrack(int type, int index)
		{
			_player.SetSelectedTrack(type, index);
			if (type == TypeText && index < 0 && _captionListener != null)
			{
				_captionListener.OnCues(new List<Cue>());
			}
		}

		public bool Backgrounded
		{
			get { return _backgrounded; }
			set
			{
				if (_backgrounded == value)
				{
					return;
				}
				_backgrounded = value;
				if (value)
				{
					_videoTrackToRestore = GetSelectedTrack(TypeVideo);
					SetSelectedTrack(TypeVideo, TrackDisabled);
					BlockingClearSurface();
				}
				else
				{
					SetSelectedTrack(TypeVideo, _videoTrackToRestore);
				}
			}
		}

		public void Prepare()
		{
			if (_rendererBuildingState == RendererBuildingStateBuilt)
			{
				_player.Stop();
			}
			_rendererBuilder.Cancel();
			_videoFormat = null;
			_videoRenderer = null;
			_rendererBuildingState = RendererBuildingStateBuilding;
			MaybeReportPlayerState();
			_rendererBuilder.BuildRenderers(this);
		}

		/// <summary>
		/// Invoked with the results from a <see cref="IRendererBuilder"/>.
		/// </summary>
		/// <param name="renderers">Renderers indexed by <see cref="MvxVideoPlayer"/>. TYPE_* constants. An individual element may be null if there do not exist tracks of the corresponding type.</param>
		/// <param name="bandwidthMeter">Provides an estimate of the currently available bandwidth. May be null.</param>
		internal void OnRenderers(TrackRenderer[] renderers, IBandwidthMeter bandwidthMeter)
		{
			for (var i = 0; i < RendererCount; i++)
			{
				if (renderers[i] == null)
				{
					// Convert a null renderer to a dummy renderer.
					renderers[i] = new DummyTrackRenderer();
				}
			}
			// Complete preparation.
			_videoRenderer = renderers[TypeVideo];
			_codecCounters = _videoRenderer is MediaCodecTrackRenderer
				? ((MediaCodecTrackRenderer) _videoRenderer).CodecCounters
				: renderers[TypeAudio] is MediaCodecTrackRenderer
					? ((MediaCodecTrackRenderer) renderers[TypeAudio]).CodecCounters
					: null;
			_bandwidthMeter = bandwidthMeter;
			PushSurface(false);
			_player.Prepare(renderers);
			_rendererBuildingState = RendererBuildingStateBuilt;
		}

		/// <summary>
		/// Invoked if a {@link RendererBuilder} encounters an error.
		/// </summary>
		/// <param name="e">Describes the error.</param>
		internal void OnRenderersError(Exception e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnRendererInitializationError(e);
			}
			foreach (var listener in _listeners)
			{
				listener.OnError(e);
			}
			_rendererBuildingState = RendererBuildingStateIdle;
			MaybeReportPlayerState();
		}

		public bool PlayWhenReady
		{
			get { return _player.PlayWhenReady; }
			set { _player.PlayWhenReady = value; }
		}

		public void SeekTo(long positionMs)
		{
			_player.SeekTo(positionMs);
		}

		public void Release()
		{
			_rendererBuilder.Cancel();
			_rendererBuildingState = RendererBuildingStateIdle;
			_surface = null;
			_player.Release();
		}

		public int PlaybackState
		{
			get
			{
				if (_rendererBuildingState == RendererBuildingStateBuilding)
				{
					return StatePreparing;
				}
				var playerState = _player.PlaybackState;
				if (_rendererBuildingState == RendererBuildingStateBuilt && playerState == StateIdle)
				{
					// This is an edge case where the renderers are built, but are still being passed to the
					// player's playback thread.
					return StatePreparing;
				}
				return playerState;
			}
		}

		public Format Format
		{
			get { return _videoFormat; }
		}

		public IBandwidthMeter BandwidthMeter
		{
			get { return _bandwidthMeter; }
		}

		public CodecCounters CodecCounters
		{
			get { return _codecCounters; }
		}

		public long CurrentPosition
		{
			get { return _player.CurrentPosition; }
		}

		public long Duration
		{
			get { return _player.Duration; }
		}

		public int BufferedPercentage
		{
			get { return _player.BufferedPercentage; }
		}

		internal Looper PlaybackLooper
		{
			get { return _player.PlaybackLooper; }
		}

		internal Handler MainHandler
		{
			get { return _mainHandler; }
		}

		public void OnPlayerStateChanged(bool playWhenReady, int state)
		{
			MaybeReportPlayerState();
		}

		public void OnPlayerError(ExoPlaybackException exception)
		{
			_rendererBuildingState = RendererBuildingStateIdle;
			foreach (var listener  in _listeners)
			{
				listener.OnError(exception);
			}
		}

		public void OnVideoSizeChanged(
			int width,
			int height,
			int unappliedRotationDegrees,
			float pixelWidthHeightRatio)
		{
			foreach (var listener in _listeners)
			{
				listener.OnVideoSizeChanged(width, height, unappliedRotationDegrees, pixelWidthHeightRatio);
			}
		}

		public void OnDroppedFrames(int count, long elapsed)
		{
			if (_infoListener != null)
			{
				_infoListener.OnDroppedFrames(count, elapsed);
			}
		}

		public void OnBandwidthSample(int elapsedMs, long bytes, long bitrateEstimate)
		{
			if (_infoListener != null)
			{
				_infoListener.OnBandwidthSample(elapsedMs, bytes, bitrateEstimate);
			}
		}

		public void OnDownstreamFormatChanged(
			int sourceId,
			Format format,
			int trigger,
			long mediaTimeMs)
		{
			if (_infoListener == null)
			{
				return;
			}
			if (sourceId == TypeVideo)
			{
				_videoFormat = format;
				_infoListener.OnVideoFormatEnabled(format, trigger, mediaTimeMs);
			}
			else if (sourceId == TypeAudio)
			{
				_infoListener.OnAudioFormatEnabled(format, trigger, mediaTimeMs);
			}
		}

		public void OnDrmKeysLoaded()
		{
			// Do nothing.
		}

		public void OnDrmSessionManagerError(Exception e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnDrmSessionManagerError(e);
			}
		}

		public void OnDecoderInitializationError(MediaCodecTrackRenderer.DecoderInitializationException e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnDecoderInitializationError(e);
			}
		}

		public void OnAudioTrackInitializationError(Com.Google.Android.Exoplayer.Audio.AudioTrack.InitializationException e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnAudioTrackInitializationError(e);
			}
		}

		public void OnAudioTrackWriteError(Com.Google.Android.Exoplayer.Audio.AudioTrack.WriteException e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnAudioTrackWriteError(e);
			}
		}

		public void OnCryptoError(MediaCodec.CryptoException e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnCryptoError(e);
			}
		}

		public void OnDecoderInitialized(
			string decoderName,
			long elapsedRealtimeMs,
			long initializationDurationMs)
		{
			if (_infoListener != null)
			{
				_infoListener.OnDecoderInitialized(decoderName, elapsedRealtimeMs, initializationDurationMs);
			}
		}

		public void OnLoadError(int sourceId, IOException e)
		{
			if (_internalErrorListener != null)
			{
				_internalErrorListener.OnLoadError(sourceId, e);
			}
		}

		public void OnCues(IList<Cue> cues)
		{
			if (_captionListener != null && GetSelectedTrack(TypeText) != TrackDisabled)
			{
				_captionListener.OnCues(cues);
			}
		}

		public void OnMetadata(Object metadata)
		{
			if (_id3MetadataListener != null && GetSelectedTrack(TypeMetadata) != TrackDisabled)
			{
				_id3MetadataListener.OnId3Metadata(metadata);
			}
		}

		public void OnAvailableRangeChanged(ITimeRange availableRange)
		{
			if (_infoListener != null)
			{
				_infoListener.OnAvailableRangeChanged(availableRange);
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
			if (_infoListener != null)
			{
				_infoListener.OnLoadStarted(sourceId, length, type, trigger, format, mediaStartTimeMs,
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
			if (_infoListener != null)
			{
				_infoListener.OnLoadCompleted(sourceId, bytesLoaded, type, trigger, format, mediaStartTimeMs,
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
			var playWhenReady = _player.PlayWhenReady;
			var playbackState = PlaybackState;
			if (_lastReportedPlayWhenReady != playWhenReady || _lastReportedPlaybackState != playbackState)
			{
				foreach (var listener in _listeners)
				{
					listener.OnStateChanged(playWhenReady, playbackState);
				}
				_lastReportedPlayWhenReady = playWhenReady;
				_lastReportedPlaybackState = playbackState;
			}
		}

		private void PushSurface(bool blockForSurfacePush)
		{
			if (_videoRenderer == null)
			{
				return;
			}

			if (blockForSurfacePush)
			{
				_player.BlockingSendMessage(
					_videoRenderer, MediaCodecVideoTrackRenderer.MsgSetSurface, _surface);
			}
			else
			{
				_player.SendMessage(
					_videoRenderer, MediaCodecVideoTrackRenderer.MsgSetSurface, _surface);
			}
		}

		public void OnAudioTrackUnderrun (int p0, long p1, long p2)
		{
            // Do nothing.
        }

        public void OnAvailableRangeChanged(int p0, ITimeRange p1)
	    {
            // Do nothing.
        }
    }
}