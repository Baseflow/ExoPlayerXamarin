using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using Java.Interop;

namespace Com.Google.Android.Exoplayer2.Ext.Cast
{
    public sealed partial class CastPlayer : global::Com.Google.Android.Exoplayer2.BasePlayer
    {
        public override unsafe bool PlayWhenReady
        {
            // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2']/class[@name='SimpleExoPlayer']/method[@name='getPlayWhenReady' and count(parameter)=0]"
            [Register("getPlayWhenReady", "()Z", "GetGetPlayWhenReadyHandler")]
            get
            {
                const string __id = "getPlayWhenReady.()Z";
                try
                {
                    var __rm = _members.InstanceMethods.InvokeVirtualBooleanMethod(__id, this, null);
                    return __rm;
                }
                finally
                {
                }
            }
            // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2']/class[@name='SimpleExoPlayer']/method[@name='setPlayWhenReady' and count(parameter)=1 and parameter[1][@type='boolean']]"
            [Register("setPlayWhenReady", "(Z)V", "GetSetPlayWhenReady_ZHandler")]
            set
            {
                const string __id = "setPlayWhenReady.(Z)V";
                try
                {
                    JniArgumentValue* __args = stackalloc JniArgumentValue[1];
                    __args[0] = new JniArgumentValue(value);
                    _members.InstanceMethods.InvokeVirtualVoidMethod(__id, this, __args);
                }
                finally
                {
                }
            }
        }

        public override unsafe int RepeatMode
        {
            // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2']/class[@name='SimpleExoPlayer']/method[@name='getRepeatMode' and count(parameter)=0]"
            [Register("getRepeatMode", "()I", "GetGetRepeatModeHandler")]
            get
            {
                const string __id = "getRepeatMode.()I";
                try
                {
                    var __rm = _members.InstanceMethods.InvokeVirtualInt32Method(__id, this, null);
                    return __rm;
                }
                finally
                {
                }
            }
            // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2']/class[@name='SimpleExoPlayer']/method[@name='setRepeatMode' and count(parameter)=1 and parameter[1][@type='int']]"
            [Register("setRepeatMode", "(I)V", "GetSetRepeatMode_IHandler")]
            set
            {
                const string __id = "setRepeatMode.(I)V";
                try
                {
                    JniArgumentValue* __args = stackalloc JniArgumentValue[1];
                    __args[0] = new JniArgumentValue(value);
                    _members.InstanceMethods.InvokeVirtualVoidMethod(__id, this, __args);
                }
                finally
                {
                }
            }
        }

        public override unsafe bool ShuffleModeEnabled
        {
            // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2']/class[@name='SimpleExoPlayer']/method[@name='getShuffleModeEnabled' and count(parameter)=0]"
            [Register("getShuffleModeEnabled", "()Z", "GetGetShuffleModeEnabledHandler")]
            get
            {
                const string __id = "getShuffleModeEnabled.()Z";
                try
                {
                    var __rm = _members.InstanceMethods.InvokeVirtualBooleanMethod(__id, this, null);
                    return __rm;
                }
                finally
                {
                }
            }
            // Metadata.xml XPath method reference: path="/api/package[@name='com.google.android.exoplayer2']/class[@name='SimpleExoPlayer']/method[@name='setShuffleModeEnabled' and count(parameter)=1 and parameter[1][@type='boolean']]"
            [Register("setShuffleModeEnabled", "(Z)V", "GetSetShuffleModeEnabled_ZHandler")]
            set
            {
                const string __id = "setShuffleModeEnabled.(Z)V";
                try
                {
                    JniArgumentValue* __args = stackalloc JniArgumentValue[1];
                    __args[0] = new JniArgumentValue(value);
                    _members.InstanceMethods.InvokeVirtualVoidMethod(__id, this, __args);
                }
                finally
                {
                }
            }
        }
    }
}
