using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Com.Google.Android.Exoplayer2.Text
{
	public partial class SimpleSubtitleDecoder
	{
		static IntPtr id_createInputBuffer;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2.text']/class[@name='SimpleSubtitleDecoder']/method[@name='createInputBuffer' and count(parameter)=0]"
		[Register("createInputBuffer", "()Lcom/google/android/exoplayer2/text/SubtitleInputBuffer;", "")]
		protected override unsafe Java.Lang.Object CreateInputBuffer()
		{
			if (id_createInputBuffer == IntPtr.Zero)
				id_createInputBuffer = JNIEnv.GetMethodID(class_ref, "createInputBuffer", "()Lcom/google/android/exoplayer2/text/SubtitleInputBuffer;");
			try
			{
				return global::Java.Lang.Object.GetObject<global::Com.Google.Android.Exoplayer2.Text.SubtitleInputBuffer>(JNIEnv.CallObjectMethod(((global::Java.Lang.Object)this).Handle, id_createInputBuffer), JniHandleOwnership.TransferLocalRef);
			}
			finally
			{
			}
		}

		static IntPtr id_createOutputBuffer;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2.text']/class[@name='SimpleSubtitleDecoder']/method[@name='createOutputBuffer' and count(parameter)=0]"
		[Register("createOutputBuffer", "()Lcom/google/android/exoplayer2/text/SubtitleOutputBuffer;", "")]
		protected override unsafe Java.Lang.Object CreateOutputBuffer()
		{
			if (id_createOutputBuffer == IntPtr.Zero)
				id_createOutputBuffer = JNIEnv.GetMethodID(class_ref, "createOutputBuffer", "()Lcom/google/android/exoplayer2/text/SubtitleOutputBuffer;");
			try
			{
				return global::Java.Lang.Object.GetObject<global::Com.Google.Android.Exoplayer2.Text.SubtitleOutputBuffer>(JNIEnv.CallObjectMethod(((global::Java.Lang.Object)this).Handle, id_createOutputBuffer), JniHandleOwnership.TransferLocalRef);
			}
			finally
			{
			}
		}
        /*
		static IntPtr id_decode_Lcom_google_android_exoplayer2_text_SubtitleInputBuffer_Lcom_google_android_exoplayer2_text_SubtitleOutputBuffer_Z;
		// Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2.text']/class[@name='SimpleSubtitleDecoder']/method[@name='decode' and count(parameter)=3 and parameter[1][@type='com.google.android.exoplayer2.text.SubtitleInputBuffer'] and parameter[2][@type='com.google.android.exoplayer2.text.SubtitleOutputBuffer'] and parameter[3][@type='boolean']]"
		[Register("decode", "(Lcom/google/android/exoplayer2/text/SubtitleInputBuffer;Lcom/google/android/exoplayer2/text/SubtitleOutputBuffer;Z)Lcom/google/android/exoplayer2/text/SubtitleDecoderException;", "")]
		protected override unsafe global::Java.Lang.Object Decode(global::Java.Lang.Object p0, global::Java.Lang.Object p1, bool p2)
		{
			if (id_decode_Lcom_google_android_exoplayer2_text_SubtitleInputBuffer_Lcom_google_android_exoplayer2_text_SubtitleOutputBuffer_Z == IntPtr.Zero)
				id_decode_Lcom_google_android_exoplayer2_text_SubtitleInputBuffer_Lcom_google_android_exoplayer2_text_SubtitleOutputBuffer_Z = JNIEnv.GetMethodID(class_ref, "decode", "(Lcom/google/android/exoplayer2/text/SubtitleInputBuffer;Lcom/google/android/exoplayer2/text/SubtitleOutputBuffer;Z)Lcom/google/android/exoplayer2/text/SubtitleDecoderException;");
			try
			{
				JValue* __args = stackalloc JValue[3];
				__args[0] = new JValue(p0);
				__args[1] = new JValue(p1);
				__args[2] = new JValue(p2);
				global::Com.Google.Android.Exoplayer2.Text.SubtitleDecoderException __ret = global::Java.Lang.Object.GetObject<global::Com.Google.Android.Exoplayer2.Text.SubtitleDecoderException>(JNIEnv.CallObjectMethod(((global::Java.Lang.Object)this).Handle, id_decode_Lcom_google_android_exoplayer2_text_SubtitleInputBuffer_Lcom_google_android_exoplayer2_text_SubtitleOutputBuffer_Z, __args), JniHandleOwnership.TransferLocalRef);
				return __ret;
			}
			finally
			{
			}
		}*/
	}
}