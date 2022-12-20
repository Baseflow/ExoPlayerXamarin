using Android.Content;

namespace ExoPlayer.Sample;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Set our view from the "main" layout resource
        SetContentView(Resource.Layout.activity_main);


        var HttpDataSourceFactory = new DefaultHttpDataSource.Factory().SetAllowCrossProtocolRedirects(true);
        var MainDataSource = new ProgressiveMediaSource.Factory(HttpDataSourceFactory);
        var Exoplayer = new IExoPlayer.Builder(Context).SetMediaSourceFactory(MainDataSource).Build();

        MediaItem mediaItem = MediaItem.FromUri(Android.Net.Uri.Parse("https://ia800806.us.archive.org/15/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3"));

        Exoplayer.AddMediaItem(mediaItem);
        Exoplayer.Prepare();
        Exoplayer.PlayWhenReady = true;
    }
}

