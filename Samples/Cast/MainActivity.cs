using Android.App;
using Android.Content;
using Android.Gms.Cast.Framework;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.Graphics;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.UI;
using static Android.Views.View;
using static Android.Widget.AdapterView;
using android = Android;

namespace Com.Google.Android.Exoplayer2.CastDemo
{
    [Activity(Label = "MainActivity")]
    public class MainActivity : AppCompatActivity, IOnClickListener, PlayerManager.IQueuePositionListener
    {

        private PlayerView localPlayerView;
        private PlayerControlView castControlView;
        private PlayerManager playerManager;
        private RecyclerView mediaQueueList;
        private MediaQueueListAdapter mediaQueueListAdapter;
        private CastContext castContext;

        // Activity lifecycle methods.

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Getting the cast context later than onStart can cause device discovery not to take place.
            castContext = CastContext.GetSharedInstance(this);

            SetContentView(Resource.Layout.main_activity);

            localPlayerView = (PlayerView)FindViewById(Resource.Id.local_player_view);
            localPlayerView.RequestFocus();

            castControlView = (PlayerControlView)FindViewById(Resource.Id.cast_control_view);

            mediaQueueListAdapter = new MediaQueueListAdapter();
            mediaQueueList = (RecyclerView)FindViewById(Resource.Id.sample_list);
            mediaQueueList.SetLayoutManager(new LinearLayoutManager(this));
            mediaQueueList.HasFixedSize = true;

            ItemTouchHelper helper = new ItemTouchHelper(new RecyclerViewCallback(playerManager, mediaQueueListAdapter));
            helper.AttachToRecyclerView(mediaQueueList);

            FindViewById(Resource.Id.add_sample_button).SetOnClickListener(this);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            base.OnCreateOptionsMenu(menu);
            MenuInflater.Inflate(Resource.Menu.menu, menu);
            CastButtonFactory.SetUpMediaRouteButton(this, menu, Resource.Id.media_route_menu_item);
            return true;
        }

        protected override void OnResume()
        {
            base.OnResume();
            playerManager = PlayerManager.CreatePlayerManager(this, localPlayerView, castControlView, this, castContext);
            mediaQueueListAdapter.PlayerManager = playerManager;
            mediaQueueList.SetAdapter(mediaQueueListAdapter);
        }

        protected override void OnPause()
        {
            base.OnPause();
            mediaQueueListAdapter.NotifyItemRangeRemoved(0, mediaQueueListAdapter.ItemCount);
            mediaQueueList.SetAdapter(null);
            playerManager.Release();
        }

        // Activity input.

        public override bool DispatchKeyEvent(KeyEvent @event)
        {
            //If the event was not handled then see if the player view can handle it.
            return base.DispatchKeyEvent(@event) || playerManager.DispatchKeyEvent(@event);
        }

        public void OnClick(View view)
        {
            new android.Support.V7.App.AlertDialog.Builder(this).SetTitle(Resource.String.sample_list_dialog_title)
                .SetView(BuildSampleListView()).SetPositiveButton(android.Resource.String.Ok, (IDialogInterfaceOnClickListener)null).Create()
                .Show();
        }

        // PlayerManager.QueuePositionListener implementation.

        public void OnQueuePositionChanged(int previousIndex, int newIndex)
        {
            if (previousIndex != C.IndexUnset)
            {
                mediaQueueListAdapter.NotifyItemChanged(previousIndex);
            }
            if (newIndex != C.IndexUnset)
            {
                mediaQueueListAdapter.NotifyItemChanged(newIndex);
            }
        }

        // Internal methods.

        private View BuildSampleListView()
        {
            View dialogList = LayoutInflater.Inflate(Resource.Layout.sample_list, null);
            ListView sampleList = (ListView)dialogList.FindViewById(Resource.Id.sample_list);
            sampleList.Adapter = new SampleListAdapter(this);
            sampleList.OnItemClickListener = new OnItemClickListener(playerManager, mediaQueueListAdapter);
            return dialogList;
        }

        private class OnItemClickListener : Java.Lang.Object, IOnItemClickListener
        {
            private PlayerManager playerManager;
            private MediaQueueListAdapter mediaQueueListAdapter;

            public OnItemClickListener(PlayerManager playerManager, MediaQueueListAdapter mediaQueueListAdapter)
            {
                this.playerManager = playerManager;
                this.mediaQueueListAdapter = mediaQueueListAdapter;
            }

            public void OnItemClick(AdapterView parent, View view, int position, long id)
            {
                playerManager.AddItem(DemoUtil.SAMPLES[position]);
                mediaQueueListAdapter.NotifyItemInserted(playerManager.GetMediaQueueSize() - 1);
            }
        }

        // Internal classes.

        private class QueueItemViewHolder : RecyclerView.ViewHolder, IOnClickListener
        {

            public readonly TextView textView;
            private PlayerManager playerManager;

            public QueueItemViewHolder(TextView textView, PlayerManager playerManager) : base(textView)
            {
                this.textView = textView;
                textView.SetOnClickListener(this);
                this.playerManager = playerManager;
            }

            public void OnClick(View v)
            {
                playerManager.SelectQueueItem(AdapterPosition);
            }
        }

        private class MediaQueueListAdapter : RecyclerView.Adapter
        {
            internal PlayerManager PlayerManager { get; set; }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                TextView v = (TextView)LayoutInflater.From(parent.Context).Inflate(android.Resource.Layout.SimpleListItem1, parent, false);
                return new QueueItemViewHolder(v, PlayerManager);
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                TextView view = (TextView)holder.ItemView;
                view.Text = PlayerManager.GetItem(position).name;
                //TODO: Solve coloring using the theme's ColorStateList.
                view.SetTextColor(new Color(ColorUtils.SetAlphaComponent(view.CurrentTextColor, position == PlayerManager.GetCurrentItemIndex() ? 255 : 100)));
            }

            public override int ItemCount
            {
                get { return PlayerManager.GetMediaQueueSize(); }
            }
        }

        private class RecyclerViewCallback : ItemTouchHelper.SimpleCallback
        {
            private int draggingFromPosition;
            private int draggingToPosition;
            private MediaQueueListAdapter mediaQueueListAdapter;
            private PlayerManager playerManager;

            public RecyclerViewCallback(PlayerManager playerManager, MediaQueueListAdapter mediaQueueListAdapter) : base(ItemTouchHelper.Up | ItemTouchHelper.Down, ItemTouchHelper.Start | ItemTouchHelper.End)
            {
                this.playerManager = playerManager;
                this.mediaQueueListAdapter = mediaQueueListAdapter;
                draggingFromPosition = C.IndexUnset;
                draggingToPosition = C.IndexUnset;
            }

            public override bool OnMove(RecyclerView list, RecyclerView.ViewHolder origin, RecyclerView.ViewHolder target)
            {
                int fromPosition = origin.AdapterPosition;
                int toPosition = target.AdapterPosition;
                if (draggingFromPosition == C.IndexUnset)
                {
                    // A drag has started, but changes to the media queue will be reflected in clearView().
                    draggingFromPosition = fromPosition;
                }
                draggingToPosition = toPosition;
                mediaQueueListAdapter.NotifyItemMoved(fromPosition, toPosition);
                return true;
            }

            public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
            {
                int position = viewHolder.AdapterPosition;
                if (playerManager.removeItem(position))
                {
                    mediaQueueListAdapter.NotifyItemRemoved(position);
                }
            }

            public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
            {
                base.ClearView(recyclerView, viewHolder);
                if (draggingFromPosition != C.IndexUnset)
                {
                    // A drag has ended. We reflect the media queue change in the player.
                    if (!playerManager.MoveItem(draggingFromPosition, draggingToPosition))
                    {
                        // The move failed. The entire sequence of onMove calls since the drag started needs to be
                        // invalidated.
                        mediaQueueListAdapter.NotifyDataSetChanged();
                    }
                }
                draggingFromPosition = C.IndexUnset;
                draggingToPosition = C.IndexUnset;
            }
        }

        private class SampleListAdapter : ArrayAdapter
        {
            public SampleListAdapter(Context context) : base(context, android.Resource.Layout.SimpleListItem1, DemoUtil.SAMPLES)
            {
            }
        }
    }
}
