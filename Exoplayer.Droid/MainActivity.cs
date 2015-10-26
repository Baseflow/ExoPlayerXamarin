using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Com.Google.Android.Exoplayer;


namespace Exoplayer.Droid
{
	[Activity (Label = "Exoplayer.Droid", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		protected Com.Google.Android.Exoplayer.IExoPlayer mediaPlayer; 

        const int BUFFER_SEGMENT_SIZE = 64 * 1024;
        const int BUFFER_SEGMENT_COUNT = 256;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);
			
			button.Click += delegate {
				if (mediaPlayer == null) 
				{ 
					mediaPlayer = Com.Google.Android.Exoplayer.ExoPlayerFactory.NewInstance(1);
				} 
				Android.Net.Uri soundString = Android.Net.Uri.Parse("http://www.montemagno.com/sample.mp3");

//				FrameworkSampleSource sampleSource = new FrameworkSampleSource(this, soundString, null); 
//				TrackRenderer aRenderer = new MediaCodecAudioTrackRenderer(sampleSource, null, true); 

                String userAgent = Com.Google.Android.Exoplayer.Util.Util.GetUserAgent(this, "ExoPlayerDemo");
                var allocator = new Com.Google.Android.Exoplayer.Upstream.DefaultAllocator(BUFFER_SEGMENT_SIZE);
                var dataSource = new Com.Google.Android.Exoplayer.Upstream.DefaultUriDataSource(this, userAgent);
                Com.Google.Android.Exoplayer.Extractor.ExtractorSampleSource sampleSource = 
                    new Com.Google.Android.Exoplayer.Extractor.ExtractorSampleSource(soundString, dataSource, allocator,
                    BUFFER_SEGMENT_COUNT * BUFFER_SEGMENT_SIZE);
                MediaCodecAudioTrackRenderer aRenderer = new MediaCodecAudioTrackRenderer(sampleSource);

				mediaPlayer.Prepare(aRenderer);
				mediaPlayer.PlayWhenReady = true;

				button.Text = string.Format ("Status: {0}", mediaPlayer.PlaybackState);
			};
		}
	}
}


