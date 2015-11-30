using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.Accessibility;
using Android.Widget;
using Com.Google.Android.Exoplayer.Audio;
using Com.Google.Android.Exoplayer.Demo.Player;
using Com.Google.Android.Exoplayer.Drm;
using Com.Google.Android.Exoplayer.Text;
using Com.Google.Android.Exoplayer.Util;
using Java.Interop;
using Java.Lang;
using Java.Net;
using Java.Util;
using Exception = Java.Lang.Exception;
using String = Java.Lang.String;
using Uri = Android.Net.Uri;

namespace Com.Google.Android.Exoplayer.Demo
{
	/**
 * An activity that plays media using {@link DemoPlayer}.
 */

	[Activity(
		Name = "com.google.android.exoplayer.demo.PlayerActivity",
		ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Keyboard | ConfigChanges.Orientation | ConfigChanges.ScreenSize,
		LaunchMode = LaunchMode.SingleInstance,
		Label = "@string/application_name",
		Theme = "@style/PlayerTheme"
		)]
	[IntentFilter(new[] {"com.google.android.exoplayer.demo.action.VIEW"}, Categories = new[] {"android.intent.category.DEFAULT"}, DataScheme = "http")]
	public class PlayerActivity : Activity, ISurfaceHolderCallback, View.IOnClickListener,
		DemoPlayer.Listener, DemoPlayer.CaptionListener, DemoPlayer.Id3MetadataListener,
		AudioCapabilitiesReceiver.IListener
	{

		// For use within demo app code.
		public const string CONTENT_ID_EXTRA = "content_id";
		public const string CONTENT_TYPE_EXTRA = "content_type";
		public const int TYPE_DASH = 0;
		public const int TYPE_SS = 1;
		public const int TYPE_HLS = 2;
		public const int TYPE_OTHER = 3;

		// For use when launching the demo app using adb.
		private const string CONTENT_EXT_EXTRA = "type";
		private const string EXT_DASH = ".mpd";
		private const string EXT_SS = ".ism";
		private const string EXT_HLS = ".m3u8";

		private const string TAG = "PlayerActivity";
		private const int MENU_GROUP_TRACKS = 1;
		private const int ID_OFFSET = 2;

		private static readonly CookieManager defaultCookieManager;

		static PlayerActivity()
		{
			defaultCookieManager = new CookieManager();
			defaultCookieManager.SetCookiePolicy(CookiePolicy.AcceptOriginalServer);
		}

		private EventLogger eventLogger;
		private MediaController mediaController;
		private View debugRootView;
		private View shutterView;
		private AspectRatioFrameLayout videoFrame;
		private SurfaceView surfaceView;
		private TextView debugTextView;
		private TextView playerStateTextView;
		private SubtitleLayout subtitleLayout;
		private Button videoButton;
		private Button audioButton;
		private Button textButton;
		private Button retryButton;

		private DemoPlayer player;
		private DebugTextViewHelper debugViewHelper;
		private bool playerNeedsPrepare;

		private long playerPosition;
		private bool enableBackgroundAudio;

		private Uri contentUri;
		private int contentType;
		private string contentId;

		private AudioCapabilitiesReceiver audioCapabilitiesReceiver;

		// Activity lifecycle

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			SetContentView(Resource.Layout.player_activity);
			View root = FindViewById(Resource.Id.root);

			root.Touch += (sender, args) =>
			{
				var motionEvent = args.Event;
				if (motionEvent.Action == MotionEventActions.Down)
				{
					toggleControlsVisibility();
				}
				else if (motionEvent.Action == MotionEventActions.Up)
				{
					((View) sender).PerformClick();
				}
				args.Handled = true;
			};
			root.KeyPress += (sender, args) =>
			{
				var keyCode = args.KeyCode;
				if (keyCode == Keycode.Back || keyCode == Keycode.Escape
				    || keyCode == Keycode.Menu)
				{
					args.Handled = false;
				}
				else
				{
					mediaController.DispatchKeyEvent(args.Event);
				}
			};

			shutterView = FindViewById(Resource.Id.shutter);
			debugRootView = FindViewById(Resource.Id.controls_root);

			videoFrame = FindViewById<AspectRatioFrameLayout>(Resource.Id.video_frame);
			surfaceView = FindViewById<SurfaceView>(Resource.Id.surface_view);
			surfaceView.Holder.AddCallback(this);
			debugTextView = FindViewById<TextView>(Resource.Id.debug_text_view);

			playerStateTextView = FindViewById<TextView>(Resource.Id.player_state_view);
			subtitleLayout = FindViewById<SubtitleLayout>(Resource.Id.subtitles);

			mediaController = new MediaController(this);
			mediaController.SetAnchorView(root);
			retryButton = (Button) FindViewById(Resource.Id.retry_button);
			retryButton.SetOnClickListener(this);
			videoButton = (Button) FindViewById(Resource.Id.video_controls);
			audioButton = (Button) FindViewById(Resource.Id.audio_controls);
			textButton = (Button) FindViewById(Resource.Id.text_controls);

			CookieHandler currentHandler = CookieHandler.Default;
			if (currentHandler != defaultCookieManager)
			{
				CookieHandler.Default = defaultCookieManager;
			}

			audioCapabilitiesReceiver = new AudioCapabilitiesReceiver(this, this);
			audioCapabilitiesReceiver.Register();
		}

		protected override void OnNewIntent(Intent intent)
		{
			releasePlayer();
			playerPosition = 0;
			Intent = intent;
		}

		protected override void OnResume()
		{
			base.OnResume();
			Intent intent = Intent;
			contentUri = intent.Data;
			contentType = intent.GetIntExtra(CONTENT_TYPE_EXTRA,
				inferContentType(contentUri, intent.GetStringExtra(CONTENT_EXT_EXTRA)));
			contentId = intent.GetStringExtra(CONTENT_ID_EXTRA);
			configureSubtitleView();
			if (player == null)
			{
				preparePlayer(true);
			}
			else
			{
				player.setBackgrounded(false);
			}
		}

		protected override void OnPause()
		{
			base.OnPause();
			if (!enableBackgroundAudio)
			{
				releasePlayer();
			}
			else
			{
				player.setBackgrounded(true);
			}
			shutterView.Visibility = ViewStates.Visible;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			audioCapabilitiesReceiver.Unregister();
			releasePlayer();
		}

		// OnClickListener methods

		public void OnClick(View view)
		{
			if (view == retryButton)
			{
				preparePlayer(true);
			}
		}

		// AudioCapabilitiesReceiver.Listener methods

		public void OnAudioCapabilitiesChanged(AudioCapabilities audioCapabilities)
		{
			if (player == null)
			{
				return;
			}
			bool backgrounded = player.getBackgrounded();
			bool playWhenReady = player.GetPlayWhenReady();
			releasePlayer();
			preparePlayer(playWhenReady);
			player.setBackgrounded(backgrounded);
		}

		// Internal methods

		private DemoPlayer.RendererBuilder getRendererBuilder()
		{
			var userAgent = Util.Util.GetUserAgent(this, "ExoPlayerDemo");
			switch (contentType)
			{
				case TYPE_SS:
					return new SmoothStreamingRendererBuilder(this, userAgent, contentUri.ToString(),
						new SmoothStreamingTestMediaDrmCallback());
				case TYPE_DASH:
					return new DashRendererBuilder(this, userAgent, contentUri.ToString(),
						new WidevineTestMediaDrmCallback(contentId));
				case TYPE_HLS:
					return new HlsRendererBuilder(this, userAgent, contentUri.ToString());
				case TYPE_OTHER:
					return new ExtractorRendererBuilder(this, userAgent, contentUri);
				default:
					throw new IllegalStateException("Unsupported type: " + contentType);
			}
		}

		private void preparePlayer(bool playWhenReady)
		{
			if (player == null)
			{
				player = new DemoPlayer(getRendererBuilder());
				player.addListener(this);
				player.setCaptionListener(this);
				player.setMetadataListener(this);
				player.SeekTo(playerPosition);
				playerNeedsPrepare = true;
				mediaController.SetMediaPlayer(player.getPlayerControl());
				mediaController.Enabled = true;
				eventLogger = new EventLogger();
				eventLogger.startSession();
				player.addListener(eventLogger);
				player.setInfoListener(eventLogger);
				player.setInternalErrorListener(eventLogger);
				debugViewHelper = new DebugTextViewHelper(player, debugTextView);
				debugViewHelper.Start();
			}
			if (playerNeedsPrepare)
			{
				player.prepare();
				playerNeedsPrepare = false;
				updateButtonVisibilities();
			}
			player.setSurface(surfaceView.Holder.Surface);
			player.SetPlayWhenReady(playWhenReady);
		}

		private void releasePlayer()
		{
			if (player != null)
			{
				debugViewHelper.Stop();
				debugViewHelper = null;
				playerPosition = player.CurrentPosition;
				player.Release();
				player = null;
				eventLogger.endSession();
				eventLogger = null;
			}
		}

		// DemoPlayer.Listener implementation

		public void onStateChanged(bool playWhenReady, int playbackState)
		{
			if (playbackState == ExoPlayer.StateEnded)
			{
				showControls();
			}
			var text = "playWhenReady=" + playWhenReady + ", playbackState=";
			switch (playbackState)
			{
				case ExoPlayer.StateBuffering:
					text += "buffering";
					break;
				case ExoPlayer.StateEnded:
					text += "ended";
					break;
				case ExoPlayer.StateIdle:
					text += "idle";
					break;
				case ExoPlayer.StatePreparing:
					text += "preparing";
					break;
				case ExoPlayer.StateReady:
					text += "ready";
					break;
				default:
					text += "unknown";
					break;
			}
			playerStateTextView.Text = text;
			updateButtonVisibilities();
		}

		public void onError(Exception e)
		{
			if (e is UnsupportedDrmException)
			{
				// Special case DRM failures.
				UnsupportedDrmException unsupportedDrmException = (UnsupportedDrmException) e;
				int stringId = Util.Util.SdkInt < 18
					? Resource.String.drm_error_not_supported
					: unsupportedDrmException.Reason == UnsupportedDrmException.ReasonUnsupportedScheme
						? Resource.String.drm_error_unsupported_scheme
						: Resource.String.drm_error_unknown;
				Toast.MakeText(ApplicationContext, stringId, ToastLength.Long).Show();
			}
			playerNeedsPrepare = true;
			updateButtonVisibilities();
			showControls();
		}

		public void onVideoSizeChanged(
			int width,
			int height,
			int unappliedRotationDegrees,
			float pixelWidthAspectRatio)
		{
			shutterView.Visibility = ViewStates.Gone;
			videoFrame.SetAspectRatio(height == 0 ? 1 : (width*pixelWidthAspectRatio)/height);
		}

		// User controls

		private void updateButtonVisibilities()
		{
			retryButton.Visibility = playerNeedsPrepare ? ViewStates.Visible : ViewStates.Gone;
			videoButton.Visibility = haveTracks(DemoPlayer.TYPE_VIDEO) ? ViewStates.Visible : ViewStates.Gone;
			audioButton.Visibility = haveTracks(DemoPlayer.TYPE_AUDIO) ? ViewStates.Visible : ViewStates.Gone;
			textButton.Visibility = haveTracks(DemoPlayer.TYPE_TEXT) ? ViewStates.Visible : ViewStates.Gone;
		}

		private bool haveTracks(int type)
		{
			return player != null && player.getTrackCount(type) > 0;
		}

		[Export("showVideoPopup")]
		public void showVideoPopup(View v)
		{
			PopupMenu popup = new PopupMenu(this, v);
			configurePopupWithTracks(popup, null, DemoPlayer.TYPE_VIDEO);
			popup.Show();
		}

		[Export("showAudioPopup")]
		public void showAudioPopup(View v)
		{
			PopupMenu popup = new PopupMenu(this, v);
			var menu = popup.Menu;
			menu.Add(Menu.None, Menu.None, Menu.None, Resource.String.enable_background_audio);
			var backgroundAudioItem = menu.FindItem(0);
			backgroundAudioItem.SetCheckable(true);
			backgroundAudioItem.SetChecked(enableBackgroundAudio);

			Func<IMenuItem, bool> clickListener = item =>
			{
				if (item == backgroundAudioItem)
				{
					enableBackgroundAudio = !item.IsChecked;
					return true;
				}
				return false;
			};
			configurePopupWithTracks(popup, clickListener, DemoPlayer.TYPE_AUDIO);
			popup.Show();
		}

		[Export("showTextPopup")]
		public void showTextPopup(View v)
		{
			PopupMenu popup = new PopupMenu(this, v);
			configurePopupWithTracks(popup, null, DemoPlayer.TYPE_TEXT);
			popup.Show();
		}

		[Export("showVerboseLogPopup")]
		public void showVerboseLogPopup(View v)
		{
			PopupMenu popup = new PopupMenu(this, v);
			var menu = popup.Menu;
			menu.Add(Menu.None, 0, Menu.None, Resource.String.logging_normal);
			menu.Add(Menu.None, 1, Menu.None, Resource.String.logging_verbose);
			menu.SetGroupCheckable(Menu.None, true, true);
			menu.FindItem((VerboseLogUtil.AreAllTagsEnabled()) ? 1 : 0).SetChecked(true);

			popup.MenuItemClick += (sender, args) =>
			{
				var item = args.Item;
				VerboseLogUtil.SetEnableAllTags(item.ItemId != 0);
			};
			popup.Show();
		}

		private void configurePopupWithTracks(PopupMenu popup, Func<IMenuItem, bool> customActionClickListener, int trackType)
		{
			if (player == null)
			{
				return;
			}
			int trackCount = player.getTrackCount(trackType);
			if (trackCount == 0)
			{
				return;
			}

			popup.MenuItemClick += (sender, args) =>
			{
				var item = args.Item;
				args.Handled = (customActionClickListener != null
				                && customActionClickListener(item))
				               || onTrackItemClick(item, trackType);
			};

			var menu = popup.Menu;
			// ID_OFFSET ensures we avoid clashing with Menu.NONE (which equals 0)
			menu.Add(MENU_GROUP_TRACKS, DemoPlayer.TRACK_DISABLED + ID_OFFSET, Menu.None, Resource.String.off);
			for (int i = 0; i < trackCount; i++)
			{
				menu.Add(MENU_GROUP_TRACKS, i + ID_OFFSET, Menu.None,
					buildTrackName(player.getTrackFormat(trackType, i)));
			}
			menu.SetGroupCheckable(MENU_GROUP_TRACKS, true, true);
			menu.FindItem(player.getSelectedTrack(trackType) + ID_OFFSET).SetChecked(true);
		}

		private static string buildTrackName(MediaFormat format)
		{
			if (format.Adaptive)
			{
				return "auto";
			}
			string trackName;
			if (MimeTypes.IsVideo(format.MimeType))
			{
				trackName = joinWithSeparator(joinWithSeparator(buildResolutionString(format),
					buildBitrateString(format)), buildTrackIdString(format));
			}
			else if (MimeTypes.IsAudio(format.MimeType))
			{
				trackName = joinWithSeparator(joinWithSeparator(joinWithSeparator(buildLanguageString(format),
					buildAudioPropertyString(format)), buildBitrateString(format)),
					buildTrackIdString(format));
			}
			else
			{
				trackName = joinWithSeparator(joinWithSeparator(buildLanguageString(format),
					buildBitrateString(format)), buildTrackIdString(format));
			}
			return trackName.Length == 0 ? "unknown" : trackName;
		}

		private static string buildResolutionString(MediaFormat format)
		{
			return format.Width == MediaFormat.NoValue || format.Height == MediaFormat.NoValue
				? ""
				: format.Width + "x" + format.Height;
		}

		private static string buildAudioPropertyString(MediaFormat format)
		{
			return format.ChannelCount == MediaFormat.NoValue || format.SampleRate == MediaFormat.NoValue
				? ""
				: format.ChannelCount + "ch, " + format.SampleRate + "Hz";
		}

		private static string buildLanguageString(MediaFormat format)
		{
			return TextUtils.IsEmpty(format.Language) || "und".Equals(format.Language)
				? ""
				: format.Language;
		}

		private static string buildBitrateString(MediaFormat format)
		{
			return format.Bitrate == MediaFormat.NoValue
				? ""
				: String.Format(Locale.Us, "%.2fMbit", format.Bitrate/1000000f);
		}

		private static string joinWithSeparator(string first, string second)
		{
			return first.Length == 0 ? second : (second.Length == 0 ? first : first + ", " + second);
		}

		private static string buildTrackIdString(MediaFormat format)
		{
			return format.TrackId == MediaFormat.NoValue
				? ""
				: String.Format(Locale.Us, " (%d)", format.TrackId);
		}

		private bool onTrackItemClick(IMenuItem item, int type)
		{
			if (player == null || item.GroupId != MENU_GROUP_TRACKS)
			{
				return false;
			}
			player.setSelectedTrack(type, item.ItemId - ID_OFFSET);
			return true;
		}

		private void toggleControlsVisibility()
		{
			if (mediaController.IsShowing)
			{
				mediaController.Hide();
				debugRootView.Visibility = ViewStates.Gone;
			}
			else
			{
				showControls();
			}
		}

		private void showControls()
		{
			mediaController.Show(0);
			debugRootView.Visibility = ViewStates.Visible;
		}

		// DemoPlayer.CaptionListener implementation

		public void onCues(IList<Cue> cues)
		{
			subtitleLayout.SetCues(cues);
		}

		// DemoPlayer.MetadataListener implementation

		public void onId3Metadata(object metadata)
		{
			/*for (Map.Entry<String, Object> entry : metadata.entrySet()) {
      if (TxxxMetadata.TYPE.equals(entry.getKey())) {
        TxxxMetadata txxxMetadata = (TxxxMetadata) entry.getValue();
        Log.i(TAG, String.format("ID3 TimedMetadata %s: description=%s, value=%s",
            TxxxMetadata.TYPE, txxxMetadata.description, txxxMetadata.value));
      } else if (PrivMetadata.TYPE.equals(entry.getKey())) {
        PrivMetadata privMetadata = (PrivMetadata) entry.getValue();
        Log.i(TAG, String.format("ID3 TimedMetadata %s: owner=%s",
            PrivMetadata.TYPE, privMetadata.owner));
      } else if (GeobMetadata.TYPE.equals(entry.getKey())) {
        GeobMetadata geobMetadata = (GeobMetadata) entry.getValue();
        Log.i(TAG, String.format("ID3 TimedMetadata %s: mimeType=%s, filename=%s, description=%s",
            GeobMetadata.TYPE, geobMetadata.mimeType, geobMetadata.filename,
            geobMetadata.description));
      } else {
        Log.i(TAG, String.format("ID3 TimedMetadata %s", entry.getKey()));
      }
    }*/
		}

		// SurfaceHolder.Callback implementation

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			if (player != null)
			{
				player.setSurface(holder.Surface);
			}
		}

		public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
		{
			// Do nothing.
		}

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			if (player != null)
			{
				player.blockingClearSurface();
			}
		}

		private void configureSubtitleView()
		{
			CaptionStyleCompat style;
			float fontScale;
			if (Util.Util.SdkInt >= 19)
			{
				style = getUserCaptionStyleV19();
				fontScale = getUserCaptionFontScaleV19();
			}
			else
			{
				style = CaptionStyleCompat.Default;
				fontScale = 1.0f;
			}
			subtitleLayout.SetStyle(style);
			subtitleLayout.SetFractionalTextSize(SubtitleLayout.DefaultTextSizeFraction*fontScale);
		}

		private float getUserCaptionFontScaleV19()
		{
			CaptioningManager captioningManager =
				(CaptioningManager) GetSystemService(Context.CaptioningService);
			return captioningManager.FontScale;
		}

		private CaptionStyleCompat getUserCaptionStyleV19()
		{
			CaptioningManager captioningManager =
				(CaptioningManager) GetSystemService(Context.CaptioningService);
			return CaptionStyleCompat.CreateFromCaptionStyle(captioningManager.UserStyle);
		}

		/**
   * Makes a best guess to infer the type from a media {@link Uri} and an optional overriding file
   * extension.
   *
   * @param uri The {@link Uri} of the media.
   * @param fileExtension An overriding file extension.
   * @return The inferred type.
   */

		private static int inferContentType(Uri uri, string fileExtension)
		{
			string lastPathSegment = !string.IsNullOrEmpty(fileExtension)
				? "." + fileExtension
				: uri.LastPathSegment;
			if (lastPathSegment == null)
			{
				return TYPE_OTHER;
			}
			if (lastPathSegment.EndsWith(EXT_DASH))
			{
				return TYPE_DASH;
			}
			if (lastPathSegment.EndsWith(EXT_SS))
			{
				return TYPE_SS;
			}
			if (lastPathSegment.EndsWith(EXT_HLS))
			{
				return TYPE_HLS;
			}
			return TYPE_OTHER;
		}
	}
}