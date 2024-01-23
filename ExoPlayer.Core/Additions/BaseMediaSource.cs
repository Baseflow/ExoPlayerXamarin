using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;

namespace Com.Google.Android.Exoplayer2.Source
{

    // Metadata.xml XPath class reference: path="/api/package[@name='com.google.android.exoplayer2.source']/class[@name='BaseMediaSource']"
    public abstract partial class BaseMediaSource : global::Java.Lang.Object, global::Com.Google.Android.Exoplayer2.Source.IMediaSource
    {
        //public abstract global::Java.Lang.Object Tag { get; }
    }

    public partial class DefaultMediaSourceFactory : global::Com.Google.Android.Exoplayer2.Source.IMediaSourceFactory
    {
        //public abstract global::Java.Lang.Object Tag { get; }
        
    }


    internal partial class BaseMediaSourceInvoker : BaseMediaSource
    {
        //public override Java.Lang.Object Tag => throw new NotImplementedException();
    }
}
