using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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
            object[] parameters = new object[] { null };
            bool result = false;

            Type type = typeof(KeePassLib.Serialization.IOConnection);
            if (type != null)
            {
                MethodInfo methodInfo = type.GetMethod("GetWebProxy", BindingFlags.Static | BindingFlags.NonPublic);
                if (methodInfo != null)
                {
                    result = (bool)methodInfo.Invoke(null, parameters);
                }
            }

            return parameters[0] as IWebProxy;
        }

        public static byte[] HashData(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        public static List<T> RemoveDuplicates<T>(List<T> source)
        {
            Dictionary<T, object> set = new Dictionary<T, object>();
            List<T> result = new List<T>(source.Count);
            bool hasNull = false;

            for (int i = 0; i < source.Count; i++)
            {
                T item = source[i];
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
            StringBuilder sb = new StringBuilder(hash.Length * 2);

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
