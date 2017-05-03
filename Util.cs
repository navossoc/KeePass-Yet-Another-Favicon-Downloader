using System.Net;
using System.Diagnostics;
using System.Reflection;

namespace YetAnotherFaviconDownloader
{
    public sealed class Util
    {
        /// <summary>
        /// Get Proxy configuration from KeePass.
        /// </summary>
        /// <returns>Returns a proxy instance.</returns>
        /// <remarks>This is a little hacky, but they made me do it!</remarks>
        public static IWebProxy GetKeePassProxy()
        {
            var parameters = new object[] { null };
            bool result = false;

            var type = typeof(KeePassLib.Serialization.IOConnection);
            if (type != null)
            {
                var methodInfo = type.GetMethod("GetWebProxy", BindingFlags.Static | BindingFlags.NonPublic);
                if (methodInfo != null)
                {
                    result = (bool)methodInfo.Invoke(null, parameters);
                }
            }

            return parameters[0] as IWebProxy;
        }

        public static void Log(string format, params object[] args)
        {
#if DEBUG
            Debug.Print("[YAFD] " + format, args);
#endif
        }
    }
}
