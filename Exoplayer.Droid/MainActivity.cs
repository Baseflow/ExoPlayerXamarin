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
					//mediaPlayer.AddListener(this); 
				} 
				Android.Net.Uri soundString = Android.Net.Uri.Parse("http://www.montemagno.com/sample.mp3");

				FrameworkSampleSource sampleSource = new FrameworkSampleSource(this, soundString, null, 1); 
				TrackRenderer aRenderer = new MediaCodecAudioTrackRenderer(sampleSource, null, true); 
				mediaPlayer.Prepare(aRenderer);
				mediaPlayer.PlayWhenReady = true;


				//ExoPlayerFactory exo = ExoPlayerFactory.NewInstance(1);

				// Construct the URL for the query
				string BASE_URL = "http://www.montemagno.com/sample.mp3";

				Android.Net.Uri builtUri = Android.Net.Uri.Parse(BASE_URL);

				button.Text = string.Format ("Status: {0}", mediaPlayer.PlaybackState);
			};
		}
	}
}


