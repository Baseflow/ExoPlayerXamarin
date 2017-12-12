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
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Audio;
using Com.Google.Android.Exoplayer2.Decoder;
using Com.Google.Android.Exoplayer2.Drm;

namespace Com.Google.Android.Exoplayer2.Ext.Flac
{
    public partial class LibflacAudioRenderer : global::Com.Google.Android.Exoplayer2.Audio.SimpleDecoderAudioRenderer
    {
        /// <summary>
        /// This method just exists to solve building errors of the binding project, so don't use it.
        /// </summary>
        /// <returns>null</returns>
        protected override SimpleDecoder CreateDecoder(Format p0, IExoMediaCrypto p1)
        {
            return null;
        }

       
    }
}