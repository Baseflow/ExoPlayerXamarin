ExoPlayer Plugin for Xamarin
================

Xamarin bindings library for the Google ExoPlayer [library][ExoPlayer].

For more information on ExoPlayer see the Android [Developer documentation][Developer].

Plugin is available on [Nuget][Nuget].

Documentation
=============

    protected Com.Google.Android.Exoplayer.IExoPlayer mediaPlayer;
    if (mediaPlayer == null) 
    { 
    	mediaPlayer = Com.Google.Android.Exoplayer.ExoPlayerFactory.NewInstance(1);
    } 
    Android.Net.Uri soundString = Android.Net.Uri.Parse("http://www.montemagno.com/sample.mp3");
    
    FrameworkSampleSource sampleSource = new FrameworkSampleSource(this, soundString, null); 
    TrackRenderer aRenderer = new MediaCodecAudioTrackRenderer(sampleSource, null, true); 
    
    mediaPlayer.Prepare(aRenderer);
    mediaPlayer.PlayWhenReady = true;


See the Exoplayer.Droid sample app.

Thanks to
=========

- [Nathan Barger][NathanBarger] for doing the initial porting work
- [MKuckert](https://github.com/MKuckert) for helping with bindings and samples

License
=======

- **ExoPlayerXamarin** plugin is licensed under [MIT][mit]

[mit]: http://opensource.org/licenses/mit-license
[NathanBarger]: http://forums.xamarin.com/profile/NathanBarger
[ExoPlayer]: https://github.com/google/ExoPlayer
[Nuget]: https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/
[Developer]: http://developer.android.com/guide/topics/media/exoplayer.html
