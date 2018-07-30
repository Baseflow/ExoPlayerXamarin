/*
 * Copyright (C) 2017 The Android Open Source Project
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
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.Offline;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Upstream;
using Utils = Com.Google.Android.Exoplayer2.Util.Util;
using Java.IO;
using Android.Util;
using android = Android;
using static Com.Google.Android.Exoplayer2.Offline.DownloadManager;
using Com.Google.Android.Exoplayer2.Source;
using Java.Lang;
using Com.Google.Android.Exoplayer2.Source.Dash.Offline;
using Com.Google.Android.Exoplayer2.Source.Smoothstreaming.Offline;
using Com.Google.Android.Exoplayer2.Source.Hls.Offline;
using System.Linq;

namespace Com.Google.Android.Exoplayer2.Demo
{
    public class DownloadTracker : Java.Lang.Object, IListener
    {

        /** Listens for changes in the tracked downloads. */
        public interface IListener
        {
            /** Called when the tracked downloads changed. */
            void OnDownloadsChanged();
        }

        private const string TAG = "DownloadTracker";

        private Context context;
        private IDataSourceFactory dataSourceFactory;
        private ITrackNameProvider trackNameProvider;
        private List<IListener> listeners;
        private Dictionary<string, DownloadAction> trackedDownloadStates;
        private ActionFile actionFile;
        private Handler actionFileWriteHandler;

        internal Context Context { get { return context; } }
        internal ITrackNameProvider TrackNameProvider
        {
            get
            {
                return trackNameProvider;
            }
        }

        public DownloadTracker(
            Context context,
            IDataSourceFactory dataSourceFactory,
            File actionFile,
            DownloadAction.Deserializer[] deserializers)
        {
            this.context = context.ApplicationContext;
            this.dataSourceFactory = dataSourceFactory;
            this.actionFile = new ActionFile(actionFile);
            trackNameProvider = new DefaultTrackNameProvider(context.Resources);
            listeners = new List<IListener>();
            trackedDownloadStates = new Dictionary<string, DownloadAction>();
            HandlerThread actionFileWriteThread = new HandlerThread("DownloadTracker");
            actionFileWriteThread.Start();
            actionFileWriteHandler = new Handler(actionFileWriteThread.Looper);
            LoadTrackedActions(deserializers);
        }

        public void AddListener(IListener listener)
        {
            listeners.Add(listener);
        }

        public void RemoveListener(IListener listener)
        {
            listeners.Remove(listener);
        }

        public bool IsDownloaded(android.Net.Uri uri)
        {
            return trackedDownloadStates.ContainsKey(uri.ToString());
        }

        public List<object> GetOfflineStreamKeys(android.Net.Uri uri)
        {
            if (!trackedDownloadStates.ContainsKey(uri.ToString()))
            {
                return new List<object>();
            }
            DownloadAction action = trackedDownloadStates[uri.ToString()];

            if (action is SegmentDownloadAction)
            {
                return (List<object>)((SegmentDownloadAction)action).Keys;
            }

            return new List<object>();
        }

        public void ToggleDownload(Activity activity, string name, android.Net.Uri uri, string extension)
        {
            if (IsDownloaded(uri))
            {
                DownloadAction removeAction =
                    GetDownloadHelper(uri, extension).GetRemoveAction(Utils.GetUtf8Bytes(name));

                StartServiceWithAction(removeAction);
            }
            else
            {
                StartDownloadDialogHelper helper = new StartDownloadDialogHelper(activity, GetDownloadHelper(uri, extension), this, name);
                helper.Prepare();
            }
        }

        // DownloadManager.Listener

        public void OnInitialized(Offline.DownloadManager downloadManager)
        {
            // Do nothing.
        }

        public void OnTaskStateChanged(Offline.DownloadManager downloadManager, TaskState taskState)
        {
            DownloadAction action = taskState.Action;
            android.Net.Uri uri = action.Uri;
            if ((action.IsRemoveAction && taskState.State == TaskState.StateCompleted)
                || (!action.IsRemoveAction && taskState.State == TaskState.StateFailed))
            {
                // A download has been removed, or has failed. Stop tracking it.
                if (trackedDownloadStates.Remove(uri.ToString()) != false)
                {
                    HandleTrackedDownloadStatesChanged();
                }
            }
        }

        public void OnIdle(Offline.DownloadManager downloadManager)
        {
            // Do nothing.
        }

        // Internal methods

        private void LoadTrackedActions(DownloadAction.Deserializer[] deserializers)
        {
            try
            {
                DownloadAction[] allActions = actionFile.Load(deserializers);

                foreach (DownloadAction action in allActions)
                {
                    trackedDownloadStates[action.Uri.ToString()] = action;
                }
            }
            catch (IOException e)
            {
                Log.Error(TAG, "Failed to load tracked actions", e);
            }
        }

        private void HandleTrackedDownloadStatesChanged()
        {
            foreach (IListener listener in listeners)
            {
                listener.OnDownloadsChanged();
            }

            DownloadAction[] actions = trackedDownloadStates.Select(aa => aa.Value).ToList().ToArray();

            actionFileWriteHandler.Post(new Action(() =>
            {
                try
                {
                    actionFile.Store(actions);
                }
                catch (IOException e)
                {
                    Log.Error(TAG, string.Format("Failed to store tracked actions\r\n{0}", e.ToString()), e);
                }
            }));
        }

        internal void StartDownload(DownloadAction action)
        {
            if (trackedDownloadStates.ContainsKey(action.Uri.ToString()))
            {
                // This content is already being downloaded. Do nothing.
                return;
            }
            trackedDownloadStates[action.Uri.ToString()] = action;
            HandleTrackedDownloadStatesChanged();
            StartServiceWithAction(action);
        }

        private void StartServiceWithAction(DownloadAction action)
        {
            DownloadService.StartWithAction(context, typeof(DemoDownloadService), action, false);        }


        private DownloadHelper GetDownloadHelper(android.Net.Uri uri, string extension)
        {
            int type = Utils.InferContentType(uri, extension);
            switch (type)
            {
                case C.TypeDash:
                    return new DashDownloadHelper(uri, dataSourceFactory);
                case C.TypeSs:
                    return new SsDownloadHelper(uri, dataSourceFactory);
                case C.TypeHls:
                    return new HlsDownloadHelper(uri, dataSourceFactory);
                case C.TypeOther:
                    return new ProgressiveDownloadHelper(uri);
                default:
                    throw new IllegalStateException("Unsupported type: " + type);
            }
        }

        internal class StartDownloadDialogHelper : Java.Lang.Object, DownloadHelper.ICallback, IDialogInterfaceOnClickListener
        {

            private DownloadHelper downloadHelper;
            private DownloadTracker downloadTracker;
            private string name;

            private AlertDialog.Builder builder;
            private View dialogView;
            private List<TrackKey> trackKeys;
            private ArrayAdapter<string> trackTitles;
            private ListView representationList;

            public StartDownloadDialogHelper(Activity activity, DownloadHelper downloadHelper, DownloadTracker downloadTracker, string name)
            {
                this.downloadHelper = downloadHelper;
                this.downloadTracker = downloadTracker;
                this.name = name;
                builder =
                    new AlertDialog.Builder(activity)
                        .SetTitle(Resource.String.exo_download_description)
                        .SetPositiveButton(android.Resource.String.Ok, this)
                        .SetNegativeButton(android.Resource.String.Cancel, (IDialogInterfaceOnClickListener)null);

                // Inflate with the builder's context to ensure the correct style is used.
                LayoutInflater dialogInflater = LayoutInflater.From(builder.Context);
                dialogView = dialogInflater.Inflate(Resource.Layout.start_download_dialog, null);

                trackKeys = new List<TrackKey>();
                trackTitles = new ArrayAdapter<string>(builder.Context, android.Resource.Layout.SimpleListItemMultipleChoice);

                representationList = (ListView)dialogView.FindViewById(Resource.Id.representation_list);
                representationList.ChoiceMode = ChoiceMode.Multiple;
                representationList.Adapter = trackTitles;
            }

            public void Prepare()
            {
                downloadHelper.Prepare(this);
            }

            public void OnPrepared(DownloadHelper helper)
            {
                for (int i = 0; i < downloadHelper.PeriodCount; i++)
                {
                    TrackGroupArray trackGroups = downloadHelper.GetTrackGroups(i);
                    for (int j = 0; j < trackGroups.Length; j++)
                    {
                        TrackGroup trackGroup = trackGroups.Get(j);
                        for (int k = 0; k < trackGroup.Length; k++)
                        {
                            trackKeys.Add(new TrackKey(i, j, k));

                            var trackNameProvider = downloadTracker.TrackNameProvider;

                            var trackName = trackNameProvider.GetTrackName(trackGroup.GetFormat(k));

                            trackTitles.Add(trackName);
                        }
                    }
                    if (trackKeys.Count != 0)
                    {
                        builder.SetView(dialogView);
                    }
                    builder.Create().Show();
                }
            }

            public void OnPrepareError(DownloadHelper helper, IOException e)
            {
                Toast.MakeText(
                       downloadTracker.Context.ApplicationContext, Resource.String.download_start_error, ToastLength.Long)
                    .Show();
            }

            public void OnClick(IDialogInterface dialog, int which)
            {
                Java.Util.ArrayList selectedTrackKeys = new Java.Util.ArrayList();
                for (int i = 0; i < representationList.ChildCount; i++)
                {
                    if (representationList.IsItemChecked(i))
                    {
                        selectedTrackKeys.Add(trackKeys[i]);
                    }
                }
                if (!selectedTrackKeys.IsEmpty || trackKeys.Count == 0)
                {
                    // We have selected keys, or we're dealing with single stream content.
                    DownloadAction downloadAction =
                        downloadHelper.GetDownloadAction(Utils.GetUtf8Bytes(name), selectedTrackKeys);

                    downloadTracker.StartDownload(downloadAction);
                }
            }
        }
    }
}
