/*
 * Copyright (C) 2016 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Util;
using Java.IO;
using Java.Lang;
using Java.Util;
using Java.Interop;

namespace Com.Google.Android.Exoplayer2.Demo
{
	/**
	 * An activity for selecting from a list of samples.
	 */
	public class SampleChooserActivity : Activity
	{
		private class ListItemSelectionListener : Object, ExpandableListView.IOnChildClickListener
		{
			private List<SampleGroup> groups;
			private SampleChooserActivity context;

			public ListItemSelectionListener(SampleChooserActivity context, List<SampleGroup> groups)
			{
				this.context = context;
				this.groups = groups;
			}

			public bool OnChildClick(ExpandableListView parent, View view, int groupPosition,
				int childPosition, long id)
			{
				context.onSampleSelected(groups[groupPosition].samples[childPosition]);
				return true;
			}
		}

		private const string TAG = "SampleChooserActivity";

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.sample_chooser_activity);
			var intent = Intent;
			var dataUri = intent.DataString;
			string[] uris;
			if (dataUri != null)
			{
				uris = new string[] { dataUri };
			}
			else
			{
				var uriList = new List<string>();
				var assetManager = Assets;
				try
				{
					foreach (var asset in assetManager.List(""))
					{
						if (asset.EndsWith(".exolist.json", System.StringComparison.Ordinal))
						{
							uriList.Add("asset:///" + asset);
						}
					}
				}
				catch (System.Exception)
				{
					Toast.MakeText(ApplicationContext, Resource.String.sample_list_load_error, ToastLength.Long)
						.Show();
				}
				uriList.Sort();
				uris = uriList.ToArray();
			}
			var loaderTask = new SampleListLoader(this);
			loaderTask.Execute(uris);
		}

		internal void onSampleGroups(List<SampleGroup> groups, bool sawError)
		{
			if (sawError)
			{
				Toast.MakeText(ApplicationContext, Resource.String.sample_list_load_error, ToastLength.Long)
					.Show();
			}
			var sampleList = FindViewById<ExpandableListView>(Resource.Id.sample_list);
			sampleList.SetAdapter(new SampleAdapter(this, groups));
			sampleList.SetOnChildClickListener(new ListItemSelectionListener(this, groups));
		}

		private void onSampleSelected(Sample sample)
		{
			StartActivity(sample.buildIntent(this));
		}

		private sealed class SampleListLoader : AsyncTask<string, Void, List<SampleGroup>>
		{
			private bool sawError;
			private SampleChooserActivity context;

			public SampleListLoader(SampleChooserActivity context)
			{
				this.context = context;
			}

			protected override List<SampleGroup> RunInBackground(params string[] @params)
			{
				var result = new List<SampleGroup>();
				var userAgent = Util.Util.GetUserAgent(context, "ExoPlayerDemo");
				var dataSource = new DefaultDataSource(context, null, userAgent, false);
				foreach (var uri in @params)
				{
					var dataSpec = new DataSpec(global::Android.Net.Uri.Parse(uri));
					var inputStream = new DataSourceInputStream(dataSource, dataSpec);
					var memory = new MemoryStream();
					var buffer = new byte[1024];
					int read;
					while ((read = inputStream.Read(buffer)) > 0)
					{
						memory.Write(buffer, 0, read);
					}
					memory.Seek(0, SeekOrigin.Begin);

					try
					{
						ReadSampleGroups(new JsonReader(new InputStreamReader(memory, "UTF-8")), result);
					}
					catch (System.Exception e)
					{
						Log.Error(TAG, "Error loading sample list: " + uri, e);
						sawError = true;
					}
					finally
					{
						Util.Util.CloseQuietly(dataSource);
					}
				}
				return result;
			}

			protected override void OnPostExecute(Object result)
			{
				base.OnPostExecute(result);
				// This overload is required so that the following overload is also called?! ¯\_(ツ)_/¯
			}

			protected override void OnPostExecute(List<SampleGroup> result)
			{
				base.OnPostExecute(result);
				context.onSampleGroups(result, sawError);
			}

			private void ReadSampleGroups(JsonReader reader, List<SampleGroup> groups)
			{
				reader.BeginArray();
				while (reader.HasNext)
				{
					ReadSampleGroup(reader, groups);
				}
				reader.EndArray();
			}

			private void ReadSampleGroup(JsonReader reader, List<SampleGroup> groups)
			{
				var groupName = "";
				var samples = new List<Sample>();

				reader.BeginObject();
				while (reader.HasNext)
				{
					var name = reader.NextName();
					switch (name)
					{
						case "name":
							groupName = reader.NextString();
							break;
						case "samples":
							reader.BeginArray();
							while (reader.HasNext)
							{
								samples.Add(ReadEntry(reader, false));
							}
							reader.EndArray();
							break;
						case "_comment":
							reader.NextString(); // Ignore.
							break;
						default:
							throw new ParserException("Unsupported name: " + name);
					}
				}
				reader.EndObject();

				SampleGroup group = GetGroup(groupName, groups);
				group.samples.AddRange(samples);
			}

			private Sample ReadEntry(JsonReader reader, bool insidePlaylist)
			{
				string sampleName = null;
				string uri = null;
				string extension = null;
				UUID drmUuid = null;
				string drmLicenseUrl = null;
				string[]
				drmKeyRequestProperties = null;
				var preferExtensionDecoders = false;
				List<UriSample> playlistSamples = null;
				string adTagUri = null;

				reader.BeginObject();
				while (reader.HasNext)
				{
					var name = reader.NextName();
					switch (name)
					{
						case "name":
							sampleName = reader.NextString();
							break;
						case "uri":
							uri = reader.NextString();
							break;
						case "extension":
							extension = reader.NextString();
							break;
						case "drm_scheme":
							Assertions.CheckState(!insidePlaylist, "Invalid attribute on nested item: drm_scheme");
							drmUuid = GetDrmUuid(reader.NextString());
							break;
						case "drm_license_url":
							Assertions.CheckState(!insidePlaylist,
								"Invalid attribute on nested item: drm_license_url");
							drmLicenseUrl = reader.NextString();
							break;
						case "drm_key_request_properties":
							Assertions.CheckState(!insidePlaylist,
								"Invalid attribute on nested item: drm_key_request_properties");
							var drmKeyRequestPropertiesList = new List<string>();
							reader.BeginObject();
							while (reader.HasNext)
							{
								drmKeyRequestPropertiesList.Add(reader.NextName());
								drmKeyRequestPropertiesList.Add(reader.NextString());
							}
							reader.EndObject();
							drmKeyRequestProperties = drmKeyRequestPropertiesList.ToArray();
							break;
						case "prefer_extension_decoders":
							Assertions.CheckState(!insidePlaylist,
								"Invalid attribute on nested item: prefer_extension_decoders");
							preferExtensionDecoders = reader.NextBoolean();
							break;
						case "playlist":
							Assertions.CheckState(!insidePlaylist, "Invalid nesting of playlists");
							playlistSamples = new List<UriSample>();
							reader.BeginArray();
							while (reader.HasNext)
							{
								playlistSamples.Add((UriSample)ReadEntry(reader, true));
							}
							reader.EndArray();
							break;
						case "ad_tag_uri":
							adTagUri = reader.NextString();
							break;
						default:
							throw new ParserException("Unsupported attribute name: " + name);
					}
				}
				reader.EndObject();

				if (playlistSamples != null)
				{
					var playlistSamplesArray = playlistSamples.ToArray();
					return new PlaylistSample(sampleName, drmUuid, drmLicenseUrl, drmKeyRequestProperties,
						preferExtensionDecoders, playlistSamplesArray);
				}
				else
				{
					return new UriSample(sampleName, drmUuid, drmLicenseUrl, drmKeyRequestProperties,
						preferExtensionDecoders, uri, extension, adTagUri);
				}
			}

			private SampleGroup GetGroup(string groupName, List<SampleGroup> groups)
			{
				for (int i = 0; i < groups.Count; i++)
				{
					if (Util.Util.AreEqual(groupName, groups[i].title))
					{
						return groups[i];
					}
				}
				var group = new SampleGroup(groupName);
				groups.Add(group);
				return group;
			}

			private UUID GetDrmUuid(string typeString)
			{
				switch (typeString.ToLowerInvariant())
				{
					case "widevine":
						return C.WidevineUuid;
					case "playready":
						return C.PlayreadyUuid;
					case "cenc":
						return C.ClearkeyUuid;
					default:
						try
						{
							return UUID.FromString(typeString);
						}
						catch (RuntimeException)
						{
							throw new ParserException("Unsupported drm type: " + typeString);
						}
				}
			}
		}

		private class SampleAdapter : BaseExpandableListAdapter
		{
			private readonly Context context;
			private readonly List<SampleGroup> sampleGroups;

			public SampleAdapter(Context context, List<SampleGroup> sampleGroups)
			{
				this.context = context;
				this.sampleGroups = sampleGroups;
			}

			public override Object GetChild(int groupPosition, int childPosition)
			{
				return sampleGroups[groupPosition].samples[childPosition];
			}

			public override long GetChildId(int groupPosition, int childPosition)
			{
				return childPosition;
			}

			public override View GetChildView(int groupPosition, int childPosition, bool isLastChild,
				View convertView, ViewGroup parent)
			{
				View view = convertView;
				if (view == null)
				{
					view = LayoutInflater.From(context).Inflate(global::Android.Resource.Layout.SimpleListItem1, parent,
						false);
				}
				((TextView)view).Text = sampleGroups[groupPosition].samples[childPosition].name;
				return view;
			}

			public override int GetChildrenCount(int groupPosition)
			{
				return sampleGroups[groupPosition].samples.Count;
			}

			public override Object GetGroup(int groupPosition)
			{
				return sampleGroups[groupPosition];
			}

			public override long GetGroupId(int groupPosition)
			{
				return groupPosition;
			}

			public override View GetGroupView(int groupPosition, bool isExpanded, View convertView,
				ViewGroup parent)
			{
				View view = convertView;
				if (view == null)
				{
					view = LayoutInflater.From(context).Inflate(global::Android.Resource.Layout.SimpleExpandableListItem1,
						parent, false);
				}
				((TextView)view).Text = sampleGroups[groupPosition].title;
				return view;
			}

			public override int GroupCount => sampleGroups.Count;

			public override bool HasStableIds => false;

			public override bool IsChildSelectable(int groupPosition, int childPosition)
			{
				return true;
			}
		}

		internal sealed class SampleGroup : Object
		{

			public readonly string title;
			public readonly List<Sample> samples;

			public SampleGroup(string title)
			{
				this.title = title;
				samples = new List<Sample>();
			}
		}

		internal abstract class Sample : Object
		{
			public readonly string name;
			public readonly bool preferExtensionDecoders;
			public readonly UUID drmSchemeUuid;
			public readonly string drmLicenseUrl;
			public readonly string[] drmKeyRequestProperties;

			public Sample(string name, UUID drmSchemeUuid, string drmLicenseUrl,
				string[] drmKeyRequestProperties, bool preferExtensionDecoders)
			{
				this.name = name;
				this.drmSchemeUuid = drmSchemeUuid;
				this.drmLicenseUrl = drmLicenseUrl;
				this.drmKeyRequestProperties = drmKeyRequestProperties;
				this.preferExtensionDecoders = preferExtensionDecoders;
			}

			public virtual Intent buildIntent(Context context)
			{
				Intent intent = new Intent(context, typeof(PlayerActivity));
				intent.PutExtra(PlayerActivity.PREFER_EXTENSION_DECODERS, preferExtensionDecoders);
				if (drmSchemeUuid != null)
				{
					intent.PutExtra(PlayerActivity.DRM_SCHEME_UUID_EXTRA, drmSchemeUuid.ToString());
					intent.PutExtra(PlayerActivity.DRM_LICENSE_URL, drmLicenseUrl);
					intent.PutExtra(PlayerActivity.DRM_KEY_REQUEST_PROPERTIES, drmKeyRequestProperties);
				}
				return intent;
			}
		}

		internal class UriSample : Sample
		{
			public readonly string uri;
			public readonly string extension;
			public readonly string adTagUri;

			public UriSample(string name, UUID drmSchemeUuid, string drmLicenseUrl,
				string[] drmKeyRequestProperties, bool preferExtensionDecoders, string uri,
							 string extension, string adTagUri) : base(name, drmSchemeUuid, drmLicenseUrl, drmKeyRequestProperties, preferExtensionDecoders)
			{
				this.uri = uri;
				this.extension = extension;
				this.adTagUri = adTagUri;
			}

			public override Intent buildIntent(Context context)
			{
				return base.buildIntent(context)
					.SetData(global::Android.Net.Uri.Parse(uri))
					.PutExtra(PlayerActivity.EXTENSION_EXTRA, extension)
					.PutExtra(PlayerActivity.AD_TAG_URI_EXTRA, adTagUri)
					.SetAction(PlayerActivity.ACTION_VIEW);
			}
		}

		internal class PlaylistSample : Sample
		{
			public readonly UriSample[] children;

			public PlaylistSample(string name, UUID drmSchemeUuid, string drmLicenseUrl,
				string[] drmKeyRequestProperties, bool preferExtensionDecoders,
								  params UriSample[] children) : base(name, drmSchemeUuid, drmLicenseUrl, drmKeyRequestProperties, preferExtensionDecoders)
			{
				this.children = children;
			}

			public override Intent buildIntent(Context context)
			{
				var uris = new string[children.Length];
				var extensions = new string[children.Length];
				for (int i = 0; i < children.Length; i++)
				{
					uris[i] = children[i].uri;
					extensions[i] = children[i].extension;
				}
				return base.buildIntent(context)
					.PutExtra(PlayerActivity.URI_LIST_EXTRA, uris)
					.PutExtra(PlayerActivity.EXTENSION_LIST_EXTRA, extensions)
					.SetAction(PlayerActivity.ACTION_VIEW_LIST);
			}
		}
	}
}