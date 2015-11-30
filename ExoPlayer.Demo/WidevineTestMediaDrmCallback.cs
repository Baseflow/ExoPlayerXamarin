using Android.Media;
using Com.Google.Android.Exoplayer.Drm;
using Java.Lang;
using Java.Util;

namespace Com.Google.Android.Exoplayer.Demo
{
/**
 * A {@link MediaDrmCallback} for Widevine test content.
 */

    public class WidevineTestMediaDrmCallback : Object, IMediaDrmCallback
    {

        private static readonly string WIDEVINE_GTS_DEFAULT_BASE_URI =
            "http://wv-staging-proxy.appspot.com/proxy?provider=YouTube&video_id=";

        private readonly string defaultUri;

        public WidevineTestMediaDrmCallback(string videoId)
        {
            defaultUri = WIDEVINE_GTS_DEFAULT_BASE_URI + videoId;
        }

        public byte[] ExecuteProvisionRequest(UUID uuid, MediaDrm.ProvisionRequest request)
        {
            var url = request.DefaultUrl + "&signedRequest=" + System.Text.Encoding.ASCII.GetString(request.GetData());
            return Util.Util.ExecutePost(url, null, null);
        }

        public byte[] ExecuteKeyRequest(UUID uuid, MediaDrm.KeyRequest request)
        {
            var url = request.DefaultUrl;
            if (string.IsNullOrEmpty(url))
            {
                url = defaultUri;
            }
            return Util.Util.ExecutePost(url, request.GetData(), null);
        }
    }
}