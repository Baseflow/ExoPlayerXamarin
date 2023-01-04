using Android.Content;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Upstream;

namespace ExoPlayer.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        SetContentView(Resource.Layout.activity_main);

        var exoPlayerView = FindViewById<StyledPlayerView>(Resource.Id.player_view_sample);

        var HttpDataSourceFactory = new DefaultHttpDataSource.Factory().SetAllowCrossProtocolRedirects(true);
        var MainDataSource = new ProgressiveMediaSource.Factory(HttpDataSourceFactory);
        var Exoplayer = new IExoPlayer.Builder(this.ApplicationContext).SetMediaSourceFactory(MainDataSource).Build();

        var mediaItem1 = MediaItem.FromUri(Android.Net.Uri.Parse("https://ia800806.us.archive.org/15/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3"));
        var mediaItem2 = MediaItem.FromUri(Android.Net.Uri.Parse("http://clips.vorwaerts-gmbh.de/big_buck_bunny.mp4"));

        exoPlayerView.Player = Exoplayer;
        exoPlayerView.Player.AddMediaItem(mediaItem1);
        exoPlayerView.Player.AddMediaItem(mediaItem2);
        exoPlayerView.Player.Prepare();
        exoPlayerView.Player.PlayWhenReady = true;
    }
}

