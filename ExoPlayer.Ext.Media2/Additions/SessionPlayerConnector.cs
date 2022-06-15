using System;
using System.Collections.Generic;
using System.Text;
using Android.Runtime;
using Java.Interop;

namespace Com.Google.Android.Exoplayer2.Ext.Media2
{
    public partial class SessionPlayerConnector : global::AndroidX.Medai2.Common.SessionPlayer
    
    {
        // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2.ext.media2']/class[@name='SessionPlayerConnector']/method[@name='setPlaylist' and count(parameter)=2 and parameter[1][@type='java.util.List&lt;androidx.media2.common.MediaItem&gt;'] and parameter[2][@type='androidx.media2.common.MediaMetadata']]"
        [Register("setPlaylist", "(Ljava/util/List;Landroidx/media2/common/MediaMetadata;)Lcom/google/common/util/concurrent/ListenableFuture;", "")]
        public unsafe override global::Google.Common.Util.Concurrent.IListenableFuture? SetPlaylist(global::System.Collections.Generic.IList<global::AndroidX.Medai2.Common.MediaItem>? playlist, global::AndroidX.Medai2.Common.MediaMetadata? metadata)
        {
            const string __id = "setPlaylist.(Ljava/util/List;Landroidx/media2/common/MediaMetadata;)Lcom/google/common/util/concurrent/ListenableFuture;";
            IntPtr native_playlist = global::Android.Runtime.JavaList<global::AndroidX.Medai2.Common.MediaItem>.ToLocalJniHandle(playlist);
            try
            {
                JniArgumentValue* __args = stackalloc JniArgumentValue[2];
                __args[0] = new JniArgumentValue(native_playlist);
                __args[1] = new JniArgumentValue((metadata == null) ? IntPtr.Zero : ((global::Java.Lang.Object)metadata).Handle);
                var __rm = _members.InstanceMethods.InvokeAbstractObjectMethod(__id, this, __args);
                return global::Java.Lang.Object.GetObject<global::Google.Common.Util.Concurrent.IListenableFuture>(__rm.Handle, JniHandleOwnership.TransferLocalRef);
            }
            finally
            {
                JNIEnv.DeleteLocalRef(native_playlist);
                global::System.GC.KeepAlive(playlist);
                global::System.GC.KeepAlive(metadata);
            }
        }
    }
}
