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

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.Drm;
using Com.Google.Android.Exoplayer2.Ext.Ima;
using Com.Google.Android.Exoplayer2.Extractor;
using Com.Google.Android.Exoplayer2.Mediacodec;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Source.Dash;
using Com.Google.Android.Exoplayer2.Source.Hls;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Upstream;
using Java.Lang;
using Java.Net;
using Java.Util;
using Uri = Android.Net.Uri;

namespace Com.Google.Android.Exoplayer2.Demo
{
	/**
	 * An activity that plays media using {@link SimpleExoPlayer}.
	 */
	public class PlayerActivity : Activity, View.IOnClickListener, IPlayerEventListener,
		PlaybackControlView.IVisibilityListener
	{

		public const string DRM_SCHEME_UUID_EXTRA = "drm_scheme_uuid";
		public const string DRM_LICENSE_URL = "drm_license_url";
		public const string DRM_KEY_REQUEST_PROPERTIES = "drm_key_request_properties";
		public const string PREFER_EXTENSION_DECODERS = "prefer_extension_decoders";

		public const string ACTION_VIEW = "com.google.android.exoplayer.demo.action.VIEW";
		public const string EXTENSION_EXTRA = "extension";

		public const string ACTION_VIEW_LIST =
			"com.google.android.exoplayer.demo.action.VIEW_LIST";
		public const string URI_LIST_EXTRA = "uri_list";
		public const string EXTENSION_LIST_EXTRA = "extension_list";
		public const string AD_TAG_URI_EXTRA = "ad_tag_uri";

		private static readonly DefaultBandwidthMeter BANDWIDTH_METER = new DefaultBandwidthMeter();
		private static readonly CookieManager DEFAULT_COOKIE_MANAGER;
		static PlayerActivity()
		{
			DEFAULT_COOKIE_MANAGER = new CookieManager();
			DEFAULT_COOKIE_MANAGER.SetCookiePolicy(CookiePolicy.AcceptOriginalServer);
		}

		private Handler mainHandler;
		private EventLogger eventLogger;
		private SimpleExoPlayerView simpleExoPlayerView;
		private LinearLayout debugRootView;
		private TextView debugTextView;
		private Button retryButton;

		private IDataSourceFactory mediaDataSourceFactory;
		private SimpleExoPlayer player;
		private DefaultTrackSelector trackSelector;
		private TrackSelectionHelper trackSelectionHelper;
		private DebugTextViewHelper debugViewHelper;
		private bool inErrorState;
		private TrackGroupArray lastSeenTrackGroupArray;

		private bool shouldAutoPlay;
		private int resumeWindow;
		private long resumePosition;

		// Fields used only for ad playback. The ads loader is loaded via reflection.

		private ImaAdsLoader imaAdsLoader; // com.google.android.exoplayer2.ext.ima.ImaAdsLoader
		private Uri loadedAdTagUri;
		private ViewGroup adOverlayViewGroup;

		// Activity lifecycle

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			shouldAutoPlay = true;
			ClearResumePosition();
			mediaDataSourceFactory = BuildDataSourceFactory(true);
			mainHandler = new Handler();
			if (CookieHandler.Default != DEFAULT_COOKIE_MANAGER)
			{
				CookieHandler.Default = DEFAULT_COOKIE_MANAGER;
			}

			SetContentView(Resource.Layout.player_activity);
			var rootView = FindViewById(Resource.Id.root);
			rootView.SetOnClickListener(this);
			debugRootView = FindViewById<LinearLayout>(Resource.Id.controls_root);
			debugTextView = FindViewById<TextView>(Resource.Id.debug_text_view);
			retryButton = FindViewById<Button>(Resource.Id.retry_button);
			retryButton.SetOnClickListener(this);

			simpleExoPlayerView = FindViewById<SimpleExoPlayerView>(Resource.Id.player_view);
			simpleExoPlayerView.SetControllerVisibilityListener(this);
			simpleExoPlayerView.RequestFocus();
		}

		protected override void OnNewIntent(Intent intent)
		{
			ReleasePlayer();
			shouldAutoPlay = true;
			ClearResumePosition();
			Intent = intent;
		}

		protected override void OnStart()
		{
			base.OnStart();
			if (Util.Util.SdkInt > 23)
			{
				initializePlayer();
			}
		}

		protected override void OnResume()
		{
			base.OnResume();
			if ((Util.Util.SdkInt <= 23 || player == null))
			{
				initializePlayer();
			}
		}

		protected override void OnPause()
		{
			base.OnPause();
			if (Util.Util.SdkInt <= 23)
			{
				ReleasePlayer();
			}
		}

		protected override void OnStop()
		{
			base.OnStop();
			if (Util.Util.SdkInt > 23)
			{
				ReleasePlayer();
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			ReleaseAdsLoader();
		}

		public void OnRequestPermissionsResult(int requestCode, string[] permissions, int[] grantResults)
		{
			if (grantResults.Length > 0 && grantResults[0] == (int)Permission.Granted)
			{
				initializePlayer();
			}
			else
			{
				ShowToast(Resource.String.storage_permission_denied);
				Finish();
			}
		}

		// Activity input

		public override bool DispatchKeyEvent(KeyEvent ev)
		{
			// If the event was not handled then see if the player view can handle it.
			return base.DispatchKeyEvent(ev) || simpleExoPlayerView.DispatchKeyEvent(ev);
		}

		// OnClickListener methods

		public void OnClick(View view)
		{
			if (view == retryButton)
			{
				initializePlayer();
			}
			else if (view.Parent == debugRootView)
			{
				var mappedTrackInfo = trackSelector.CurrentMappedTrackInfo;
				if (mappedTrackInfo != null)
				{
					trackSelectionHelper.showSelectionDialog(this, ((Button)view).Text,
						trackSelector.CurrentMappedTrackInfo, (int)view.Tag);
				}
			}
		}

		// PlaybackControlView.VisibilityListener implementation

		public void OnVisibilityChange(int visibility)
		{
			debugRootView.Visibility = (ViewStates)visibility;
		}

		// Internal methods

		private void initializePlayer()
		{
			Intent intent = Intent;
			bool needNewPlayer = player == null;
			if (needNewPlayer)
			{
				var adaptiveTrackSelectionFactory =
					new AdaptiveTrackSelection.Factory(BANDWIDTH_METER);
				trackSelector = new DefaultTrackSelector(adaptiveTrackSelectionFactory);
				trackSelectionHelper = new TrackSelectionHelper(trackSelector, adaptiveTrackSelectionFactory);
				lastSeenTrackGroupArray = null;
				eventLogger = new EventLogger(trackSelector);

				var drmSchemeUuid = intent.HasExtra(DRM_SCHEME_UUID_EXTRA)
					? UUID.FromString(intent.GetStringExtra(DRM_SCHEME_UUID_EXTRA)) : null;
				IDrmSessionManager drmSessionManager = null;
				if (drmSchemeUuid != null)
				{
					var drmLicenseUrl = intent.GetStringExtra(DRM_LICENSE_URL);
					var keyRequestPropertiesArray = intent.GetStringArrayExtra(DRM_KEY_REQUEST_PROPERTIES);
					int errorStringId = Resource.String.error_drm_unknown;
					if (Util.Util.SdkInt < 18)
					{
						errorStringId = Resource.String.error_drm_not_supported;
					}
					else
					{
						try
						{
							drmSessionManager = BuildDrmSessionManagerV18(drmSchemeUuid, drmLicenseUrl,
								keyRequestPropertiesArray);
						}
						catch (UnsupportedDrmException e)
						{
							errorStringId = e.Reason == UnsupportedDrmException.ReasonUnsupportedScheme
								? Resource.String.error_drm_unsupported_scheme : Resource.String.error_drm_unknown;
						}
					}
					if (drmSessionManager == null)
					{
						ShowToast(errorStringId);
						return;
					}
				}

				var preferExtensionDecoders = intent.GetBooleanExtra(PREFER_EXTENSION_DECODERS, false);
				var extensionRendererMode =
					((DemoApplication)Application).UseExtensionRenderers()
						? (preferExtensionDecoders ? DefaultRenderersFactory.ExtensionRendererModePrefer
						: DefaultRenderersFactory.ExtensionRendererModeOn)
						: DefaultRenderersFactory.ExtensionRendererModeOff;
				var renderersFactory = new DefaultRenderersFactory(this,
					drmSessionManager, extensionRendererMode);

				player = ExoPlayerFactory.NewSimpleInstance(renderersFactory, trackSelector);
				player.AddListener(this);
				player.AddListener(eventLogger);
				player.SetAudioDebugListener(eventLogger);
				player.SetVideoDebugListener(eventLogger);
				player.SetMetadataOutput(eventLogger);

				simpleExoPlayerView.Player = player;
				player.PlayWhenReady = shouldAutoPlay;
				debugViewHelper = new DebugTextViewHelper(player, debugTextView);
				debugViewHelper.Start();
			}
			var action = intent.Action;
			Uri[] uris;
			string[] extensions;
			if (ACTION_VIEW.Equals(action))
			{
				uris = new Uri[] { intent.Data };
				extensions = new string[] { intent.GetStringExtra(EXTENSION_EXTRA) };
			}
			else if (ACTION_VIEW_LIST.Equals(action))
			{
				var uriStrings = intent.GetStringArrayExtra(URI_LIST_EXTRA);
				uris = new Uri[uriStrings.Length];
				for (int i = 0; i < uriStrings.Length; i++)
				{
					uris[i] = Uri.Parse(uriStrings[i]);
				}
				extensions = intent.GetStringArrayExtra(EXTENSION_LIST_EXTRA);
				if (extensions == null)
				{
					extensions = new string[uriStrings.Length];
				}
			}
			else
			{
				ShowToast(GetString(Resource.String.unexpected_intent_action, action));
				return;
			}
			if (Util.Util.MaybeRequestReadExternalStoragePermission(this, uris))
			{
				// The player will be reinitialized if the permission is granted.
				return;
			}
			var mediaSources = new IMediaSource[uris.Length];
			for (var i = 0; i < uris.Length; i++)
			{
				mediaSources[i] = BuildMediaSource(uris[i], extensions[i]);
			}
			var mediaSource = mediaSources.Length == 1 ? mediaSources[0]
				: new ConcatenatingMediaSource(mediaSources);
			var adTagUriString = intent.GetStringExtra(AD_TAG_URI_EXTRA);
			if (adTagUriString != null)
			{
				Uri adTagUri = Uri.Parse(adTagUriString);
				if (!adTagUri.Equals(loadedAdTagUri))
				{
					ReleaseAdsLoader();
					loadedAdTagUri = adTagUri;
				}
				try
				{
					mediaSource = CreateAdsMediaSource(mediaSource, Uri.Parse(adTagUriString));
				}
				catch (System.Exception e)
				{
					ShowToast(Resource.String.ima_not_loaded);
				}
			}
			else
			{
				ReleaseAdsLoader();
			}
			bool haveResumePosition = resumeWindow != C.IndexUnset;
			if (haveResumePosition)
			{
				player.SeekTo(resumeWindow, resumePosition);
			}
			player.Prepare(mediaSource, !haveResumePosition, false);
			inErrorState = false;
			UpdateButtonVisibilities();
		}

		private IMediaSource BuildMediaSource(Uri uri, string overrideExtension)
		{
			int type = TextUtils.IsEmpty(overrideExtension) ? Util.Util.InferContentType(uri)
				: Util.Util.InferContentType("." + overrideExtension);
			switch (type)
			{
				case C.TypeSs:
					return new SsMediaSource(uri, BuildDataSourceFactory(false),
						new DefaultSsChunkSource.Factory(mediaDataSourceFactory), mainHandler, eventLogger);
				case C.TypeDash:
					return new DashMediaSource(uri, BuildDataSourceFactory(false),
						new DefaultDashChunkSource.Factory(mediaDataSourceFactory), mainHandler, eventLogger);
				case C.TypeHls:
					return new HlsMediaSource(uri, mediaDataSourceFactory, mainHandler, eventLogger);
				case C.TypeOther:
					return new ExtractorMediaSource(uri, mediaDataSourceFactory, new DefaultExtractorsFactory(),
						mainHandler, eventLogger);
				default:
					{
						throw new IllegalStateException("Unsupported type: " + type);
					}
			}
		}

		private IDrmSessionManager BuildDrmSessionManagerV18(UUID uuid, string licenseUrl, string[] keyRequestPropertiesArray)
		{
			HttpMediaDrmCallback drmCallback = new HttpMediaDrmCallback(licenseUrl,
				BuildHttpDataSourceFactory(false));
			if (keyRequestPropertiesArray != null)
			{
				for (int i = 0; i < keyRequestPropertiesArray.Length - 1; i += 2)
				{
					drmCallback.SetKeyRequestProperty(keyRequestPropertiesArray[i],
						keyRequestPropertiesArray[i + 1]);
				}
			}
			return new DefaultDrmSessionManager(uuid, FrameworkMediaDrm.NewInstance(uuid), drmCallback,
				null, mainHandler, eventLogger);
		}

		private void ReleasePlayer()
		{
			if (player != null)
			{
				debugViewHelper.Stop();
				debugViewHelper = null;
				shouldAutoPlay = player.PlayWhenReady;
				UpdateResumePosition();
				player.Release();
				player = null;
				trackSelector = null;
				trackSelectionHelper = null;
				eventLogger = null;
			}
		}

		private void UpdateResumePosition()
		{
			resumeWindow = player.CurrentWindowIndex;
			resumePosition = System.Math.Max(0, player.ContentPosition);
		}

		private void ClearResumePosition()
		{
			resumeWindow = C.IndexUnset;
			resumePosition = C.TimeUnset;
		}

		/**
		 * Returns a new DataSource factory.
		 *
		 * @param useBandwidthMeter Whether to set {@link #BANDWIDTH_METER} as a listener to the new
		 *     DataSource factory.
		 * @return A new DataSource factory.
		 */
		private IDataSourceFactory BuildDataSourceFactory(bool useBandwidthMeter)
		{
			return ((DemoApplication)Application)
				.BuildDataSourceFactory(useBandwidthMeter ? BANDWIDTH_METER : null);
		}

		/**
		 * Returns a new HttpDataSource factory.
		 *
		 * @param useBandwidthMeter Whether to set {@link #BANDWIDTH_METER} as a listener to the new
		 *     DataSource factory.
		 * @return A new HttpDataSource factory.
		 */
		private IHttpDataSourceFactory BuildHttpDataSourceFactory(bool useBandwidthMeter)
		{
			return ((DemoApplication)Application)
				.BuildHttpDataSourceFactory(useBandwidthMeter ? BANDWIDTH_METER : null);
		}

		/**
		 * Returns an ads media source, reusing the ads loader if one exists.
		 *
		 * @throws Exception Thrown if it was not possible to create an ads media source, for example, due
		 *     to a missing dependency.
		 */
		private IMediaSource CreateAdsMediaSource(IMediaSource mediaSource, Uri adTagUri)
		{
			if (imaAdsLoader == null)
			{
				imaAdsLoader = new ImaAdsLoader(this, adTagUri);
				adOverlayViewGroup = new FrameLayout(this);
				// The demo app has a non-null overlay frame layout.
				simpleExoPlayerView.OverlayFrameLayout.AddView(adOverlayViewGroup);
			}
			return new ImaAdsMediaSource(mediaSource, mediaDataSourceFactory, imaAdsLoader, adOverlayViewGroup);
		}

		private void ReleaseAdsLoader()
		{
			if (imaAdsLoader != null)
			{
				imaAdsLoader.Release();
				imaAdsLoader = null;
				loadedAdTagUri = null;
				simpleExoPlayerView.OverlayFrameLayout.RemoveAllViews();
			}
		}

		// Player.EventListener implementation

		public void OnLoadingChanged(bool isLoading)
		{
			// Do nothing.
		}

		public void OnPlayerStateChanged(bool playWhenReady, int playbackState)
		{
			if (playbackState == Player.StateEnded)
			{
				ShowControls();
			}
			UpdateButtonVisibilities();
		}

		public void OnRepeatModeChanged(int repeatMode)
		{
			// Do nothing.
		}

		public void OnPositionDiscontinuity()
		{
			if (inErrorState)
			{
				// This will only occur if the user has performed a seek whilst in the error state. Update the
				// resume position so that if the user then retries, playback will resume from the position to
				// which they seeked.
				UpdateResumePosition();
			}
		}

		public void OnPlaybackParametersChanged(PlaybackParameters playbackParameters)
		{
			// Do nothing.
		}

		public void OnTimelineChanged(Timeline timeline, Java.Lang.Object manifest)
		{
			// Do nothing.
		}

		public void OnPlayerError(ExoPlaybackException e)
		{
			string errorString = null;
			if (e.Type == ExoPlaybackException.TypeRenderer)
			{
				var cause = e.RendererException;
				if (cause is MediaCodecRenderer.DecoderInitializationException)
				{
					// Special case for decoder initialization failures.
					var decoderInitializationException =
						(MediaCodecRenderer.DecoderInitializationException)cause;
					if (decoderInitializationException.DecoderName == null)
					{
						if (decoderInitializationException.Cause is MediaCodecUtil.DecoderQueryException)
						{
							errorString = GetString(Resource.String.error_querying_decoders);
						}
						else if (decoderInitializationException.SecureDecoderRequired)
						{
							errorString = GetString(Resource.String.error_no_secure_decoder,
								decoderInitializationException.MimeType);
						}
						else
						{
							errorString = GetString(Resource.String.error_no_decoder,
								decoderInitializationException.MimeType);
						}
					}
					else
					{
						errorString = GetString(Resource.String.error_instantiating_decoder,
							decoderInitializationException.DecoderName);
					}
				}
			}
			if (errorString != null)
			{
				ShowToast(errorString);
			}
			inErrorState = true;
			if (IsBehindLiveWindow(e))
			{
				ClearResumePosition();
				initializePlayer();
			}
			else
			{
				UpdateResumePosition();
				UpdateButtonVisibilities();
				ShowControls();
			}
		}

		public void OnTracksChanged(TrackGroupArray trackGroups, TrackSelectionArray trackSelections)
		{
			UpdateButtonVisibilities();
			if (trackGroups != lastSeenTrackGroupArray)
			{
				var mappedTrackInfo = trackSelector.CurrentMappedTrackInfo;
				if (mappedTrackInfo != null)
				{
					if (mappedTrackInfo.GetTrackTypeRendererSupport(C.TrackTypeVideo)
						== MappingTrackSelector.MappedTrackInfo.RendererSupportUnsupportedTracks)
					{
						ShowToast(Resource.String.error_unsupported_video);
					}
					if (mappedTrackInfo.GetTrackTypeRendererSupport(C.TrackTypeAudio)
						== MappingTrackSelector.MappedTrackInfo.RendererSupportUnsupportedTracks)
					{
						ShowToast(Resource.String.error_unsupported_audio);
					}
				}
				lastSeenTrackGroupArray = trackGroups;
			}
		}

		// User controls

		private void UpdateButtonVisibilities()
		{
			debugRootView.RemoveAllViews();

			retryButton.Visibility = inErrorState ? ViewStates.Visible : ViewStates.Gone;
			debugRootView.AddView(retryButton);

			if (player == null)
			{
				return;
			}

			var mappedTrackInfo = trackSelector.CurrentMappedTrackInfo;
			if (mappedTrackInfo == null)
			{
				return;
			}

			for (int i = 0; i < mappedTrackInfo.Length; i++)
			{
				var trackGroups = mappedTrackInfo.GetTrackGroups(i);
				if (trackGroups.Length != 0)
				{
					Button button = new Button(this);
					int label;
					switch (player.GetRendererType(i))
					{
						case C.TrackTypeAudio:
							label = Resource.String.audio;
							break;
						case C.TrackTypeVideo:
							label = Resource.String.video;
							break;
						case C.TrackTypeText:
							label = Resource.String.text;
							break;
						default:
							continue;
					}
					button.SetText(label);
					button.Tag = i;
					button.SetOnClickListener(this);
					debugRootView.AddView(button, debugRootView.ChildCount - 1);
				}
			}
		}

		private void ShowControls()
		{
			debugRootView.Visibility = ViewStates.Visible;
		}

		private void ShowToast(int messageId)
		{
			ShowToast(GetString(messageId));
		}

		private void ShowToast(string message)
		{
			Toast.MakeText(ApplicationContext, message, ToastLength.Long).Show();
		}

		private static bool IsBehindLiveWindow(ExoPlaybackException e)
		{
			if (e.Type != ExoPlaybackException.TypeSource)
			{
				return false;
			}
			Throwable cause = e.SourceException;
			while (cause != null)
			{
				if (cause is BehindLiveWindowException)
				{
					return true;
				}
				cause = cause.Cause;
			}
			return false;
		}

	}
}