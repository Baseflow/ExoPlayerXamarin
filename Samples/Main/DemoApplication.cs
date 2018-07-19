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
using Com.Google.Android.Exoplayer2.Offline;
using Com.Google.Android.Exoplayer2.Source.Dash.Offline;
using Com.Google.Android.Exoplayer2.Source.Hls.Offline;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming.Offline;
using static Com.Google.Android.Exoplayer2.Offline.DownloadAction;
using Utils = Com.Google.Android.Exoplayer2.Util.Util;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Upstream.Cache;
using android = Android;
using Java.IO;


namespace Com.Google.Android.Exoplayer2.Demo
{
    /**
     * Placeholder application to facilitate overriding Application methods for debugging and testing.
     */
    public class DemoApplication : Application
    {

        private const string DOWNLOAD_ACTION_FILE = "actions";
        private const string DOWNLOAD_TRACKER_ACTION_FILE = "tracked_actions";
        private const string DOWNLOAD_CONTENT_DIRECTORY = "downloads";
        private const int MAX_SIMULTANEOUS_DOWNLOADS = 2;
        private Deserializer[] DOWNLOAD_DESERIALIZERS =
          new Deserializer[] {
            DashDownloadAction.Deserializer,
            HlsDownloadAction.Deserializer,
            SsDownloadAction.Deserializer,
            ProgressiveDownloadAction.Deserializer
        };

        protected String userAgent;

        private File downloadDirectory;
        private ICache downloadCache;
        private Offline.DownloadManager downloadManager;
        private DownloadTracker downloadTracker;


        public override void OnCreate()
        {
            base.OnCreate();
            userAgent = Utils.GetUserAgent(this, "ExoPlayerDemo");
        }

        /** Returns a {@link DataSource.Factory}. */
        public IDataSourceFactory BuildDataSourceFactory(ITransferListener listener)
        {
            DefaultDataSourceFactory upstreamFactory = new DefaultDataSourceFactory(this, listener, BuildHttpDataSourceFactory(listener));

            return buildReadOnlyCacheDataSource(upstreamFactory, getDownloadCache());
        }

        /** Returns a {@link HttpDataSource.Factory}. */
        public IHttpDataSourceFactory BuildHttpDataSourceFactory(ITransferListener listener)
        {
            return new DefaultHttpDataSourceFactory(userAgent, listener);
        }

        /** Returns whether extension renderers should be used. */
        public bool useExtensionRenderers()
        {
            return android.Support.Compat.BuildConfig.Flavor.Equals("withExtensions");
        }

        public Offline.DownloadManager getDownloadManager()
        {
            initDownloadManager();
            return downloadManager;
        }

        public DownloadTracker getDownloadTracker()
        {
            initDownloadManager();
            return downloadTracker;
        }

        object _lock;

        private void initDownloadManager()
        {
            lock (_lock)
            {
                if (downloadManager == null)
                {
                    DownloaderConstructorHelper downloaderConstructorHelper = new DownloaderConstructorHelper(
                        getDownloadCache(),
                        BuildHttpDataSourceFactory(/* listener= */ null));

                    downloadManager =
                        new Offline.DownloadManager(
                            downloaderConstructorHelper,
                            MAX_SIMULTANEOUS_DOWNLOADS,
                            Offline.DownloadManager.DefaultMinRetryCount,
                            new File(getDownloadDirectory(), DOWNLOAD_ACTION_FILE),
                            DOWNLOAD_DESERIALIZERS);

                    downloadTracker =
                        new DownloadTracker(
                            /* context= */ this,
                            BuildDataSourceFactory(/* listener= */ null),
                            new File(getDownloadDirectory(), DOWNLOAD_TRACKER_ACTION_FILE),
                            DOWNLOAD_DESERIALIZERS);
                    downloadManager.AddListener(downloadTracker);
                }
            }
        }

        private ICache getDownloadCache()
        {
            lock (_lock)
            {
                if (downloadCache == null)
                {
                    File downloadContentDirectory = new File(getDownloadDirectory(), DOWNLOAD_CONTENT_DIRECTORY);
                    downloadCache = new SimpleCache(downloadContentDirectory, new NoOpCacheEvictor());
                }
            }
            return downloadCache;

        }

        private File getDownloadDirectory()
        {
            if (downloadDirectory == null)
            {
                downloadDirectory = android.OS.Environment.ExternalStorageDirectory;
                if (downloadDirectory == null)
                {
                    downloadDirectory = this.FilesDir;
                }
            }
            return downloadDirectory;
        }

        private static CacheDataSourceFactory buildReadOnlyCacheDataSource(
            DefaultDataSourceFactory upstreamFactory, ICache cache)
        {
            return new CacheDataSourceFactory(
                cache,
                upstreamFactory,
                new FileDataSourceFactory(),
                /* cacheWriteDataSinkFactory= */ null,
                CacheDataSource.FlagIgnoreCacheOnError,
                /* eventListener= */ null);
        }
    }
}
