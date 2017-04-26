using System.Diagnostics;

namespace YetAnotherFaviconDownloader
{
    public sealed class Util
    {
        public static void Log(string format, params object[] args)
        {
            Debug.Print("[YAFD] " + format, args);
        }
    }
}
