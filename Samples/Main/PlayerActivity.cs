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
using android = Android;
using Android.App;
using Android.Content;
using Android.Util;
using Java.Lang;
using static Com.Google.Android.Exoplayer2.Mediacodec.MediaCodecRenderer;
using static Com.Google.Android.Exoplayer2.Mediacodec.MediaCodecUtil;
using Utils = Com.Google.Android.Exoplayer2.Util.Util;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Trackselection;
using static Com.Google.Android.Exoplayer2.Trackselection.MappingTrackSelector;
using Android.Widget;
using Android.Views;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Drm;
using Java.Util;
using System.Collections.Generic;
using Com.Google.Android.Exoplayer2.Source.Dash;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming;
using Com.Google.Android.Exoplayer2.Source.Hls;
using Com.Google.Android.Exoplayer2.Source.Dash.Manifest;
using Com.Google.Android.Exoplayer2.Offline;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming.Manifest;
using Com.Google.Android.Exoplayer2.Source.Hls.Playlist;
using Com.Google.Android.Exoplayer2.Util;
using Com.Google.Android.Exoplayer2.UI;
using Java.Net;
using Com.Google.Android.Exoplayer2.Source.Ads;
using Android.OS;
using Android.Runtime;
using Android.Content.PM;
using Java.Lang.Reflect;
using Android.Net;

namespace Com.Google.Android.Exoplayer2.Demo
{
    /** An activity that plays media using {@link SimpleExoPlayer}. */
    public class PlayerActivity : Activity, View.IOnClickListener, IPlaybackPreparer, PlayerControlView.IVisibilityListener
    {
        public static string DRM_SCHEME_EXTRA = "drm_scheme";
        public static string DRM_LICENSE_URL_EXTRA = "drm_license_url";
        public static string DRM_KEY_REQUEST_PROPERTIES_EXTRA = "drm_key_request_properties";
        public static string DRM_MULTI_SESSION_EXTRA = "drm_multi_session";
        public static string PREFER_EXTENSION_DECODERS_EXTRA = "prefer_extension_decoders";

        public static string ACTION_VIEW = "com.google.android.exoplayer.demo.action.VIEW";
        public static string EXTENSION_EXTRA = "extension";

        public static string ACTION_VIEW_LIST =
            "com.google.android.exoplayer.demo.action.VIEW_LIST";
        public static string URI_LIST_EXTRA = "uri_list";
        public static string EXTENSION_LIST_EXTRA = "extension_list";

        public static string AD_TAG_URI_EXTRA = "ad_tag_uri";

        public static string ABR_ALGORITHM_EXTRA = "abr_algorithm";
        private static string ABR_ALGORITHM_DEFAULT = "default";
        private static string ABR_ALGORITHM_RANDOM = "random";

        // For backwards compatibility only.
        private static string DRM_SCHEME_UUID_EXTRA = "drm_scheme_uuid";

        // Saved instance state keys.
        private static string KEY_TRACK_SELECTOR_PARAMETERS = "track_selector_parameters";
        private static string KEY_WINDOW = "window";
        private static string KEY_POSITION = "position";
        private static string KEY_AUTO_PLAY = "auto_play";

        private static DefaultBandwidthMeter BANDWIDTH_METER = new DefaultBandwidthMeter();
        private static CookieManager DEFAULT_COOKIE_MANAGER = new CookieManager();

        private EventLogger eventLogger;
        private Handler mainHandler;

        private PlayerView playerView;
        private LinearLayout debugRootView;
        private TextView debugTextView;

        private IDataSourceFactory mediaDataSourceFactory;
        private SimpleExoPlayer player;
        private FrameworkMediaDrm mediaDrm;
        private IMediaSource mediaSource;
        private DefaultTrackSelector trackSelector;
        private DefaultTrackSelector.Parameters trackSelectorParameters;
        private DebugTextViewHelper debugViewHelper;
        private TrackGroupArray lastSeenTrackGroupArray;

        private bool startAutoPlay;
        private int startWindow;
        private long startPosition;

        // Fields used only for ad playback. The ads loader is loaded via reflection.

        private IAdsLoader adsLoader;
        private android.Net.Uri loadedAdTagUri;
        private ViewGroup adUiViewGroup;

        public PlayerActivity()
        {
            DEFAULT_COOKIE_MANAGER.SetCookiePolicy(CookiePolicy.AcceptOriginalServer);
        }

        // Activity lifecycle

        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            mainHandler = new Handler();

            mediaDataSourceFactory = BuildDataSourceFactory(true);
            if (CookieHandler.Default != DEFAULT_COOKIE_MANAGER)
            {
                CookieHandler.Default = DEFAULT_COOKIE_MANAGER;
            }

            SetContentView(Resource.Layout.player_activity);
            View rootView = FindViewById(Resource.Id.root);
            rootView.SetOnClickListener(this);
            debugRootView = (LinearLayout)FindViewById(Resource.Id.controls_root);
            debugTextView = (TextView)FindViewById(Resource.Id.debug_text_view);

            playerView = (PlayerView)FindViewById(Resource.Id.player_view);
            playerView.SetControllerVisibilityListener(this);
            playerView.SetErrorMessageProvider(new PlayerErrorMessageProvider(this));
            playerView.RequestFocus();

            if (savedInstanceState != null)
            {
                trackSelectorParameters = (DefaultTrackSelector.Parameters)savedInstanceState.GetParcelable(KEY_TRACK_SELECTOR_PARAMETERS);
                startAutoPlay = savedInstanceState.GetBoolean(KEY_AUTO_PLAY);
                startWindow = savedInstanceState.GetInt(KEY_WINDOW);
                startPosition = savedInstanceState.GetLong(KEY_POSITION);
            }
            else
            {
                trackSelectorParameters = new DefaultTrackSelector.ParametersBuilder().Build();
                ClearStartPosition();
            }
        }
        
        protected override void OnNewIntent(Intent intent)
        {
            ReleasePlayer();
            ClearStartPosition();
            Intent = intent;
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (Utils.SdkInt > 23)
            {
                InitializePlayer();
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (Utils.SdkInt <= 23 || player == null)
            {
                InitializePlayer();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            if (Utils.SdkInt <= 23)
            {
                ReleasePlayer();
            }
        }

        protected override void OnStop()
        {
            base.OnStop();
            if (Utils.SdkInt > 23)
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
            if (grantResults.Length == 0)
            {
                // Empty results are triggered if a permission is requested while another request was already
                // pending and can be safely ignored in this case.
                return;
            }
            if (grantResults[0] == (int)Permission.Granted)
            {
                InitializePlayer();
            }
            else
            {
                ShowToast(Resource.String.storage_permission_denied);
                Finish();
            }
        }
        
        protected override void OnSaveInstanceState(Bundle outState)
        {
            UpdateTrackSelectorParameters();
            UpdateStartPosition();
            outState.PutParcelable(KEY_TRACK_SELECTOR_PARAMETERS, trackSelectorParameters);
            outState.PutBoolean(KEY_AUTO_PLAY, startAutoPlay);
            outState.PutInt(KEY_WINDOW, startWindow);
            outState.PutLong(KEY_POSITION, startPosition);
        }
        // Activity input
        
        public override bool DispatchKeyEvent(KeyEvent @event)
        {
            // See whether the player view wants to handle media or DPAD keys events.
            return playerView.DispatchKeyEvent(@event) || base.DispatchKeyEvent(@event);
        }

        // OnClickListener methods
        
        public void OnClick(View view)
        {
            if (view.Parent == debugRootView)
            {
                MappedTrackInfo mappedTrackInfo = trackSelector.CurrentMappedTrackInfo;
                if (mappedTrackInfo != null)
                {
                    string title = ((Button)view).Text;
                    int rendererIndex = (int)view.GetTag(view.Id);
                    int rendererType = mappedTrackInfo.GetRendererType(rendererIndex);
                    bool allowAdaptiveSelections =
                        rendererType == C.TrackTypeVideo
                            || (rendererType == C.TrackTypeAudio
                                && mappedTrackInfo.GetTypeSupport(C.TrackTypeVideo)
                                    == MappedTrackInfo.RendererSupportNoTracks);
                    Pair dialogPair = TrackSelectionView.GetDialog(this, title, trackSelector, rendererIndex);

                    ((TrackSelectionView)dialogPair.Second).SetShowDisableOption(true);
                    ((TrackSelectionView)dialogPair.Second).SetAllowAdaptiveSelections(allowAdaptiveSelections);
                    ((AlertDialog)dialogPair.First).Show();
                }
            }
        }

        // PlaybackControlView.PlaybackPreparer implementation
        public void PreparePlayback()
        {
            InitializePlayer();
        }

        // PlaybackControlView.VisibilityListener implementation
        public void OnVisibilityChange(int visibility)
        {
            debugRootView.Visibility = (ViewStates)visibility;
        }

        // Internal methods

        private void InitializePlayer()
        {
            if (player == null)
            {
                Intent intent = Intent;
                string action = intent.Action;
                android.Net.Uri[] uris;
                string[] extensions;

                if (ACTION_VIEW.Equals(action))
                {
                    uris = new android.Net.Uri[] { intent.Data };
                    extensions = new string[] { intent.GetStringExtra(EXTENSION_EXTRA) };
                }
                else if (ACTION_VIEW_LIST.Equals(action))
                {
                    string[] uristrings = intent.GetStringArrayExtra(URI_LIST_EXTRA);
                    uris = new android.Net.Uri[uristrings.Length];
                    for (int i = 0; i < uristrings.Length; i++)
                    {
                        uris[i] = android.Net.Uri.Parse(uristrings[i]);
                    }
                    extensions = intent.GetStringArrayExtra(EXTENSION_LIST_EXTRA);
                    if (extensions == null)
                    {
                        extensions = new string[uristrings.Length];
                    }
                }
                else
                {
                    ShowToast(GetString(Resource.String.unexpected_intent_action, action));
                    Finish();
                    return;
                }
                if (Utils.MaybeRequestReadExternalStoragePermission(this, uris))
                {
                    // The player will be reinitialized if the permission is granted.
                    return;
                }

                DefaultDrmSessionManager drmSessionManager = null;
                if (intent.HasExtra(DRM_SCHEME_EXTRA) || intent.HasExtra(DRM_SCHEME_UUID_EXTRA))
                {
                    string drmLicenseUrl = intent.GetStringExtra(DRM_LICENSE_URL_EXTRA);
                    string[] keyRequestPropertiesArray =
                        intent.GetStringArrayExtra(DRM_KEY_REQUEST_PROPERTIES_EXTRA);
                    bool multiSession = intent.GetBooleanExtra(DRM_MULTI_SESSION_EXTRA, false);
                    int errorstringId = Resource.String.error_drm_unknown;
                    if (Utils.SdkInt < 18)
                    {
                        errorstringId = Resource.String.error_drm_not_supported;
                    }
                    else
                    {
                        try
                        {
                            string drmSchemeExtra = intent.HasExtra(DRM_SCHEME_EXTRA) ? DRM_SCHEME_EXTRA
                                : DRM_SCHEME_UUID_EXTRA;
                            UUID drmSchemeUuid = Utils.GetDrmUuid(intent.GetStringExtra(drmSchemeExtra));
                            if (drmSchemeUuid == null)
                            {
                                errorstringId = Resource.String.error_drm_unsupported_scheme;
                            }
                            else
                            {
                                drmSessionManager =
                                    BuildDrmSessionManagerV18(
                                        drmSchemeUuid, drmLicenseUrl, keyRequestPropertiesArray, multiSession);
                            }
                        }
                        catch (UnsupportedDrmException e)
                        {
                            errorstringId = e.Reason == UnsupportedDrmException.ReasonUnsupportedScheme
                                ? Resource.String.error_drm_unsupported_scheme : Resource.String.error_drm_unknown;
                        }
                    }
                    if (drmSessionManager == null)
                    {
                        ShowToast(errorstringId);
                        Finish();
                        return;
                    }
                }

                ITrackSelectionFactory trackSelectionFactory;
                string abrAlgorithm = intent.GetStringExtra(ABR_ALGORITHM_EXTRA);
                if (abrAlgorithm == null || ABR_ALGORITHM_DEFAULT.Equals(abrAlgorithm))
                {
                    trackSelectionFactory = new AdaptiveTrackSelection.Factory(BANDWIDTH_METER);
                }
                else if (ABR_ALGORITHM_RANDOM.Equals(abrAlgorithm))
                {
                    trackSelectionFactory = new RandomTrackSelection.Factory();
                }
                else
                {
                    ShowToast(Resource.String.error_unrecognized_abr_algorithm);
                    Finish();
                    return;
                }

                bool preferExtensionDecoders =
                    intent.GetBooleanExtra(PREFER_EXTENSION_DECODERS_EXTRA, false);
                int extensionRendererMode =
                    ((DemoApplication)Application).UseExtensionRenderers()
                        ? (preferExtensionDecoders ? DefaultRenderersFactory.ExtensionRendererModePrefer
                        : DefaultRenderersFactory.ExtensionRendererModeOn)
                        : DefaultRenderersFactory.ExtensionRendererModeOff;
                DefaultRenderersFactory renderersFactory =
                    new DefaultRenderersFactory(this, extensionRendererMode);

                trackSelector = new DefaultTrackSelector(trackSelectionFactory);
                trackSelector.SetParameters(trackSelectorParameters);
                lastSeenTrackGroupArray = null;

                player = ExoPlayerFactory.NewSimpleInstance(renderersFactory, trackSelector, drmSessionManager);

                eventLogger = new EventLogger(trackSelector);

                player.AddListener(new PlayerEventListener(this));
                player.PlayWhenReady = startAutoPlay;

                player.AddListener(eventLogger);

                // Cannot implement the AnalyticsListener because the binding doesn't work.

                //Todo: implement IAnalyticsListener
                //player.AddAnalyticsListener(eventLogger);

                player.AddAudioDebugListener(eventLogger);
                player.AddVideoDebugListener(eventLogger);

                player.AddMetadataOutput(eventLogger);
                //end Todo

                playerView.Player = player;
                playerView.SetPlaybackPreparer(this);
                debugViewHelper = new DebugTextViewHelper(player, debugTextView);
                debugViewHelper.Start();

                IMediaSource[] mediaSources = new IMediaSource[uris.Length];
                for (int i = 0; i < uris.Length; i++)
                {
                    mediaSources[i] = BuildMediaSource(uris[i], extensions[i]);
                }
                mediaSource =
                    mediaSources.Length == 1 ? mediaSources[0] : new ConcatenatingMediaSource(mediaSources);
                string adTagUristring = intent.GetStringExtra(AD_TAG_URI_EXTRA);
                if (adTagUristring != null)
                {
                    android.Net.Uri adTagUri = android.Net.Uri.Parse(adTagUristring);
                    if (!adTagUri.Equals(loadedAdTagUri))
                    {
                        ReleaseAdsLoader();
                        loadedAdTagUri = adTagUri;
                    }
                    IMediaSource adsMediaSource = CreateAdsMediaSource(mediaSource, android.Net.Uri.Parse(adTagUristring));
                    if (adsMediaSource != null)
                    {
                        mediaSource = adsMediaSource;
                    }
                    else
                    {
                        ShowToast(Resource.String.ima_not_loaded);
                    }
                }
                else
                {
                    ReleaseAdsLoader();
                }
            }
            bool haveStartPosition = startWindow != C.IndexUnset;
            if (haveStartPosition)
            {
                player.SeekTo(startWindow, startPosition);
            }
            player.Prepare(mediaSource, !haveStartPosition, false);
            UpdateButtonVisibilities();
        }

        private IMediaSource BuildMediaSource(android.Net.Uri uri)
        {
            return BuildMediaSource(uri, null);
        }

        private IMediaSource BuildMediaSource(android.Net.Uri uri, string overrideExtension)
        {
            int type = Utils.InferContentType(uri, overrideExtension);

            IMediaSource src = null;

            switch (type)
            {
                case C.TypeDash:
                    src = new DashMediaSource.Factory(new DefaultDashChunkSource.Factory(mediaDataSourceFactory), BuildDataSourceFactory(false))
                        .SetManifestParser(new FilteringManifestParser(new DashManifestParser(), GetOfflineStreamKeys(uri)))
                        .CreateMediaSource(uri);
                    break;
                case C.TypeSs:
                    src = new SsMediaSource.Factory(new DefaultSsChunkSource.Factory(mediaDataSourceFactory), BuildDataSourceFactory(false))
                        .SetManifestParser(new FilteringManifestParser(new SsManifestParser(), GetOfflineStreamKeys(uri)))
                        .CreateMediaSource(uri);
                    break;
                case C.TypeHls:
                    src = new HlsMediaSource.Factory(mediaDataSourceFactory)
                        .SetPlaylistParser(new FilteringManifestParser(new HlsPlaylistParser(), GetOfflineStreamKeys(uri)))
                        .CreateMediaSource(uri);
                    break;
                case C.TypeOther:
                    src = new ExtractorMediaSource.Factory(mediaDataSourceFactory).CreateMediaSource(uri);
                    break;
                default:
                    throw new IllegalStateException("Unsupported type: " + type);
            }

            //Todo: implement IAnalyticsListener
            src.AddEventListener(mainHandler, eventLogger);
            return src;
        }

        private List<StreamKey> GetOfflineStreamKeys(android.Net.Uri uri)
        {
            return ((DemoApplication)Application).GetDownloadTracker().GetOfflineStreamKeys(uri);
        }

        private DefaultDrmSessionManager BuildDrmSessionManagerV18(UUID uuid, string licenseUrl, string[] keyRequestPropertiesArray, bool multiSession)
        {
            IHttpDataSourceFactory licenseDataSourceFactory = ((DemoApplication)Application).BuildHttpDataSourceFactory(/* listener= */ null);
            HttpMediaDrmCallback drmCallback =
            new HttpMediaDrmCallback(licenseUrl, licenseDataSourceFactory);
            if (keyRequestPropertiesArray != null)
            {
                for (int i = 0; i < keyRequestPropertiesArray.Length - 1; i += 2)
                {
                    drmCallback.SetKeyRequestProperty(keyRequestPropertiesArray[i],
                        keyRequestPropertiesArray[i + 1]);
                }
            }
            ReleaseMediaDrm();
            mediaDrm = FrameworkMediaDrm.NewInstance(uuid);
            //return new DefaultDrmSessionManager(uuid, mediaDrm, drmCallback, null, multiSession);

            //Todo: implement IAnalyticsListener
            return new DefaultDrmSessionManager(uuid, FrameworkMediaDrm.NewInstance(uuid), drmCallback,
                null, mainHandler, eventLogger);
        }

        private void ReleasePlayer()
        {
            if (player != null)
            {
                UpdateTrackSelectorParameters();
                UpdateStartPosition();
                debugViewHelper.Stop();
                debugViewHelper = null;
                player.Release();
                player = null;
                mediaSource = null;
                trackSelector = null;

                //Todo: implement IAnalyticsListener
                eventLogger = null;
            }
            ReleaseMediaDrm();
        }

        private void ReleaseMediaDrm()
        {
            if (mediaDrm != null)
            {
                mediaDrm.Release();
                mediaDrm = null;
            }
        }

        private void ReleaseAdsLoader()
        {
            if (adsLoader != null)
            {
                adsLoader.Release();
                adsLoader = null;
                loadedAdTagUri = null;
                playerView.OverlayFrameLayout.RemoveAllViews();
            }
        }

        private void UpdateTrackSelectorParameters()
        {
            if (trackSelector != null)
            {
                trackSelectorParameters = trackSelector.GetParameters();
            }
        }

        private void UpdateStartPosition()
        {
            if (player != null)
            {
                startAutoPlay = player.PlayWhenReady;
                startWindow = player.CurrentWindowIndex;
                startPosition = Java.Lang.Math.Max(0, player.ContentPosition);
            }
        }

        private void ClearStartPosition()
        {
            startAutoPlay = true;
            startWindow = C.IndexUnset;
            startPosition = C.TimeUnset;
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

        private IMediaSource CreateAdsMediaSource(IMediaSource mediaSource, android.Net.Uri adTagUri)
        {
            // Load the extension source using reflection so the demo app doesn't have to depend on it.
            // The ads loader is reused for multiple playbacks, so that ad playback can resume.
            try
            {
                Class loaderClass = Class.ForName("com.google.android.exoplayer2.ext.ima.ImaAdsLoader");
                if (adsLoader == null)
                {
                    Constructor loaderConstructor = loaderClass.AsSubclass(Class.FromType(typeof(IAdsLoader))).GetConstructor(Class.FromType(typeof(Context)), Class.FromType(typeof(android.Net.Uri)));

                    adsLoader = (IAdsLoader)loaderConstructor.NewInstance(this, adTagUri);
                    adUiViewGroup = new FrameLayout(this);
                    // The demo app has a non-null overlay frame layout.
                    playerView.OverlayFrameLayout.AddView(adUiViewGroup);
                }
                AdMediaSourceFactory adMediaSourceFactory = new AdMediaSourceFactory(this);

                return new AdsMediaSource(mediaSource, adMediaSourceFactory, adsLoader, adUiViewGroup);
            }
            catch (ClassNotFoundException e)
            {
                // IMA extension not loaded.
                return null;
            }
            catch (Java.Lang.Exception e)
            {
                throw new RuntimeException(e);
            }
        }

        private class AdMediaSourceFactory : Java.Lang.Object, AdsMediaSource.IMediaSourceFactory
        {
            PlayerActivity activity;

            public AdMediaSourceFactory(PlayerActivity activity)
            {
                this.activity = activity;
            }

            public IMediaSource CreateMediaSource(android.Net.Uri uri)
            {
                return activity.BuildMediaSource(uri);
            }

            public int[] GetSupportedTypes()
            {
                return new int[] { C.TypeDash, C.TypeSs, C.TypeHls, C.TypeOther };
            }
        }

        // User controls
        private void UpdateButtonVisibilities()
        {
            debugRootView.RemoveAllViews();
            if (player == null)
            {
                return;
            }

            MappedTrackInfo mappedTrackInfo = trackSelector.CurrentMappedTrackInfo;
            if (mappedTrackInfo == null)
            {
                return;
            }

            for (int i = 0; i < mappedTrackInfo.RendererCount; i++)
            {
                TrackGroupArray trackGroups = mappedTrackInfo.GetTrackGroups(i);
                if (trackGroups.Length != 0)
                {
                    Button button = new Button(this);
                    int label;
                    switch (player.GetRendererType(i))
                    {
                        case C.TrackTypeAudio:
                            label = Resource.String.exo_track_selection_title_audio;
                            break;
                        case C.TrackTypeVideo:
                            label = Resource.String.exo_track_selection_title_video;
                            break;
                        case C.TrackTypeText:
                            label = Resource.String.exo_track_selection_title_text;
                            break;
                        default:
                            continue;
                    }
                    button.SetText(label);
                    button.SetTag(button.Id, i);
                    button.SetOnClickListener(this);
                    debugRootView.AddView(button);
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

        private class PlayerEventListener : Java.Lang.Object, IPlayerEventListener
        {
            PlayerActivity activity;

            public PlayerEventListener(PlayerActivity activity)
            {
                this.activity = activity;
            }
            
            public override void OnPlayerStateChanged(bool playWhenReady, int playbackState)
            {
                if (playbackState == Player.StateEnded)
                {
                    activity.ShowControls();
                }
                activity.UpdateButtonVisibilities();
            }
            
            public override void OnPositionDiscontinuity(int reason)
            {
                if (activity.player.PlaybackError != null)
                {
                    // The user has performed a seek whilst in the error state. Update the resume position so
                    // that if the user then retries, playback resumes from the position to which they seeked.
                    activity.UpdateStartPosition();
                }
            }
            
            public override void OnPlayerError(ExoPlaybackException e)
            {
                if (IsBehindLiveWindow(e))
                {
                    activity.ClearStartPosition();
                    activity.InitializePlayer();
                }
                else
                {
                    activity.UpdateStartPosition();
                    activity.UpdateButtonVisibilities();
                    activity.ShowControls();
                }
            }

            public override void OnTracksChanged(TrackGroupArray trackGroups, TrackSelectionArray trackSelections)
            {
                activity.UpdateButtonVisibilities();
                if (trackGroups != activity.lastSeenTrackGroupArray)
                {
                    MappedTrackInfo mappedTrackInfo = activity.trackSelector.CurrentMappedTrackInfo;
                    if (mappedTrackInfo != null)
                    {
                        if (mappedTrackInfo.GetTypeSupport(C.TrackTypeVideo)
                            == MappedTrackInfo.RendererSupportUnsupportedTracks)
                        {
                            activity.ShowToast(Resource.String.error_unsupported_video);
                        }
                        if (mappedTrackInfo.GetTypeSupport(C.TrackTypeAudio)
                            == MappedTrackInfo.RendererSupportUnsupportedTracks)
                        {
                            activity.ShowToast(Resource.String.error_unsupported_audio);
                        }
                    }
                    activity.lastSeenTrackGroupArray = trackGroups;
                }
            }
        }

        internal class PlayerErrorMessageProvider : Java.Lang.Object, IErrorMessageProvider
        {
            private Activity activity;

            public PlayerErrorMessageProvider(Activity activity)
            {
                this.activity = activity;
            }
            
            public Pair GetErrorMessage(ExoPlaybackException e)
            {
                string errorstring = activity.ApplicationContext.GetString(Resource.String.error_generic);
                if (e.Type == ExoPlaybackException.TypeRenderer)
                {
                    Java.Lang.Exception cause = e.RendererException;

                    if (cause is DecoderInitializationException)
                    {
                        // Special case for decoder initialization failures.
                        DecoderInitializationException decoderInitializationException =
                            (DecoderInitializationException)cause;
                        if (decoderInitializationException.DecoderName == null)
                        {
                            if (decoderInitializationException.Cause is DecoderQueryException)
                            {
                                errorstring = activity.ApplicationContext.GetString(Resource.String.error_querying_decoders);
                            }
                            else if (decoderInitializationException.SecureDecoderRequired)
                            {
                                errorstring =
                                    activity.ApplicationContext.GetString(Resource.String.error_no_secure_decoder, decoderInitializationException.MimeType);
                            }
                            else
                            {
                                errorstring =
                                    activity.ApplicationContext.GetString(Resource.String.error_no_decoder, decoderInitializationException.MimeType);
                            }
                        }
                        else
                        {
                            errorstring =
                               activity.ApplicationContext.GetString(Resource.String.error_instantiating_decoder, decoderInitializationException.DecoderName);
                        }
                    }
                }

                return Pair.Create(0, errorstring);
            }

            public Pair GetErrorMessage(Java.Lang.Object p0)
            {
                throw new NotImplementedException();
            }
        }
    }
}
