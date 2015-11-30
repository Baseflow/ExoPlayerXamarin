using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer.Util;
using Java.Lang;
using AndroidResource = Android.Resource;

namespace Com.Google.Android.Exoplayer.Demo
{
/**
 * An activity for selecting from a number of samples.
 */

	[Activity(
		Name = "com.google.android.exoplayer.demo.SampleChooserActivity",
		ConfigurationChanges = ConfigChanges.KeyboardHidden,
		MainLauncher = true,
		Label = "@string/application_name"
		)]
	public class SampleChooserActivity : Activity
	{

		private const string TAG = "SampleChooserActivity";

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.sample_chooser_activity);

			ListView sampleList = FindViewById<ListView>(Resource.Id.sample_list);
			var sampleAdapter = new SampleAdapter(this);

			sampleAdapter.Add(new Header("YouTube DASH"));
			sampleAdapter.AddAll(Samples.YOUTUBE_DASH_MP4);
			sampleAdapter.Add(new Header("Widevine GTS DASH"));
			sampleAdapter.AddAll(Samples.WIDEVINE_GTS);
			sampleAdapter.Add(new Header("SmoothStreaming"));
			sampleAdapter.AddAll(Samples.SMOOTHSTREAMING);
			sampleAdapter.Add(new Header("HLS"));
			sampleAdapter.AddAll(Samples.HLS);
			sampleAdapter.Add(new Header("Misc"));
			sampleAdapter.AddAll(Samples.MISC);

			// Add WebM samples if the device has a VP9 decoder.
			try
			{
				if (MediaCodecUtil.GetDecoderInfo(MimeTypes.VideoVp9, false) != null)
				{
					sampleAdapter.Add(new Header("YouTube WebM DASH (Experimental)"));
					sampleAdapter.AddAll(Samples.YOUTUBE_DASH_WEBM);
				}
			}
			catch (MediaCodecUtil.DecoderQueryException e)
			{
				Log.Error(TAG, "Failed to query vp9 decoder", e);
			}

			sampleList.Adapter = sampleAdapter;
			sampleList.ItemClick += (sender, args) =>
			{
				var item = sampleAdapter.GetItem(args.Position);
				var sample = item as Samples.Sample;
				if (sample != null)
				{
					onSampleSelected(sample);
				}
			};
		}

		private void onSampleSelected(Samples.Sample sample)
		{
			var mpdIntent = new Intent(this, typeof (PlayerActivity))
				.SetData(Uri.Parse(sample.uri))
				.PutExtra(PlayerActivity.CONTENT_ID_EXTRA, sample.contentId)
				.PutExtra(PlayerActivity.CONTENT_TYPE_EXTRA, sample.type);
			StartActivity(mpdIntent);
		}

		internal class SampleAdapter : ArrayAdapter<Object>
		{

			public SampleAdapter(Context context) : base(context, 0)
			{

			}

			public override View GetView(int position, View convertView, ViewGroup parent)
			{
				var view = convertView;
				if (view == null)
				{
					int layoutId = GetItemViewType(position) == 1
						? AndroidResource.Layout.SimpleListItem1
						: Resource.Layout.sample_chooser_inline_header;
					view = LayoutInflater.From(Context).Inflate(layoutId, null, false);
				}
				Object item = GetItem(position);
				string name = null;
				if (item is Samples.Sample)
				{
					name = ((Samples.Sample) item).name;
				}
				else if (item is Header)
				{
					name = ((Header) item).name;
				}
				((TextView) view).Text = name;
				return view;
			}

			public override int GetItemViewType(int position)
			{
				return (GetItem(position) is Samples.Sample) ? 1 : 0;
			}

			public override int ViewTypeCount
			{
				get { return 2; }
			}
		}

		internal class Header : Object
		{
			public readonly string name;

			public Header(string name)
			{
				this.name = name;
			}
		}
	}
}