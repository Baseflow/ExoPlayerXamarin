Media3 Plugin for MAUI (Android)
================

![license](https://img.shields.io/github/license/martijn00/ExoPlayerXamarin.svg)
[![Build status](https://ci.appveyor.com/api/projects/status/r2farwm2837vm86t?svg=true)](https://ci.appveyor.com/project/martijn00/exoplayerxamarin)
[![NuGet](https://img.shields.io/nuget/v/Xam.Plugins.Android.ExoPlayer.svg)](https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/Xam.Plugins.Android.ExoPlayer.svg)](https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/)
[![GitHub tag](https://img.shields.io/github/tag/martijn00/ExoPlayerXamarin.svg)](https://github.com/martijn00/ExoPlayerXamarin/releases)
[![MyGet](https://img.shields.io/myget/martijn00/ExoPlayerXamarin.svg)](https://www.myget.org/F/martijn00/api/v3/index.json)

MAUI bindings library for Media3 [library][Media3].

Media3 is an application level media player for Android. It provides an
alternative to Android’s MediaPlayer API for playing audio and video both
locally and over the Internet. Media3 supports features not currently
supported by Android’s MediaPlayer API, including DASH and SmoothStreaming
adaptive playbacks. Unlike the MediaPlayer API, Media3 is easy to customize
and extend, and can be updated through Play Store application updates.

# Support

* Feel free to open an issue. Make sure to use one of the templates!
* Commercial support is available. Integration with your app or services, samples, feature request, etc. Email: [hello@baseflow.com](mailto:hello@baseflow.com)
* Powered by: [baseflow.com](https://baseflow.com)

## Documentation ##
* The [migration guide][] provides information for developers migrating from ExoPlayer to Media3
* The [class reference][] documents the classes and methods.
* The [release notes][] document the major changes in each release.
* Follow our [developer blog][] to keep up to date with the latest developments!

[migration guide]: https://developer.android.com/guide/topics/media/media3/getting-started/migration-guide
[class reference]: https://developer.android.com/reference/androidx/media3/common/package-summary
[release notes]: https://github.com/androidx/media/blob/release/RELEASENOTES.md
[developer blog]: https://medium.com/google-exoplayer

## Using Media3 ##

The Media3 plugin is available on [Nuget][Nuget].

```c#
    var HttpDataSourceFactory = new DefaultHttpDataSource.Factory().SetAllowCrossProtocolRedirects(true);
    var MainDataSource = new ProgressiveMediaSource.Factory(HttpDataSourceFactory);
    var Exoplayer = new IExoPlayer.Builder(Context).SetMediaSourceFactory(MainDataSource).Build();

    MediaItem mediaItem = MediaItem.FromUri(Android.Net.Uri.Parse("https://ia800806.us.archive.org/15/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3"));

    Exoplayer.AddMediaItem(mediaItem);
    Exoplayer.Prepare();
    Exoplayer.PlayWhenReady = true;
```

See the Media3.Sample sample app for further details.

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
[Media3]: https://github.com/androidx/media
[Nuget]: https://www.nuget.org/packages/Xam.Plugins.Android.ExoPlayer/
[Developer]: http://developer.android.com/guide/topics/media/exoplayer.html
