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

namespace Com.Google.Android.Exoplayer2.Demo
{
    /** An activity that plays media using {@link SimpleExoPlayer}. */
    public class PlayerActivity : Activity, OnClickListener, PlaybackPreparer, PlayerControlView.VisibilityListener
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


        private PlayerView playerView;
        private LinearLayout debugRootView;
        private TextView debugTextView;

        private DataSource.Factory mediaDataSourceFactory;
        private SimpleExoPlayer player;
        private FrameworkMediaDrm mediaDrm;
        private MediaSource mediaSource;
        private DefaultTrackSelector trackSelector;
        private DefaultTrackSelector.Parameters trackSelectorParameters;
        private DebugTextViewHelper debugViewHelper;
        private TrackGroupArray lastSeenTrackGroupArray;

        private bool startAutoPlay;
        private int startWindow;
        private long startPosition;

        // Fields used only for ad playback. The ads loader is loaded via reflection.

        private AdsLoader adsLoader;
        private Uri loadedAdTagUri;
        private ViewGroup adUiViewGroup;

        public PlayerActivity()
        {
            DEFAULT_COOKIE_MANAGER.setCookiePolicy(CookiePolicy.ACCEPT_ORIGINAL_SERVER);
        }

        // Activity lifecycle

        //override
        public void onCreate(Bundle savedInstanceState)
        {
            super.onCreate(savedInstanceState);
            mediaDataSourceFactory = buildDataSourceFactory(true);
            if (CookieHandler.getDefault() != DEFAULT_COOKIE_MANAGER)
            {
                CookieHandler.setDefault(DEFAULT_COOKIE_MANAGER);
            }

            setContentView(Resource.Layout.player_activity);
            View rootView = findViewById(Resource.Id.root);
            rootView.setOnClickListener(this);
            debugRootView = findViewById(Resource.Id.controls_root);
            debugTextView = findViewById(Resource.Id.debug_text_view);

            playerView = findViewById(Resource.Id.player_view);
            playerView.setControllerVisibilityListener(this);
            playerView.setErrorMessageProvider(new PlayerErrorMessageProvider());
            playerView.requestFocus();

            if (savedInstanceState != null)
            {
                trackSelectorParameters = savedInstanceState.getParcelable(KEY_TRACK_SELECTOR_PARAMETERS);
                startAutoPlay = savedInstanceState.getBoolean(KEY_AUTO_PLAY);
                startWindow = savedInstanceState.getInt(KEY_WINDOW);
                startPosition = savedInstanceState.getLong(KEY_POSITION);
            }
            else
            {
                trackSelectorParameters = new DefaultTrackSelector.ParametersBuilder().build();
                clearStartPosition();
            }
        }

        //override
        public void onNewIntent(Intent intent)
        {
            releasePlayer();
            clearStartPosition();
            setIntent(intent);
        }

        //override
        public void onStart()
        {
            super.onStart();
            if (Util.SDK_INT > 23)
            {
                initializePlayer();
            }
        }

        //override
        public void onResume()
        {
            super.onResume();
            if (Util.SDK_INT <= 23 || player == null)
            {
                initializePlayer();
            }
        }

        //override
        public void onPause()
        {
            super.onPause();
            if (Util.SDK_INT <= 23)
            {
                releasePlayer();
            }
        }

        //override
        public void onStop()
        {
            super.onStop();
            if (Util.SDK_INT > 23)
            {
                releasePlayer();
            }
        }

        //override
        public void onDestroy()
        {
            super.onDestroy();
            releaseAdsLoader();
        }

        //override
        public void onRequestPermissionsResult(int requestCode, string[] permissions,
             int[] grantResults)
        {
            if (grantResults.length == 0)
            {
                // Empty results are triggered if a permission is requested while another request was already
                // pending and can be safely ignored in this case.
                return;
            }
            if (grantResults[0] == PackageManager.PERMISSION_GRANTED)
            {
                initializePlayer();
            }
            else
            {
                showToast(Resource.String.storage_permission_denied);
                finish();
            }
        }

        //override
        public void onSaveInstanceState(Bundle outState)
        {
            updateTrackSelectorParameters();
            updateStartPosition();
            outState.putParcelable(KEY_TRACK_SELECTOR_PARAMETERS, trackSelectorParameters);
            outState.putBoolean(KEY_AUTO_PLAY, startAutoPlay);
            outState.putInt(KEY_WINDOW, startWindow);
            outState.putLong(KEY_POSITION, startPosition);
        }

        // Activity input

        //override
        public bool dispatchKeyEvent(KeyEvent event)
        {
            // See whether the player view wants to handle media or DPAD keys events.
            return playerView.dispatchKeyEvent(event) || super.dispatchKeyEvent(event);
        }

        // OnClickListener methods

        //override
        public void onClick(View view)
        {
            if (view.getParent() == debugRootView)
            {
                MappedTrackInfo mappedTrackInfo = trackSelector.getCurrentMappedTrackInfo();
                if (mappedTrackInfo != null)
                {
                    CharSequence title = ((Button)view).getText();
                    int rendererIndex = (int)view.getTag();
                    int rendererType = mappedTrackInfo.getRendererType(rendererIndex);
                    bool allowAdaptiveSelections =
                        rendererType == C.TRACK_TYPE_VIDEO
                            || (rendererType == C.TRACK_TYPE_AUDIO
                                && mappedTrackInfo.getTypeSupport(C.TRACK_TYPE_VIDEO)
                                    == MappedTrackInfo.RENDERER_SUPPORT_NO_TRACKS);
                    Pair<AlertDialog, TrackSelectionView> dialogPair =
                        TrackSelectionView.getDialog(this, title, trackSelector, rendererIndex);
                    dialogPair.second.setShowDisableOption(true);
                    dialogPair.second.setAllowAdaptiveSelections(allowAdaptiveSelections);
                    dialogPair.first.show();
                }
            }
        }

        // PlaybackControlView.PlaybackPreparer implementation

        //override
        public void preparePlayback()
        {
            initializePlayer();
        }

        // PlaybackControlView.VisibilityListener implementation

        //override
        public void onVisibilityChange(int visibility)
        {
            debugRootView.setVisibility(visibility);
        }

        // Internal methods

        private void initializePlayer()
        {
            if (player == null)
            {
                Intent intent = getIntent();
                string action = intent.getAction();
                Uri[] uris;
                string[] extensions;
                if (ACTION_VIEW.equals(action))
                {
                    uris = new Uri[] { intent.getData() };
                    extensions = new string[] { intent.getstringExtra(EXTENSION_EXTRA) };
                }
                else if (ACTION_VIEW_LIST.equals(action))
                {
                    string[] uristrings = intent.getstringArrayExtra(URI_LIST_EXTRA);
                    uris = new Uri[uristrings.length];
                    for (int i = 0; i < uristrings.length; i++)
                    {
                        uris[i] = Uri.parse(uristrings[i]);
                    }
                    extensions = intent.getstringArrayExtra(EXTENSION_LIST_EXTRA);
                    if (extensions == null)
                    {
                        extensions = new string[uristrings.length];
                    }
                }
                else
                {
                    showToast(getstring(Resource.String.unexpected_intent_action, action));
                    finish();
                    return;
                }
                if (Util.maybeRequestReadExternalStoragePermission(this, uris))
                {
                    // The player will be reinitialized if the permission is granted.
                    return;
                }

                DefaultDrmSessionManager<FrameworkMediaCrypto> drmSessionManager = null;
                if (intent.hasExtra(DRM_SCHEME_EXTRA) || intent.hasExtra(DRM_SCHEME_UUID_EXTRA))
                {
                    string drmLicenseUrl = intent.getstringExtra(DRM_LICENSE_URL_EXTRA);
                    string[] keyRequestPropertiesArray =
                        intent.getstringArrayExtra(DRM_KEY_REQUEST_PROPERTIES_EXTRA);
                    bool multiSession = intent.getBooleanExtra(DRM_MULTI_SESSION_EXTRA, false);
                    int errorstringId = Resource.String.error_drm_unknown;
                    if (Util.SDK_INT < 18)
                    {
                        errorstringId = Resource.String.error_drm_not_supported;
                    }
                    else
                    {
                        try
                        {
                            string drmSchemeExtra = intent.hasExtra(DRM_SCHEME_EXTRA) ? DRM_SCHEME_EXTRA
                                : DRM_SCHEME_UUID_EXTRA;
                            UUID drmSchemeUuid = Util.getDrmUuid(intent.getstringExtra(drmSchemeExtra));
                            if (drmSchemeUuid == null)
                            {
                                errorstringId = Resource.String.error_drm_unsupported_scheme;
                            }
                            else
                            {
                                drmSessionManager =
                                    buildDrmSessionManagerV18(
                                        drmSchemeUuid, drmLicenseUrl, keyRequestPropertiesArray, multiSession);
                            }
                        }
                        catch (UnsupportedDrmException e)
                        {
                            errorstringId = e.reason == UnsupportedDrmException.REASON_UNSUPPORTED_SCHEME
                                ? Resource.String.error_drm_unsupported_scheme : Resource.String.error_drm_unknown;
                        }
                    }
                    if (drmSessionManager == null)
                    {
                        showToast(errorstringId);
                        finish();
                        return;
                    }
                }

                TrackSelection.Factory trackSelectionFactory;
                string abrAlgorithm = intent.getstringExtra(ABR_ALGORITHM_EXTRA);
                if (abrAlgorithm == null || ABR_ALGORITHM_DEFAULT.equals(abrAlgorithm))
                {
                    trackSelectionFactory = new AdaptiveTrackSelection.Factory(BANDWIDTH_METER);
                }
                else if (ABR_ALGORITHM_RANDOM.equals(abrAlgorithm))
                {
                    trackSelectionFactory = new RandomTrackSelection.Factory();
                }
                else
                {
                    showToast(Resource.String.error_unrecognized_abr_algorithm);
                    finish();
                    return;
                }

                bool preferExtensionDecoders =
                    intent.getBooleanExtra(PREFER_EXTENSION_DECODERS_EXTRA, false);
                @DefaultRenderersFactory.ExtensionRendererMode int extensionRendererMode =
                    ((DemoApplication)getApplication()).useExtensionRenderers()
                        ? (preferExtensionDecoders ? DefaultRenderersFactory.EXTENSION_RENDERER_MODE_PREFER
                        : DefaultRenderersFactory.EXTENSION_RENDERER_MODE_ON)
                        : DefaultRenderersFactory.EXTENSION_RENDERER_MODE_OFF;
                DefaultRenderersFactory renderersFactory =
                    new DefaultRenderersFactory(this, extensionRendererMode);

                trackSelector = new DefaultTrackSelector(trackSelectionFactory);
                trackSelector.setParameters(trackSelectorParameters);
                lastSeenTrackGroupArray = null;

                player =
                    ExoPlayerFactory.newSimpleInstance(renderersFactory, trackSelector, drmSessionManager);
                player.addListener(new PlayerEventListener());
                player.setPlayWhenReady(startAutoPlay);
                player.addAnalyticsListener(new EventLogger(trackSelector));
                playerView.setPlayer(player);
                playerView.setPlaybackPreparer(this);
                debugViewHelper = new DebugTextViewHelper(player, debugTextView);
                debugViewHelper.start();

                MediaSource[] mediaSources = new MediaSource[uris.length];
                for (int i = 0; i < uris.length; i++)
                {
                    mediaSources[i] = buildMediaSource(uris[i], extensions[i]);
                }
                mediaSource =
                    mediaSources.length == 1 ? mediaSources[0] : new ConcatenatingMediaSource(mediaSources);
                string adTagUristring = intent.getstringExtra(AD_TAG_URI_EXTRA);
                if (adTagUristring != null)
                {
                    Uri adTagUri = Uri.parse(adTagUristring);
                    if (!adTagUri.equals(loadedAdTagUri))
                    {
                        releaseAdsLoader();
                        loadedAdTagUri = adTagUri;
                    }
                    MediaSource adsMediaSource = createAdsMediaSource(mediaSource, Uri.parse(adTagUristring));
                    if (adsMediaSource != null)
                    {
                        mediaSource = adsMediaSource;
                    }
                    else
                    {
                        showToast(Resource.String.ima_not_loaded);
                    }
                }
                else
                {
                    releaseAdsLoader();
                }
            }
            bool haveStartPosition = startWindow != C.INDEX_UNSET;
            if (haveStartPosition)
            {
                player.seekTo(startWindow, startPosition);
            }
            player.prepare(mediaSource, !haveStartPosition, false);
            updateButtonVisibilities();
        }

        private MediaSource buildMediaSource(Uri uri)
        {
            return buildMediaSource(uri, null);
        }


        private MediaSource buildMediaSource(Uri uri, string overrideExtension)
        {
            int type = Util.inferContentType(uri, overrideExtension);
            switch (type)
            {
                case C.TYPE_DASH:
                    return new DashMediaSource.Factory(
                            new DefaultDashChunkSource.Factory(mediaDataSourceFactory),
                            buildDataSourceFactory(false))
                        .setManifestParser(
                            new FilteringManifestParser<>(
                                new DashManifestParser(), (List<RepresentationKey>)getOfflineStreamKeys(uri)))
                        .createMediaSource(uri);
                case C.TYPE_SS:
                    return new SsMediaSource.Factory(
                            new DefaultSsChunkSource.Factory(mediaDataSourceFactory),
                            buildDataSourceFactory(false))
                        .setManifestParser(
                            new FilteringManifestParser<>(
                                new SsManifestParser(), (List<StreamKey>)getOfflineStreamKeys(uri)))
                        .createMediaSource(uri);
                case C.TYPE_HLS:
                    return new HlsMediaSource.Factory(mediaDataSourceFactory)
                        .setPlaylistParser(
                            new FilteringManifestParser<>(
                                new HlsPlaylistParser(), (List<RenditionKey>)getOfflineStreamKeys(uri)))
                        .createMediaSource(uri);
                case C.TYPE_OTHER:
                    return new ExtractorMediaSource.Factory(mediaDataSourceFactory).createMediaSource(uri);
                default:
                    {
                        throw new IllegalStateException("Unsupported type: " + type);
                    }
            }
        }

        private List<?> getOfflineStreamKeys(Uri uri)
        {
            return ((DemoApplication)getApplication()).getDownloadTracker().getOfflineStreamKeys(uri);
        }

        private DefaultDrmSessionManager<FrameworkMediaCrypto> buildDrmSessionManagerV18(
            UUID uuid, string licenseUrl, string[] keyRequestPropertiesArray, bool multiSession)
        {
            HttpDataSource.Factory licenseDataSourceFactory =
        ((DemoApplication)getApplication()).buildHttpDataSourceFactory(/* listener= */ null);
            HttpMediaDrmCallback drmCallback =
            new HttpMediaDrmCallback(licenseUrl, licenseDataSourceFactory);
            if (keyRequestPropertiesArray != null)
            {
                for (int i = 0; i < keyRequestPropertiesArray.length - 1; i += 2)
                {
                    drmCallback.setKeyRequestProperty(keyRequestPropertiesArray[i],
                        keyRequestPropertiesArray[i + 1]);
                }
            }
            releaseMediaDrm();
            mediaDrm = FrameworkMediaDrm.newInstance(uuid);
            return new DefaultDrmSessionManager<>(uuid, mediaDrm, drmCallback, null, multiSession);
        }

        private void releasePlayer()
        {
            if (player != null)
            {
                updateTrackSelectorParameters();
                updateStartPosition();
                debugViewHelper.stop();
                debugViewHelper = null;
                player.release();
                player = null;
                mediaSource = null;
                trackSelector = null;
            }
            releaseMediaDrm();
        }

        private void releaseMediaDrm()
        {
            if (mediaDrm != null)
            {
                mediaDrm.release();
                mediaDrm = null;
            }
        }

        private void releaseAdsLoader()
        {
            if (adsLoader != null)
            {
                adsLoader.release();
                adsLoader = null;
                loadedAdTagUri = null;
                playerView.getOverlayFrameLayout().removeAllViews();
            }
        }

        private void updateTrackSelectorParameters()
        {
            if (trackSelector != null)
            {
                trackSelectorParameters = trackSelector.getParameters();
            }
        }

        private void updateStartPosition()
        {
            if (player != null)
            {
                startAutoPlay = player.getPlayWhenReady();
                startWindow = player.getCurrentWindowIndex();
                startPosition = Math.max(0, player.getContentPosition());
            }
        }

        private void clearStartPosition()
        {
            startAutoPlay = true;
            startWindow = C.INDEX_UNSET;
            startPosition = C.TIME_UNSET;
        }

        /**
         * Returns a new DataSource factory.
         *
         * @param useBandwidthMeter Whether to set {@link #BANDWIDTH_METER} as a listener to the new
         *     DataSource factory.
         * @return A new DataSource factory.
         */
        private DataSource.Factory buildDataSourceFactory(bool useBandwidthMeter)
        {
            return ((DemoApplication)getApplication())
                .buildDataSourceFactory(useBandwidthMeter ? BANDWIDTH_METER : null);
        }



        // User controls

        private void updateButtonVisibilities()
        {
            debugRootView.removeAllViews();
            if (player == null)
            {
                return;
            }

            MappedTrackInfo mappedTrackInfo = trackSelector.getCurrentMappedTrackInfo();
            if (mappedTrackInfo == null)
            {
                return;
            }

            for (int i = 0; i < mappedTrackInfo.getRendererCount(); i++)
            {
                TrackGroupArray trackGroups = mappedTrackInfo.getTrackGroups(i);
                if (trackGroups.length != 0)
                {
                    Button button = new Button(this);
                    int label;
                    switch (player.getRendererType(i))
                    {
                        case C.TRACK_TYPE_AUDIO:
                            label = Resource.String.exo_track_selection_title_audio;
                            break;
                        case C.TRACK_TYPE_VIDEO:
                            label = Resource.String.exo_track_selection_title_video;
                            break;
                        case C.TRACK_TYPE_TEXT:
                            label = Resource.String.exo_track_selection_title_text;
                            break;
                        default:
                            continue;
                    }
                    button.setText(label);
                    button.setTag(i);
                    button.setOnClickListener(this);
                    debugRootView.addView(button);
                }
            }
        }

        private void showControls()
        {
            debugRootView.setVisibility(View.VISIBLE);
        }

        private void showToast(int messageId)
        {
            showToast(getstring(messageId));
        }

        private void showToast(string message)
        {
            Toast.makeText(getApplicationContext(), message, Toast.LENGTH_LONG).show();
        }

        private static bool isBehindLiveWindow(ExoPlaybackException e)
        {
            if (e.type != ExoPlaybackException.TYPE_SOURCE)
            {
                return false;
            }
            Throwable cause = e.getSourceException();
            while (cause != null)
            {
                if (cause instanceof BehindLiveWindowException) {
                    return true;
                }
                cause = cause.getCause();
            }
            return false;
        }

        private class PlayerEventListener extends Player.DefaultEventListener
        {

    //override
        public void onPlayerStateChanged(bool playWhenReady, int playbackState)
        {
            if (playbackState == Player.STATE_ENDED)
            {
                showControls();
            }
            updateButtonVisibilities();
        }

        //override
        public void onPositionDiscontinuity(@Player.DiscontinuityReason int reason)
        {
            if (player.getPlaybackError() != null)
            {
                // The user has performed a seek whilst in the error state. Update the resume position so
                // that if the user then retries, playback resumes from the position to which they seeked.
                updateStartPosition();
            }
        }

        //override
        public void onPlayerError(ExoPlaybackException e)
        {
            if (isBehindLiveWindow(e))
            {
                clearStartPosition();
                initializePlayer();
            }
            else
            {
                updateStartPosition();
                updateButtonVisibilities();
                showControls();
            }
        }

        //override
        @SuppressWarnings("ReferenceEquality")
    public void onTracksChanged(TrackGroupArray trackGroups, TrackSelectionArray trackSelections)
        {
            updateButtonVisibilities();
            if (trackGroups != lastSeenTrackGroupArray)
            {
                MappedTrackInfo mappedTrackInfo = trackSelector.getCurrentMappedTrackInfo();
                if (mappedTrackInfo != null)
                {
                    if (mappedTrackInfo.getTypeSupport(C.TRACK_TYPE_VIDEO)
                        == MappedTrackInfo.RENDERER_SUPPORT_UNSUPPORTED_TRACKS)
                    {
                        showToast(Resource.String.error_unsupported_video);
                    }
                    if (mappedTrackInfo.getTypeSupport(C.TRACK_TYPE_AUDIO)
                        == MappedTrackInfo.RENDERER_SUPPORT_UNSUPPORTED_TRACKS)
                    {
                        showToast(Resource.String.error_unsupported_audio);
                    }
                }
                lastSeenTrackGroupArray = trackGroups;
            }
        }
    }

    internal class PlayerErrorMessageProvider : Util.IErrorMessageProvider
    {
        private Activity activity;

        public IntPtr Handle => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public PlayerErrorMessageProvider(Activity activity)
        {
            this.activity = activity;
        }

        //override
        public Pair GetErrorMessage(ExoPlaybackException e)
        {
            string errorstring = activity.ApplicationContext.GetString(Resource.String.error_generic);
            if (e.Type == ExoPlaybackException.TypeRenderer)
            {
                Java.Lang.Exception cause = e.RendererException;

                if (cause is DecoderInitializationException) {
                    // Special case for decoder initialization failures.
                    DecoderInitializationException decoderInitializationException =
                        (DecoderInitializationException)cause;
                    if (decoderInitializationException.DecoderName == null)
                    {
                        if (decoderInitializationException.Cause is DecoderQueryException) {
                            errorstring = activity.ApplicationContext.GetString(Resource.String.error_querying_decoders);
                        } else if (decoderInitializationException.SecureDecoderRequired)
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
