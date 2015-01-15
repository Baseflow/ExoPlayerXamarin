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
		int count = 1;


		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			// Get our button from the layout resource,
			// and attach an event to it
			Button button = FindViewById<Button> (Resource.Id.myButton);
			
			button.Click += delegate {

				//ExoPlayerFactory exo = ExoPlayerFactory.NewInstance(1);

				// Construct the URL for the query
				string BASE_URL = "http://www.montemagno.com/sample.mp3";

				Android.Net.Uri builtUri = Android.Net.Uri.Parse(BASE_URL);

				// Build the sample source
				FrameworkSampleSource sampleSource = new FrameworkSampleSource(Application.Context, builtUri, null, 1);

				// Build the track renderers
				TrackRenderer audioRenderer = new MediaCodecAudioTrackRenderer(sampleSource, null, true);

				// Build the ExoPlayer and start playback
				var exo = ExoPlayerFactory.NewInstance(1);
				exo.Prepare(audioRenderer);
				exo.PlayWhenReady = true;

				button.Text = string.Format ("{0} clicks!", count++);
			};
		}
	}
}


