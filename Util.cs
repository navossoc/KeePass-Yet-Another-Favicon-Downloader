using System.Net;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

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

        public static byte[] HashData(byte[] data)
        {
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        public static List<T> RemoveDuplicates<T>(List<T> source)
        {
            var set = new Dictionary<T, object>();
            var result = new List<T>(source.Count);
            var hasNull = false;

            for (var i = 0; i < source.Count; i++)
            {
                var item = source[i];
                if (item == null)
                {
                    if (!hasNull)
                    {
                        hasNull = true;
                        result.Add(item);
                    }
                }
                else
                {
                    if (!set.ContainsKey(item))
                    {
                        set.Add(item, null);
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        public static string ToHex(byte[] hash)
        {
            var sb = new StringBuilder(hash.Length * 2);

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

        public static void Log(string format, params object[] args)
        {
#if DEBUG
            Debug.Print("[YAFD] " + format, args);
#endif
        }
    }
}
