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

using Java.Lang;

namespace Com.Google.Android.Exoplayer.Demo
{
	/// <summary>
	/// Holds statically defined sample definitions.
	/// </summary>
	internal static class Samples
	{
		public class Sample : Object
		{
			public readonly string Name;
			public readonly string ContentId;
			public readonly string Uri;
			public readonly int Type;

			public Sample(string name, string uri, int type)
				: this(name, name.ToLower().Replace(" ", ""), uri, type)
			{
			}

			public Sample(string name, string contentId, string uri, int type)
			{
				Name = name;
				ContentId = contentId;
				Uri = uri;
				Type = type;
			}
		}

		public static readonly Sample[] YoutubeDashMp4 =
		{
			new Sample("Google Glass",
				"http://www.youtube.com/api/manifest/dash/id/bf5bb2419360daf1/source/youtube?"
				+ "as=fmp4_audio_clear,fmp4_sd_hd_clear&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0&"
				+ "ipbits=0&expire=19000000000&signature=51AF5F39AB0CEC3E5497CD9C900EBFEAECCCB5C7."
				+ "8506521BFC350652163895D4C26DEE124209AA9E&key=ik0", PlayerActivity.TypeDash),
			new Sample("Google Play",
				"http://www.youtube.com/api/manifest/dash/id/3aa39fa2cc27967f/source/youtube?"
				+ "as=fmp4_audio_clear,fmp4_sd_hd_clear&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0&"
				+ "ipbits=0&expire=19000000000&signature=A2716F75795F5D2AF0E88962FFCD10DB79384F29."
				+ "84308FF04844498CE6FBCE4731507882B8307798&key=ik0", PlayerActivity.TypeDash)
		};

		public static readonly Sample[] YoutubeDashWebm =
		{
			new Sample("Google Glass",
				"http://www.youtube.com/api/manifest/dash/id/bf5bb2419360daf1/source/youtube?"
				+ "as=fmp4_audio_clear,webm2_sd_hd_clear&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0&"
				+ "ipbits=0&expire=19000000000&signature=249B04F79E984D7F86B4D8DB48AE6FAF41C17AB3."
				+ "7B9F0EC0505E1566E59B8E488E9419F253DDF413&key=ik0", PlayerActivity.TypeDash),
			new Sample("Google Play",
				"http://www.youtube.com/api/manifest/dash/id/3aa39fa2cc27967f/source/youtube?"
				+ "as=fmp4_audio_clear,webm2_sd_hd_clear&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0&"
				+ "ipbits=0&expire=19000000000&signature=B1C2A74783AC1CC4865EB312D7DD2D48230CC9FD."
				+ "BD153B9882175F1F94BFE5141A5482313EA38E8D&key=ik0", PlayerActivity.TypeDash)
		};

		public static readonly Sample[] Smoothstreaming =
		{
			new Sample("Super speed",
				"http://playready.directtaps.net/smoothstreaming/SSWSS720H264/SuperSpeedway_720.ism",
				PlayerActivity.TypeSs),
			new Sample("Super speed (PlayReady)",
				"http://playready.directtaps.net/smoothstreaming/SSWSS720H264PR/SuperSpeedway_720.ism",
				PlayerActivity.TypeSs)
		};

		public static readonly Sample[] WidevineGts =
		{
			new Sample("WV: HDCP not specified", "d286538032258a1c",
				"http://www.youtube.com/api/manifest/dash/id/d286538032258a1c/source/youtube?"
				+ "as=fmp4_audio_cenc,fmp4_sd_hd_cenc&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0"
				+ "&ipbits=0&expire=19000000000&signature=477CF7D478BE26C205045D507E9358F85F84C065."
				+ "8971631EB657BC33EC2F48A2FF4211956760C3E9&key=ik0", PlayerActivity.TypeDash),
			new Sample("WV: HDCP not required", "48fcc369939ac96c",
				"http://www.youtube.com/api/manifest/dash/id/48fcc369939ac96c/source/youtube?"
				+ "as=fmp4_audio_cenc,fmp4_sd_hd_cenc&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0"
				+ "&ipbits=0&expire=19000000000&signature=171DAE48D00B5BE7434BC1A9F84DAE0463C7EA7A."
				+ "0925B4DBB5605BEE9F5D088C48F25F5108E96191&key=ik0", PlayerActivity.TypeDash),
			new Sample("WV: HDCP required", "e06c39f1151da3df",
				"http://www.youtube.com/api/manifest/dash/id/e06c39f1151da3df/source/youtube?"
				+ "as=fmp4_audio_cenc,fmp4_sd_hd_cenc&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0"
				+ "&ipbits=0&expire=19000000000&signature=8D3B8AF4E3F72B7F127C8D0D39B7AFCF37B30519."
				+ "A118BADEBF3582AD2CC257B0EE6E579C6955D8AA&key=ik0", PlayerActivity.TypeDash),
			new Sample("WV: Secure video path required", "0894c7c8719b28a0",
				"http://www.youtube.com/api/manifest/dash/id/0894c7c8719b28a0/source/youtube?"
				+ "as=fmp4_audio_cenc,fmp4_sd_hd_cenc&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0"
				+ "&ipbits=0&expire=19000000000&signature=A41D835C7387885A4A820628F57E481E00095931."
				+ "9D50DBEEB5E37344647EE11BDA129A7FCDE8B7B9&key=ik0", PlayerActivity.TypeDash),
			new Sample("WV: HDCP + secure video path required", "efd045b1eb61888a",
				"http://www.youtube.com/api/manifest/dash/id/efd045b1eb61888a/source/youtube?"
				+ "as=fmp4_audio_cenc,fmp4_sd_hd_cenc&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0"
				+ "&ipbits=0&expire=19000000000&signature=A97C9032C9D0C74F1643DB17C178873887C229E4."
				+ "0A657BF6F23C8BC1538F276137383478330B76DE&key=ik0", PlayerActivity.TypeDash),
			new Sample("WV: 30s license duration (fails at ~30s)", "f9a34cab7b05881a",
				"http://www.youtube.com/api/manifest/dash/id/f9a34cab7b05881a/source/youtube?"
				+ "as=fmp4_audio_cenc,fmp4_sd_hd_cenc&sparams=ip,ipbits,expire,source,id,as&ip=0.0.0.0"
				+ "&ipbits=0&expire=19000000000&signature=80648A12A7D5FC1FA02B52B4250E4EB74CF0C5FD."
				+ "66A261130CA137AA5C541EA9CED2DBF240829EE6&key=ik0", PlayerActivity.TypeDash)
		};

		public static readonly Sample[] Hls =
		{
			new Sample("Apple master playlist",
				"https://devimages.apple.com.edgekey.net/streaming/examples/bipbop_4x3/"
				+ "bipbop_4x3_variant.m3u8", PlayerActivity.TypeHls),
			new Sample("Apple master playlist advanced",
				"https://devimages.apple.com.edgekey.net/streaming/examples/bipbop_16x9/"
				+ "bipbop_16x9_variant.m3u8", PlayerActivity.TypeHls),
			new Sample("Apple TS media playlist",
				"https://devimages.apple.com.edgekey.net/streaming/examples/bipbop_4x3/gear1/"
				+ "prog_index.m3u8", PlayerActivity.TypeHls),
			new Sample("Apple AAC media playlist",
				"https://devimages.apple.com.edgekey.net/streaming/examples/bipbop_4x3/gear0/"
				+ "prog_index.m3u8", PlayerActivity.TypeHls),
			new Sample("Apple ID3 metadata", "http://devimages.apple.com/samplecode/adDemo/ad.m3u8",
				PlayerActivity.TypeHls)
		};

		public static readonly Sample[] Misc =
		{
			new Sample("Dizzy", "http://html5demos.com/assets/dizzy.mp4", PlayerActivity.TypeOther),
			new Sample("Apple AAC 10s", "https://devimages.apple.com.edgekey.net/"
			                            + "streaming/examples/bipbop_4x3/gear0/fileSequence0.aac", PlayerActivity.TypeOther),
			new Sample("Apple TS 10s", "https://devimages.apple.com.edgekey.net/streaming/examples/"
			                           + "bipbop_4x3/gear1/fileSequence0.ts", PlayerActivity.TypeOther),
			new Sample("Android screens (Matroska)", "http://storage.googleapis.com/exoplayer-test-media-1/"
			                                         + "mkv/android-screens-lavf-56.36.100-aac-avc-main-1280x720.mkv",
				PlayerActivity.TypeOther),
			new Sample("Big Buck Bunny (MP4 Video)",
				"http://redirector.c.youtube.com/videoplayback?id=604ed5ce52eda7ee&itag=22&source=youtube&"
				+ "sparams=ip,ipbits,expire,source,id&ip=0.0.0.0&ipbits=0&expire=19000000000&signature="
				+ "513F28C7FDCBEC60A66C86C9A393556C99DC47FB.04C88036EEE12565A1ED864A875A58F15D8B5300"
				+ "&key=ik0", PlayerActivity.TypeOther),
			new Sample("Google Play (MP3 Audio)",
				"http://storage.googleapis.com/exoplayer-test-media-0/play.mp3", PlayerActivity.TypeOther),
			new Sample("Google Glass (WebM Video with Vorbis Audio)",
				"http://demos.webmproject.org/exoplayer/glass_vp9_vorbis.webm", PlayerActivity.TypeOther)
		};
	}
}