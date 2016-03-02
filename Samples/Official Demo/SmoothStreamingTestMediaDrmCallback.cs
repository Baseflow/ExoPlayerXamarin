/*
 * Copyright (C) 2014 The Android Open Source Project
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
using Android.Media;
using Com.Google.Android.Exoplayer.Drm;
using Com.Google.Android.Exoplayer.Util;
using Java.Lang;
using Java.Util;

namespace Com.Google.Android.Exoplayer.Demo
{
	/// <summary>
	/// Demo <see cref="StreamingDrmSessionManager"/> for smooth streaming test content.
	/// </summary>
	public class SmoothStreamingTestMediaDrmCallback : Object, IMediaDrmCallback
	{
		private const string PlayreadyTestDefaultUri =
			"http://playready.directtaps.net/pr/svc/rightsmanager.asmx";
		private static readonly IDictionary<string, string> KeyRequestProperties = new Dictionary<string, string>
		{
			{"Content-Type", "text/xml"},
			{"SOAPAction", "http://schemas.microsoft.com/DRM/2007/03/protocols/AcquireLicense"}
		};

		public byte[] ExecuteProvisionRequest(UUID uuid, MediaDrm.ProvisionRequest request)
		{
			var url = request.DefaultUrl + "&signedRequest=" + System.Text.Encoding.ASCII.GetString(request.GetData());
			return ExoPlayerUtil.ExecutePost(url, null, null);
		}

		public byte[] ExecuteKeyRequest(UUID uuid, MediaDrm.KeyRequest request)
		{
			var url = request.DefaultUrl;
			if (string.IsNullOrEmpty(url))
			{
				url = PlayreadyTestDefaultUri;
			}
			return ExoPlayerUtil.ExecutePost(url, request.GetData(), KeyRequestProperties);
		}

	}
}