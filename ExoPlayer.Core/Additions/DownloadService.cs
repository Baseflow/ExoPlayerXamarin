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
using Android.Util;
using Com.Google.Android.Exoplayer2.Util;
using Java.IO;
using Utils = Com.Google.Android.Exoplayer2.Util.Util;
using Com.Google.Android.Exoplayer2.Scheduler;
using Java.Lang;
using static Com.Google.Android.Exoplayer2.Offline.DownloadManager;

namespace Com.Google.Android.Exoplayer2.Offline
{
    public abstract partial class DownloadService : Service
    {
        public static string action_init
        {
            get { return ACTION_INIT; }
        }

        public static string action_add
        {
            get { return ACTION_ADD; }
        }

        public static string action_start_downloads
        {
            get { return ACTION_START_DOWNLOADS; }
        }

        public static string action_stop_downloads
        {
            get { return ACTION_STOP_DOWNLOADS; }
        }



        /** Starts a download service without adding a new {@link DownloadAction}. */
        public const string ACTION_INIT = "com.google.android.exoplayer.downloadService.action.INIT";

        /** Starts a download service, adding a new {@link DownloadAction} to be executed. */
        public const string ACTION_ADD = "com.google.android.exoplayer.downloadService.action.ADD";

        /** Like {@link #ACTION_INIT}, but with {@link #KEY_FOREGROUND} implicitly set to true. */
        private const string ACTION_RESTART =
                "com.google.android.exoplayer.downloadService.action.RESTART";

        /** Starts download tasks. */
        public const string ACTION_START_DOWNLOADS =
                "com.google.android.exoplayer.downloadService.action.START_DOWNLOADS";

        /** Stops download tasks. */
        private const string ACTION_STOP_DOWNLOADS =
                "com.google.android.exoplayer.downloadService.action.STOP_DOWNLOADS";

        /** Key for the {@link DownloadAction} in an {@link #ACTION_ADD} intent. */
        public const string KEY_DOWNLOAD_ACTION = "download_action";

        /**
         * Key for a bool flag in any intent to indicate whether the service was started in the
         * foreground. If set, the service is guaranteed to call {@link #startForeground(int,
         * Notification)}.
         */
        public const string KEY_FOREGROUND = "foreground";

        /** Default foreground notification update interval in milliseconds. */
        public static long DEFAULT_FOREGROUND_NOTIFICATION_UPDATE_INTERVAL = 1000;

        private static string TAG = "DownloadService";
        private static bool DEBUG = false;

        // Keep the requirements helper for each DownloadService as long as there are tasks (and the
        // process is running). This allows tasks to resume when there's no scheduler. It may also allow
        // tasks the resume more quickly than when relying on the scheduler alone.
        private static Dictionary<DownloadService, RequirementsHelper> requirementsHelpers = new Dictionary<DownloadService, RequirementsHelper>();

        private ForegroundNotificationUpdater foregroundNotificationUpdater;
        private string channelId;
        private int channelName;

        private DownloadManager downloadManager;
        private DownloadManagerListener downloadManagerListener;
        private int lastStartId;
        private bool startedInForeground;

        ///HACK Needed to instantiate the service, Java calls super() from method body, C# for some reason can't do ctor : base(args) or ctor(args) -> ctor() : this(args)
        ///TODO Must call Initialize() from child class.
        public DownloadService()
        {

        }

        /**
         * Creates a DownloadService with {@link #DEFAULT_FOREGROUND_NOTIFICATION_UPDATE_INTERVAL}.
         *
         * @param foregroundNotificationId The notification id for the foreground notification, must not
         *     be 0.
         */
        protected DownloadService(int foregroundNotificationId) : this(foregroundNotificationId, DEFAULT_FOREGROUND_NOTIFICATION_UPDATE_INTERVAL)
        {
        }

        /**
         * @param foregroundNotificationId The notification id for the foreground notification, must not
         *     be 0.
         * @param foregroundNotificationUpdateInterval The maximum interval to update foreground
         *     notification, in milliseconds.
         */
        protected DownloadService(int foregroundNotificationId, long foregroundNotificationUpdateInterval) : this(foregroundNotificationId, foregroundNotificationUpdateInterval, null, 0)
        {
        }

        /**
         * @param foregroundNotificationId The notification id for the foreground notification. Must not
         *     be 0.
         * @param foregroundNotificationUpdateInterval The maximum interval between updates to the
         *     foreground notification, in milliseconds.
         * @param channelId An id for a low priority notification channel to create, or {@code null} if
         *     the app will take care of creating a notification channel if needed. If specified, must be
         *     unique per package and the value may be truncated if it is too long.
         * @param channelName A string resource identifier for the user visible name of the channel, if
         *     {@code channelId} is specified. The recommended maximum length is 40 characters; the value
         *     may be truncated if it is too long.
         */
        protected DownloadService(int foregroundNotificationId, long foregroundNotificationUpdateInterval, string channelId, int channelName)
        {
            foregroundNotificationUpdater = new ForegroundNotificationUpdater(foregroundNotificationId, foregroundNotificationUpdateInterval, this);
            this.channelId = channelId;
            this.channelName = channelName;
        }

        public void Initialize(int foregroundNotificationId, long foregroundNotificationUpdateInterval, string channelId, int channelName)
        {
            foregroundNotificationUpdater = new ForegroundNotificationUpdater(foregroundNotificationId, foregroundNotificationUpdateInterval, this);
            this.channelId = channelId;
            this.channelName = channelName;
        }

        /**
         * Builds an {@link Intent} for adding an action to be executed by the service.
         *
         * @param context A {@link Context}.
         * @param clazz The concrete download service being targeted by the intent.
         * @param downloadAction The action to be executed.
         * @param foreground Whether this intent will be used to start the service in the foreground.
         * @return Created Intent.
         */
        public static Intent BuildAddActionIntent(Context context, Type downloadServiceType, DownloadAction downloadAction, bool foreground)
        {
            return new Intent(context, downloadServiceType)
                    .SetAction(ACTION_ADD)
                    .PutExtra(KEY_DOWNLOAD_ACTION, downloadAction.ToByteArray())
                    .PutExtra(KEY_FOREGROUND, foreground);
        }

        /**
         * Starts the service, adding an action to be executed.
         *
         * @param context A {@link Context}.
         * @param clazz The concrete download service to be started.
         * @param downloadAction The action to be executed.
         * @param foreground Whether the service is started in the foreground.
         */
        public static void StartWithAction(Context context, Type downloadServiceType, DownloadAction downloadAction, bool foreground)
        {
            Intent intent = BuildAddActionIntent(context, downloadServiceType, downloadAction, foreground);
            if (foreground)
            {
                Utils.StartForegroundService(context, intent);
            }
            else
            {
                context.StartService(intent);
            }
        }

        /**
         * Starts the service without adding a new action. If there are any not finished actions and the
         * requirements are met, the service resumes executing actions. Otherwise it stops immediately.
         *
         * @param context A {@link Context}.
         * @param clazz The concrete download service to be started.
         * @see #startForeground(Context, Class)
         */
        public static void Start(Context context, Type downloadServiceType)
        {
            context.StartService(new Intent(context, downloadServiceType).SetAction(ACTION_INIT));
        }

        /**
         * Starts the service in the foreground without adding a new action. If there are any not finished
         * actions and the requirements are met, the service resumes executing actions. Otherwise it stops
         * immediately.
         *
         * @param context A {@link Context}.
         * @param clazz The concrete download service to be started.
         * @see #start(Context, Class)
         */
        public static void StartForeground(Context context, Type downloadServiceType)
        {
            Intent intent = new Intent(context, downloadServiceType).SetAction(ACTION_INIT).PutExtra(KEY_FOREGROUND, true);
            Utils.StartForegroundService(context, intent);
        }

        //@Override
        public override void OnCreate()
        {
            Logd("onCreate");
            if (channelId != null)
            {
                NotificationUtil.CreateNotificationChannel(this, channelId, channelName, NotificationUtil.ImportanceLow);
            }
            downloadManager = DownloadManager;
            downloadManagerListener = new DownloadManagerListener(this);
            downloadManager.AddListener(downloadManagerListener);
        }


        //@Override
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {

            lastStartId = startId;
            string intentAction = null;
            if (intent != null)
            {
                intentAction = intent.Action;
                startedInForeground |= intent.GetBooleanExtra(KEY_FOREGROUND, false) || ACTION_RESTART.Equals(intentAction);
            }
            Logd("onStartCommand action: " + intentAction + " startId: " + startId);
            switch (intentAction)
            {
                case ACTION_INIT:
                case ACTION_RESTART:
                    // Do nothing. The RequirementsWatcher will start downloads when possible.
                    break;
                case ACTION_ADD:
                    byte[] actionData = intent.GetByteArrayExtra(KEY_DOWNLOAD_ACTION);
                    if (actionData == null)
                    {
                        Log.Error(TAG, "Ignoring ADD action with no action data");
                    }
                    else
                    {
                        try
                        {
                            downloadManager.HandleAction(actionData);
                        }
                        catch (IOException e)
                        {
                            Log.Error(TAG, "Failed to handle ADD action", e);
                        }
                    }
                    break;
                case ACTION_STOP_DOWNLOADS:
                    downloadManager.StopDownloads();
                    break;
                case ACTION_START_DOWNLOADS:
                    downloadManager.StartDownloads();
                    break;
                default:
                    Log.Error(TAG, "Ignoring unrecognized action: " + intentAction);
                    break;
            }
            MaybeStartWatchingRequirements();
            if (downloadManager.IsIdle)
            {
                Stop();
            }

            return StartCommandResult.Sticky;
        }

        //@Override
        public override void OnDestroy()
        {
            Logd("onDestroy");
            foregroundNotificationUpdater.StopPeriodicUpdates();
            downloadManager.RemoveListener((Offline.DownloadManager.IListener)downloadManagerListener);
            MaybeStopWatchingRequirements();
        }

        //@Nullable
        //@Override
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        /**
         * Returns a {@link DownloadManager} to be used to downloaded content. Called only once in the
         * life cycle of the service. The service will call {@link DownloadManager#startDownloads()} and
         * {@link DownloadManager#stopDownloads} as necessary when requirements returned by {@link
         * #getRequirements()} are met or stop being met.
         */
        protected abstract Offline.DownloadManager DownloadManager { get; }

        /**
         * Returns a {@link Scheduler} to restart the service when requirements allowing downloads to take
         * place are met. If {@code null}, the service will only be restarted if the process is still in
         * memory when the requirements are met.
         */
        //@Nullable
        protected abstract IScheduler Scheduler { get; }

        /**
         * Returns requirements for downloads to take place. By default the only requirement is that the
         * device has network connectivity.
         */
        protected Requirements GetRequirements()
        {
            return new Requirements(Requirements.NetworkTypeAny, false, false);
        }

        /**
         * Returns a notification to be displayed when this service running in the foreground.
         *
         * <p>This method is called when there is a task state change and periodically while there are
         * active tasks. The periodic update interval can be set using {@link #DownloadService(int,
         * long)}.
         *
         * <p>On API level 26 and above, this method may also be called just before the service stops,
         * with an empty {@code taskStates} array. The returned notification is used to satisfy system
         * requirements for foreground services.
         *
         * @param taskStates The states of all current tasks.
         * @return The foreground notification to display.
         */
        protected abstract Notification GetForegroundNotification(TaskState[] taskStates);

        /**
         * Called when the state of a task changes.
         *
         * @param taskState The state of the task.
         */
        protected virtual void OnTaskStateChanged(TaskState taskState)
        {
            // Do nothing.
        }

        private void MaybeStartWatchingRequirements()
        {
            if (downloadManager.DownloadCount == 0)
            {
                return;
            }

            if (!requirementsHelpers.ContainsKey(this))
            {
                RequirementsHelper requirementsHelper = new RequirementsHelper(this, GetRequirements(), Scheduler, this);
                requirementsHelpers.Add(this, requirementsHelper);
                requirementsHelper.Start();
                Logd("started watching requirements");
            }
        }

        private void MaybeStopWatchingRequirements()
        {
            if (downloadManager.DownloadCount > 0)
            {
                return;
            }

            if (requirementsHelpers.ContainsKey(this))
            {
                RequirementsHelper requirementsHelper = requirementsHelpers[this];

                requirementsHelpers.Remove(this);

                requirementsHelper.Stop();
                Logd("stopped watching requirements");
            }
        }

        private void Stop()
        {
            foregroundNotificationUpdater.StopPeriodicUpdates();
            // Make sure startForeground is called before stopping. Workaround for [Internal: b/69424260].
            if (startedInForeground && Utils.SdkInt >= 26)
            {
                foregroundNotificationUpdater.ShowNotificationIfNotAlready();
            }
            bool stopSelfResult = StopSelfResult(lastStartId);
            Logd("stopSelf(" + lastStartId + ") result: " + stopSelfResult);
        }

        private void Logd(string message)
        {
            if (DEBUG)
            {
                Log.Debug(TAG, message);
            }
        }

        private class DownloadManagerListener : Java.Lang.Object, IListener
        {
            DownloadService service;

            public DownloadManagerListener(DownloadService service)
            {
                this.service = service;
            }

            //@Override
            public void OnInitialized(Offline.DownloadManager downloadManager)
            {
                service.MaybeStartWatchingRequirements();
            }

            //@Override
            public void OnTaskStateChanged(Offline.DownloadManager downloadManager, TaskState taskState)
            {
                service.OnTaskStateChanged(taskState);

                if (taskState.State == TaskState.StateStarted)
                {
                    service.foregroundNotificationUpdater.StartPeriodicUpdates();
                }
                else
                {
                    service.foregroundNotificationUpdater.Update();
                }
            }

            //@Override
            public void OnIdle(Offline.DownloadManager downloadManager)
            {
                service.Stop();
            }
        }

        private class ForegroundNotificationUpdater : Java.Lang.Object, IRunnable
        {

            private int notificationId;
            private long updateInterval;
            private Handler handler;

            private bool periodicUpdatesStarted;
            private bool notificationDisplayed;
            private DownloadService service;

            public ForegroundNotificationUpdater(int notificationId, long updateInterval, DownloadService service)
            {
                this.notificationId = notificationId;
                this.updateInterval = updateInterval;
                this.handler = new Handler(Looper.MainLooper);
                this.service = service;
            }

            public void StartPeriodicUpdates()
            {
                periodicUpdatesStarted = true;
                Update();
            }

            public void StopPeriodicUpdates()
            {
                periodicUpdatesStarted = false;
                handler.RemoveCallbacks(this);
            }

            public void Update()
            {
                TaskState[] taskStates = service.downloadManager.GetAllTaskStates();
                service.StartForeground(notificationId, service.GetForegroundNotification(taskStates));
                notificationDisplayed = true;
                if (periodicUpdatesStarted)
                {
                    handler.RemoveCallbacks(this);
                    handler.PostDelayed(this, updateInterval);
                }
            }

            public void ShowNotificationIfNotAlready()
            {
                if (!notificationDisplayed)
                {
                    Update();
                }
            }

            //@Override
            public void Run()
            {
                Update();
            }
        }

        private class RequirementsHelper : Java.Lang.Object, RequirementsWatcher.IListener
        {

            private Context context;
            private Requirements requirements;
            private IScheduler scheduler;
            private DownloadService service;
            private RequirementsWatcher requirementsWatcher;

            internal RequirementsHelper(
                    Context context,
                    Requirements requirements,
                    IScheduler scheduler,
                    DownloadService service)
            {
                this.context = context;
                this.requirements = requirements;
                this.scheduler = scheduler;
                this.service = service;
                requirementsWatcher = new RequirementsWatcher(context, this, requirements);
            }

            public void Start()
            {
                requirementsWatcher.Start();
            }

            public void Stop()
            {
                requirementsWatcher.Stop();
                if (scheduler != null)
                {
                    scheduler.Cancel();
                }
            }

            //@Override
            public void RequirementsMet(RequirementsWatcher requirementsWatcher)
            {
                StartServiceWithAction(ACTION_START_DOWNLOADS);
                if (scheduler != null)
                {
                    scheduler.Cancel();
                }
            }

            //@Override
            public void RequirementsNotMet(RequirementsWatcher requirementsWatcher)
            {
                StartServiceWithAction(ACTION_STOP_DOWNLOADS);
                if (scheduler != null)
                {
                    string servicePackage = context.PackageName;
                    bool success = scheduler.Schedule(requirements, servicePackage, ACTION_RESTART);
                    if (!success)
                    {
                        Log.Error(TAG, "Scheduling downloads failed.");
                    }
                }
            }

            private void StartServiceWithAction(string action)
            {
                Intent intent = new Intent(context, service.GetType()).SetAction(action).PutExtra(KEY_FOREGROUND, true);
                Utils.StartForegroundService(context, intent);
            }
        }
    }

}
