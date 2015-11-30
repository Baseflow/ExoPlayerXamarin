using System.Collections.Generic;
using Android.Media;
using Com.Google.Android.Exoplayer.Drm;
using Java.Lang;
using Java.Util;

namespace Com.Google.Android.Exoplayer.Demo
{
/**
 * Demo {@link StreamingDrmSessionManager} for smooth streaming test content.
 */
    public class SmoothStreamingTestMediaDrmCallback : Object, IMediaDrmCallback
    {

        private const string PLAYREADY_TEST_DEFAULT_URI =
            "http://playready.directtaps.net/pr/svc/rightsmanager.asmx";
        private static readonly IDictionary<string, string> KEY_REQUEST_PROPERTIES = new Dictionary<string, string>
        {
            {"Content-Type", "text/xml"},
            {"SOAPAction", "http://schemas.microsoft.com/DRM/2007/03/protocols/AcquireLicense"}
        };

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
                url = PLAYREADY_TEST_DEFAULT_URI;
            }
            return Util.Util.ExecutePost(url, request.GetData(), KEY_REQUEST_PROPERTIES);
        }

    }
}