ExoPlayer Plugin for Xamarin
================

Xamarin bindings library for the Google ExoPlayer [library][ExoPlayer].

ExoPlayer is an application level media player for Android. It provides an
alternative to Android’s MediaPlayer API for playing audio and video both
locally and over the Internet. ExoPlayer supports features not currently
supported by Android’s MediaPlayer API, including DASH and SmoothStreaming
adaptive playbacks. Unlike the MediaPlayer API, ExoPlayer is easy to customize
and extend, and can be updated through Play Store application updates.

## Documentation ##

* The [developer guide][] provides a wealth of information.
* The [class reference][] documents ExoPlayer classes.
* The [release notes][] document the major changes in each release.
* Follow our [developer blog][] to keep up to date with the latest ExoPlayer
  developments!

[developer guide]: https://google.github.io/ExoPlayer/guide.html
[class reference]: https://google.github.io/ExoPlayer/doc/reference
[release notes]: https://github.com/google/ExoPlayer/blob/release-v2/RELEASENOTES.md
[developer blog]: https://medium.com/google-exoplayer

## Using ExoPlayer ##

The ExoPlayer plugin is available on [Nuget][Nuget].

```c#
protected Com.Google.Android.Exoplayer.IExoPlayer mediaPlayer;
if (mediaPlayer == null) 
{ 
	mediaPlayer = Com.Google.Android.Exoplayer.ExoPlayerFactory.NewInstance(1);
} 
Android.Net.Uri soundString = Android.Net.Uri.Parse("http://www.montemagno.com/sample.mp3");

FrameworkSampleSource sampleSource = new FrameworkSampleSource(this, soundString, null); 
TrackRenderer aRenderer = MediaCodecAudioTrackRenderer(sampleSource, MediaCodecSelector.Default);

mediaPlayer.Prepare(aRenderer);
mediaPlayer.PlayWhenReady = true;
```

See the Exoplayer.Droid sample app for further details.

Thanks to
=========

- [Nathan Barger][NathanBarger] for doing the initial porting work
- [MKuckert](https://github.com/MKuckert) for helping with bindings and samples
- [bspinner](https://github.com/bspinner) for helping with bindings and samples

License
=======

- **ExoPlayerXamarin** plugin is licensed under [MIT][mit]

[mit]: http://opensource.org/licenses/mit-license
[NathanBarger]: http://forums.xamarin.com/profile/NathanBarger
[ExoPlayer]: https://github.com/google/ExoPlayer
[Nuget]: https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/
[Developer]: http://developer.android.com/guide/topics/media/exoplayer.html
