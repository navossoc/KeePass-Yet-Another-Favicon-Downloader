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
            var result = (bool)typeof(KeePassLib.Serialization.IOConnection)
                ?.GetMethod("GetWebProxy", BindingFlags.Static | BindingFlags.NonPublic)
                ?.Invoke(null, parameters);

            return parameters[0] as IWebProxy;
        }

        public static void Log(string format, params object[] args)
        {
            Debug.Print("[YAFD] " + format, args);
        }
    }
}
