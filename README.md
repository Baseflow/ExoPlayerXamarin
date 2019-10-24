ExoPlayer Plugin for Xamarin
================

![license](https://img.shields.io/github/license/martijn00/ExoPlayerXamarin.svg)
[![Build status](https://ci.appveyor.com/api/projects/status/r2farwm2837vm86t?svg=true)](https://ci.appveyor.com/project/martijn00/exoplayerxamarin)
[![NuGet](https://img.shields.io/nuget/v/Xam.Plugins.Android.ExoPlayer.svg)](https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Xam.Plugins.Android.ExoPlayer.svg)](https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/)
[![GitHub tag](https://img.shields.io/github/tag/martijn00/ExoPlayerXamarin.svg)](https://github.com/martijn00/ExoPlayerXamarin/releases)
[![MyGet](https://img.shields.io/myget/martijn00/ExoPlayerXamarin.svg)](https://www.myget.org/F/martijn00/api/v3/index.json)

Xamarin bindings library for the Google ExoPlayer [library][ExoPlayer].

ExoPlayer is an application level media player for Android. It provides an
alternative to Android’s MediaPlayer API for playing audio and video both
locally and over the Internet. ExoPlayer supports features not currently
supported by Android’s MediaPlayer API, including DASH and SmoothStreaming
adaptive playbacks. Unlike the MediaPlayer API, ExoPlayer is easy to customize
and extend, and can be updated through Play Store application updates.

# Support

* Feel free to open an issue. Make sure to use one of the templates!
* Commercial support is available. Integration with your app or services, samples, feature request, etc. Email: [hello@baseflow.com](mailto:hello@baseflow.com)
* Powered by: [baseflow.com](https://baseflow.com)

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
    SimpleExoPlayer _player;
    var mediaUri = Android.Net.Uri.Parse("https://ia800806.us.archive.org/15/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3");

    var userAgent = Util.GetUserAgent(context, "ExoPlayerDemo");
    var defaultHttpDataSourceFactory = new DefaultHttpDataSourceFactory(userAgent);
    var defaultDataSourceFactory = new DefaultDataSourceFactory(context, null, defaultHttpDataSourceFactory);
    var extractorMediaSource = new ExtractorMediaSource(mediaUri, defaultDataSourceFactory, new DefaultExtractorsFactory(), null, null);
    var defaultBandwidthMeter = new DefaultBandwidthMeter();
    var adaptiveTrackSelectionFactory = new AdaptiveTrackSelection.Factory(defaultBandwidthMeter);
    var defaultTrackSelector = new DefaultTrackSelector(adaptiveTrackSelectionFactory);

    _player = ExoPlayerFactory.NewSimpleInstance(context, defaultTrackSelector);
    _player.Prepare(extractorMediaSource);
    _player.PlayWhenReady = true;
```

See the Exoplayer.Droid sample app for further details.

**IMPORTANT: Exoplayer 2.9.0 and up requires Visual Studio 2019 with R8 and D8. You also need to enable AAPT2. Readmore at: https://devblogs.microsoft.com/xamarin/androids-d8-dexer-and-r8-shrinker/**

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
