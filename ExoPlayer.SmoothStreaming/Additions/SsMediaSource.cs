using System;
using System.Collections.Generic;
using Android.Runtime;
using Java.Interop;

namespace Com.Google.Android.Exoplayer2.Source.Smoothstreaming
{
    public sealed partial class SsMediaSource : global::Com.Google.Android.Exoplayer2.Source.BaseMediaSource, global::Com.Google.Android.Exoplayer2.Upstream.Loader.ICallback
    {
        // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2.source.ads']/class[@name='AdsMediaSource']/method[@name='releasePeriod' and count(parameter)=1 and parameter[1][@type='com.google.android.exoplayer2.source.MediaPeriod']]"
        [Register("releasePeriod", "(Lcom/google/android/exoplayer2/source/MediaPeriod;)V", "")]
        public override unsafe void ReleasePeriod(global::Com.Google.Android.Exoplayer2.Source.IMediaPeriod mediaPeriod)
        {
            const string __id = "releasePeriod.(Lcom/google/android/exoplayer2/source/MediaPeriod;)V";
            try
            {
                JniArgumentValue* __args = stackalloc JniArgumentValue[1];
                __args[0] = new JniArgumentValue((mediaPeriod == null) ? IntPtr.Zero : ((global::Java.Lang.Object)mediaPeriod).Handle);
                _members.InstanceMethods.InvokeAbstractVoidMethod(__id, this, __args);
            }
            finally
            {
            }
        }
    }
}
