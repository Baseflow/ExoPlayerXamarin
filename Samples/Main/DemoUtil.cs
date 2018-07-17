/*
 * Copyright (C) 2017 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using Android.Text;
using Com.Google.Android.Exoplayer2.Util;

namespace Com.Google.Android.Exoplayer2.Demo
{
	/**
	 * Utility methods for demo application.
	 */
	internal static class DemoUtil
	{
		/**
		 * Builds a track name for display.
		 *
		 * @param format {@link Format} of the track.
		 * @return a generated name specific to the track.
		 */
		public static string BuildTrackName(Format format)
		{
			string trackName;
			if (MimeTypes.IsVideo(format.SampleMimeType))
			{
				trackName = JoinWithSeparator(JoinWithSeparator(JoinWithSeparator(
					buildResolutionstring(format), buildBitratestring(format)), buildTrackIdstring(format)),
					buildSampleMimeTypestring(format));
			}
			else if (MimeTypes.IsAudio(format.SampleMimeType))
			{
				trackName = JoinWithSeparator(JoinWithSeparator(JoinWithSeparator(JoinWithSeparator(
					buildLanguagestring(format), buildAudioPropertystring(format)),
					buildBitratestring(format)), buildTrackIdstring(format)),
					buildSampleMimeTypestring(format));
			}
			else
			{
				trackName = JoinWithSeparator(JoinWithSeparator(JoinWithSeparator(buildLanguagestring(format),
					buildBitratestring(format)), buildTrackIdstring(format)),
					buildSampleMimeTypestring(format));
			}
			return trackName.Length == 0 ? "unknown" : trackName;
		}

		private static string buildResolutionstring(Format format)
		{
			return format.Width == Format.NoValue || format.Height == Format.NoValue
				? "" : format.Width + "x" + format.Height;
		}

		private static string buildAudioPropertystring(Format format)
		{
			return format.ChannelCount == Format.NoValue || format.SampleRate == Format.NoValue
				? "" : format.ChannelCount + "ch, " + format.SampleRate + "Hz";
		}

		private static string buildLanguagestring(Format format)
		{
			return TextUtils.IsEmpty(format.Language) || "und".Equals(format.Language) ? ""
				: format.Language;
		}

		private static string buildBitratestring(Format format)
		{
			return format.Bitrate == Format.NoValue ? ""
				: string.Format(CultureInfo.InvariantCulture, "{0}Mbit", format.Bitrate / 1000000f);
		}

		private static string JoinWithSeparator(string first, string second)
		{
			return first.Length == 0 ? second : (second.Length == 0 ? first : first + ", " + second);
		}

		private static string buildTrackIdstring(Format format)
		{
			return format.Id == null ? "" : ("id:" + format.Id);
		}

		private static string buildSampleMimeTypestring(Format format)
		{
			return format.SampleMimeType == null ? "" : format.SampleMimeType;
		}
	}
}