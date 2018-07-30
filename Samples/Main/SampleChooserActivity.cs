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

using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Util;
using Java.IO;
using Java.Lang;
using Utils = Com.Google.Android.Exoplayer2.Util.Util;
using Android.Graphics;
using android = Android;
using Android.Content.Res;

namespace Com.Google.Android.Exoplayer2.Demo
{
    /** An activity for selecting from a list of media samples. */
    public class SampleChooserActivity : Activity, DownloadTracker.IListener, ExpandableListView.IOnChildClickListener
    {

        private static string TAG = "SampleChooserActivity";

        private DownloadTracker downloadTracker;
        private SampleAdapter sampleAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.sample_chooser_activity);
            sampleAdapter = new SampleAdapter(this);
            ExpandableListView sampleListView = (ExpandableListView)FindViewById(Resource.Id.sample_list);
            sampleListView.SetAdapter(sampleAdapter);
            sampleListView.SetOnChildClickListener(this);

            Intent intent = Intent;
            string dataUri = intent.DataString;
            string[] uris;
            if (dataUri != null)
            {
                uris = new string[] { dataUri };
            }
            else
            {
                List<string> uriList = new List<string>();
                AssetManager assetManager = Assets;
                try
                {
                    foreach (string asset in assetManager.List(""))
                    {
                        if (asset.EndsWith(".exolist.json"))
                        {
                            uriList.Add("asset:///" + asset);
                        }
                    }
                }
                catch (Java.IO.IOException e)
                {
                    Toast.MakeText(ApplicationContext, Resource.String.sample_list_load_error, ToastLength.Long).Show();
                }

                uriList.Sort();
                uris = uriList.ToArray();
            }

            downloadTracker = ((DemoApplication)Application).GetDownloadTracker();
            SampleListLoader loaderTask = new SampleListLoader(this);
            loaderTask.Execute(uris);

            // Start the download service if it should be running but it's not currently.
            // Starting the service in the foreground causes notification flicker if there is no scheduled
            // action. Starting it in the background throws an exception if the app is in the background too
            // (e.g. if device screen is locked).
            try
            {
                Offline.DownloadService.Start(this, typeof(DemoDownloadService));
            }
            catch (IllegalStateException e)
            {
                Offline.DownloadService.StartForeground(this, typeof(DemoDownloadService));
            }
        }

        protected override void OnStart()
        {
            downloadTracker.AddListener(this);
            sampleAdapter.NotifyDataSetChanged();
            base.OnStart();
        }

        protected override void OnStop()
        {
            downloadTracker.RemoveListener(this);
            base.OnStop();
        }

        public void OnDownloadsChanged()
        {
            sampleAdapter.NotifyDataSetChanged();
        }

        private void OnSampleGroups(List<SampleGroup> groups, bool sawError)
        {
            if (sawError)
            {
                Toast.MakeText(ApplicationContext, Resource.String.sample_list_load_error, ToastLength.Long)
                    .Show();
            }

            sampleAdapter.SetSampleGroups(groups);
        }

        public bool OnChildClick(ExpandableListView parent, View view, int groupPosition, int childPosition, long id)
        {
            Sample sample = (Sample)view.GetTag(view.Id);
            StartActivity(sample.BuildIntent(this));
            return true;
        }

        private void OnSampleDownloadButtonClicked(Sample sample)
        {
            int downloadUnsupportedstringId = GetDownloadUnsupportedstringId(sample);
            if (downloadUnsupportedstringId != 0)
            {
                Toast.MakeText(ApplicationContext, downloadUnsupportedstringId, ToastLength.Long)
                    .Show();
            }
            else
            {
                UriSample uriSample = (UriSample)sample;
                downloadTracker.ToggleDownload(this, sample.name, uriSample.uri, uriSample.extension);
            }
        }

        private int GetDownloadUnsupportedstringId(Sample sample)
        {
            if (sample is PlaylistSample)
            {
                return Resource.String.download_playlist_unsupported;
            }

            UriSample uriSample = (UriSample)sample;

            if (uriSample.drmInfo != null)
            {
                return Resource.String.download_drm_unsupported;
            }

            if (uriSample.adTagUri != null)
            {
                return Resource.String.download_ads_unsupported;
            }

            string scheme = uriSample.uri.Scheme;

            if (!("http".Equals(scheme) || "https".Equals(scheme)))
            {
                return Resource.String.download_scheme_unsupported;
            }
            return 0;
        }

        private class SampleListLoader : AsyncTask<string, int, List<SampleGroup>>
        {
            private SampleChooserActivity activity;

            public SampleListLoader(SampleChooserActivity activity)
            {
                this.activity = activity;
            }

            private bool sawError;

            protected override List<SampleGroup> RunInBackground(params string[] uris)
            {
                List<SampleGroup> result = new List<SampleGroup>();
                Context context = activity.ApplicationContext;
                string userAgent = Utils.GetUserAgent(context, "ExoPlayerDemo");
                IDataSource dataSource = new DefaultDataSource(context, null, userAgent, false);
                foreach (string uri in uris)
                {
                    DataSpec dataSpec = new DataSpec(android.Net.Uri.Parse(uri));
                    var inputStream = new DataSourceInputStream(dataSource, dataSpec);
                    var memory = new MemoryStream();
                    var buffer = new byte[1024];
                    int read;
                    while ((read = inputStream.Read(buffer)) > 0)
                    {
                        memory.Write(buffer, 0, read);
                    }
                    memory.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        ReadSampleGroups(new JsonReader(new InputStreamReader(memory, "UTF-8")), result);
                    }
                    catch (Exception e)
                    {
                        Log.Error(TAG, "Error loading sample list: " + uri, e);
                        sawError = true;
                    }
                    finally
                    {
                        Utils.CloseQuietly(dataSource);
                    }
                }

                return result;
            }

            protected override void OnPostExecute(Object result)
            {
                base.OnPostExecute(result);
                // This overload is required so that the following overload is also called?! ¯\_(ツ)_/¯
            }

            protected override void OnPostExecute(List<SampleGroup> result)
            {
                base.OnPostExecute(result);
                activity.OnSampleGroups(result, sawError);
            }

            private void ReadSampleGroups(JsonReader reader, List<SampleGroup> groups)
            {
                reader.BeginArray();
                while (reader.HasNext)
                {
                    ReadSampleGroup(reader, groups);
                }
                reader.EndArray();
            }

            private void ReadSampleGroup(JsonReader reader, List<SampleGroup> groups)
            {
                string groupName = "";
                List<Sample> samples = new List<Sample>();

                reader.BeginObject();
                while (reader.HasNext)
                {
                    string name = reader.NextName();
                    switch (name)
                    {
                        case "name":
                            groupName = reader.NextString();
                            break;
                        case "samples":
                            reader.BeginArray();
                            while (reader.HasNext)
                            {
                                samples.Add(ReadEntry(reader, false));
                            }
                            reader.EndArray();
                            break;
                        case "_comment":
                            reader.NextString(); // Ignore.
                            break;
                        default:
                            throw new ParserException("Unsupported name: " + name);
                    }
                }
                reader.EndObject();

                SampleGroup group = getGroup(groupName, groups);
                group.samples.AddRange(samples);
            }

            private Sample ReadEntry(JsonReader reader, bool insidePlaylist)
            {
                string sampleName = null;
                android.Net.Uri uri = null;
                string extension = null;
                string drmScheme = null;
                string drmLicenseUrl = null;
                string[]
                drmKeyRequestProperties = null;
                bool drmMultiSession = false;
                bool preferExtensionDecoders = false;
                List<UriSample> playlistSamples = null;
                string adTagUri = null;
                string abrAlgorithm = null;

                reader.BeginObject();
                while (reader.HasNext)
                {
                    string name = reader.NextName();
                    switch (name)
                    {
                        case "name":
                            sampleName = reader.NextString();
                            break;
                        case "uri":
                            uri = android.Net.Uri.Parse(reader.NextString());
                            break;
                        case "extension":
                            extension = reader.NextString();
                            break;
                        case "drm_scheme":
                            Assertions.CheckState(!insidePlaylist, "Invalid attribute on nested item: drm_scheme");
                            drmScheme = reader.NextString();
                            break;
                        case "drm_license_url":
                            Assertions.CheckState(!insidePlaylist,
                                "Invalid attribute on nested item: drm_license_url");
                            drmLicenseUrl = reader.NextString();
                            break;
                        case "drm_key_request_properties":
                            Assertions.CheckState(!insidePlaylist,
                                "Invalid attribute on nested item: drm_key_request_properties");
                            List<string> drmKeyRequestPropertiesList = new List<string>();
                            reader.BeginObject();
                            while (reader.HasNext)
                            {
                                drmKeyRequestPropertiesList.Add(reader.NextName());
                                drmKeyRequestPropertiesList.Add(reader.NextString());
                            }
                            reader.EndObject();
                            drmKeyRequestProperties = drmKeyRequestPropertiesList.ToArray();
                            break;
                        case "drm_multi_session":
                            drmMultiSession = reader.NextBoolean();
                            break;
                        case "prefer_extension_decoders":
                            Assertions.CheckState(!insidePlaylist,
                                "Invalid attribute on nested item: prefer_extension_decoders");
                            preferExtensionDecoders = reader.NextBoolean();
                            break;
                        case "playlist":
                            Assertions.CheckState(!insidePlaylist, "Invalid nesting of playlists");
                            playlistSamples = new List<UriSample>();
                            reader.BeginArray();
                            while (reader.HasNext)
                            {
                                playlistSamples.Add((UriSample)ReadEntry(reader, true));
                            }
                            reader.EndArray();
                            break;
                        case "ad_tag_uri":
                            adTagUri = reader.NextString();
                            break;
                        case "abr_algorithm":
                            Assertions.CheckState(
                                !insidePlaylist, "Invalid attribute on nested item: abr_algorithm");
                            abrAlgorithm = reader.NextString();
                            break;
                        default:
                            throw new ParserException("Unsupported attribute name: " + name);
                    }
                }
                reader.EndObject();
                DrmInfo drmInfo =
                      drmScheme == null
                          ? null
                          : new DrmInfo(drmScheme, drmLicenseUrl, drmKeyRequestProperties, drmMultiSession);
                if (playlistSamples != null)
                {
                    UriSample[] playlistSamplesArray = playlistSamples.ToArray();
                    return new PlaylistSample(
                        sampleName, preferExtensionDecoders, abrAlgorithm, drmInfo, playlistSamplesArray);
                }
                else
                {
                    return new UriSample(
                        sampleName, preferExtensionDecoders, abrAlgorithm, drmInfo, uri, extension, adTagUri);
                }
            }

            private SampleGroup getGroup(string groupName, List<SampleGroup> groups)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (Utils.AreEqual(groupName, groups[i].title))
                    {
                        return groups[i];
                    }
                }
                SampleGroup group = new SampleGroup(groupName);
                groups.Add(group);
                return group;
            }
        }

        internal class SampleAdapter : BaseExpandableListAdapter, View.IOnClickListener
        {
            SampleChooserActivity activity;
            private List<SampleGroup> sampleGroups;

            public SampleAdapter(SampleChooserActivity activity)
            {
                this.activity = activity;
                sampleGroups = new List<SampleGroup>();
            }

            public void SetSampleGroups(List<SampleGroup> sampleGroups)
            {
                this.sampleGroups = sampleGroups;
                NotifyDataSetChanged();
            }


            public override Java.Lang.Object GetChild(int groupPosition, int childPosition)
            {
                return ((SampleGroup)GetGroup(groupPosition)).samples[childPosition];
            }


            public override long GetChildId(int groupPosition, int childPosition)
            {
                return childPosition;
            }


            public override View GetChildView(int groupPosition, int childPosition, bool isLastChild,
                View convertView, ViewGroup parent)
            {
                View view = convertView;
                if (view == null)
                {
                    view = activity.LayoutInflater.Inflate(Resource.Layout.sample_list_item, parent, false);
                    ImageView downloadButton = (ImageView)view.FindViewById(Resource.Id.download_button);
                    downloadButton.SetOnClickListener(this);
                    //downloadButton.SetFocusable(ViewFocusability.NotFocusable);

                    downloadButton.Focusable = false;
                }
                InitializeChildView(view, (Sample)GetChild(groupPosition, childPosition));
                return view;
            }


            public override int GetChildrenCount(int groupPosition)
            {
                return ((SampleGroup)GetGroup(groupPosition)).samples.Count;
            }


            public override Object GetGroup(int groupPosition)
            {
                return sampleGroups[groupPosition];
            }


            public override long GetGroupId(int groupPosition)
            {
                return groupPosition;
            }


            public override View GetGroupView(int groupPosition, bool isExpanded, View convertView,
                ViewGroup parent)
            {
                View view = convertView;
                if (view == null)
                {
                    view =
                        activity.LayoutInflater.Inflate(android.Resource.Layout.SimpleExpandableListItem1, parent, false);
                }
              ((TextView)view).SetText(((SampleGroup)GetGroup(groupPosition)).title, TextView.BufferType.Normal);
                return view;
            }


            public override int GroupCount
            {
                get
                {
                    return sampleGroups.Count;
                }
            }


            public override bool HasStableIds
            {
                get
                {
                    return false;
                }
            }


            public override bool IsChildSelectable(int groupPosition, int childPosition)
            {
                return true;
            }

            public void OnClick(View view)
            {
                activity.OnSampleDownloadButtonClicked((Sample)view.GetTag(view.Id));
            }

            private void InitializeChildView(View view, Sample sample)
            {
                view.SetTag(view.Id, sample);
                TextView sampleTitle = (TextView)view.FindViewById(Resource.Id.sample_title);
                sampleTitle.SetText(sample.name, TextView.BufferType.Normal);

                bool canDownload = activity.GetDownloadUnsupportedstringId(sample) == 0;
                bool isDownloaded = canDownload && activity.downloadTracker.IsDownloaded(((UriSample)sample).uri);
                ImageButton downloadButton = (ImageButton)view.FindViewById(Resource.Id.download_button);
                downloadButton.SetTag(downloadButton.Id, sample);
                downloadButton.SetColorFilter(new Color((canDownload ? (isDownloaded ? int.Parse("FF42A5F5", System.Globalization.NumberStyles.HexNumber) : int.Parse("FFBDBDBD", System.Globalization.NumberStyles.HexNumber)) : int.Parse("FFEEEEEE", System.Globalization.NumberStyles.HexNumber))));
                downloadButton.SetImageResource(
                    isDownloaded ? Resource.Drawable.ic_download_done : Resource.Drawable.ic_download);
            }
        }

        internal class SampleGroup : Java.Lang.Object
        {

            public string title;
            public List<Sample> samples;

            public SampleGroup(string title)
            {
                this.title = title;
                this.samples = new List<Sample>();
            }

        }

        internal class DrmInfo
        {
            public string drmScheme;
            public string drmLicenseUrl;
            public string[] drmKeyRequestProperties;
            public bool drmMultiSession;

            public DrmInfo(
                string drmScheme,
                string drmLicenseUrl,
                string[] drmKeyRequestProperties,
                bool drmMultiSession)
            {
                this.drmScheme = drmScheme;
                this.drmLicenseUrl = drmLicenseUrl;
                this.drmKeyRequestProperties = drmKeyRequestProperties;
                this.drmMultiSession = drmMultiSession;
            }

            public void updateIntent(Intent intent)
            {
                Assertions.CheckNotNull(intent);
                intent.PutExtra(PlayerActivity.DRM_SCHEME_EXTRA, drmScheme);
                intent.PutExtra(PlayerActivity.DRM_LICENSE_URL_EXTRA, drmLicenseUrl);
                intent.PutExtra(PlayerActivity.DRM_KEY_REQUEST_PROPERTIES_EXTRA, drmKeyRequestProperties);
                intent.PutExtra(PlayerActivity.DRM_MULTI_SESSION_EXTRA, drmMultiSession);
            }
        }

        internal abstract class Sample : Java.Lang.Object
        {
            public string name;
            public bool preferExtensionDecoders;
            public string abrAlgorithm;
            public DrmInfo drmInfo;

            public Sample(string name, bool preferExtensionDecoders, string abrAlgorithm, DrmInfo drmInfo)
            {
                this.name = name;
                this.preferExtensionDecoders = preferExtensionDecoders;
                this.abrAlgorithm = abrAlgorithm;
                this.drmInfo = drmInfo;
            }

            public virtual Intent BuildIntent(Context context)
            {
                Intent intent = new Intent(context, Class.FromType(typeof(PlayerActivity)));
                intent.PutExtra(PlayerActivity.PREFER_EXTENSION_DECODERS_EXTRA, preferExtensionDecoders);
                intent.PutExtra(PlayerActivity.ABR_ALGORITHM_EXTRA, abrAlgorithm);
                if (drmInfo != null)
                {
                    drmInfo.updateIntent(intent);
                }
                return intent;
            }

        }

        internal class UriSample : Sample
        {

            public android.Net.Uri uri;
            public string extension;
            public string adTagUri;

            public UriSample(
                string name,
                bool preferExtensionDecoders,
                string abrAlgorithm,
                DrmInfo drmInfo,
                android.Net.Uri uri,
                string extension,
                string adTagUri) : base(name, preferExtensionDecoders, abrAlgorithm, drmInfo)
            {
                this.uri = uri;
                this.extension = extension;
                this.adTagUri = adTagUri;
            }


            public override Intent BuildIntent(Context context)
            {
                return base.BuildIntent(context)
                    .SetData(uri)
                    .PutExtra(PlayerActivity.EXTENSION_EXTRA, extension)
                    .PutExtra(PlayerActivity.AD_TAG_URI_EXTRA, adTagUri)
                    .SetAction(PlayerActivity.ACTION_VIEW);
            }

        }

        internal class PlaylistSample : Sample
        {
            public readonly UriSample[] children;

            public PlaylistSample(
                string name,
                bool preferExtensionDecoders,
                string abrAlgorithm,
                DrmInfo drmInfo,
                params UriSample[] children) : base(name, preferExtensionDecoders, abrAlgorithm, drmInfo)
            {
                this.children = children;
            }


            public override Intent BuildIntent(Context context)
            {
                string[] uris = new string[children.Length];
                string[] extensions = new string[children.Length];
                for (int i = 0; i < children.Length; i++)
                {
                    uris[i] = children[i].uri.ToString();
                    extensions[i] = children[i].extension;
                }
                return base.BuildIntent(context)
                    .PutExtra(PlayerActivity.URI_LIST_EXTRA, uris)
                    .PutExtra(PlayerActivity.EXTENSION_LIST_EXTRA, extensions)
                    .SetAction(PlayerActivity.ACTION_VIEW_LIST);
            }
        }
    }
}
