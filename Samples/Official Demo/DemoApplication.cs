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

using Android.App;
using Com.Google.Android.Exoplayer2.Upstream;
using Utils = Com.Google.Android.Exoplayer2.Util.Util;

namespace Com.Google.Android.Exoplayer2.Demo
{
	/**
	 * Placeholder application to facilitate overriding Application methods for debugging and testing.
	 */
	public class DemoApplication : Application
	{
		protected string userAgent;

		public override void OnCreate()
		{
			base.OnCreate();
			userAgent = Utils.GetUserAgent(this, "ExoPlayerDemo");
		}

		public IDataSourceFactory BuildDataSourceFactory(DefaultBandwidthMeter bandwidthMeter)
		{
			return new DefaultDataSourceFactory(this, bandwidthMeter,
				BuildHttpDataSourceFactory(bandwidthMeter));
		}

		public IHttpDataSourceFactory BuildHttpDataSourceFactory(DefaultBandwidthMeter bandwidthMeter)
		{
			return new DefaultHttpDataSourceFactory(userAgent, bandwidthMeter);
		}

		public bool UseExtensionRenderers()
		{
			return BuildConfig.Flavor.Equals("withExtensions");
		}
	}
}