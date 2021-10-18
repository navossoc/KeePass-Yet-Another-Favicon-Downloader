using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YetAnotherFaviconDownloader
{
    static class ProviderList
    {
        public static readonly string CustomURLName = "Custom URL";

        private static readonly List<Provider> providers = new List<Provider>();
        private static readonly Provider customProvider = new Provider(CustomURLName, null);

        static ProviderList()
        {
            providers.Add(new Provider("None (Default)", null));
            providers.Add(new Provider("DuckDuckGo", "https://icons.duckduckgo.com/ip3/{URL:HOST}.ico"));
            providers.Add(new Provider("Favicon Kit", "https://api.faviconkit.com/{URL:HOST}/{YAFD:ICON_SIZE}"));
            providers.Add(new Provider("Google", "https://www.google.com/s2/favicons?domain={URL:HOST}&sz={YAFD:ICON_SIZE}"));
            providers.Add(new Provider("Yandex", "https://favicon.yandex.net/favicon/{URL:HOST}"));
            providers.Add(customProvider);
        }

        public static Provider[] GetDefaultList()
        {
            return providers.ToArray();
        }

        public static Provider FindByURL(string url)
        {
            return providers.Find(x => x.URL == url);
        }

        public static void SetCustomProviderURL(string url)
        {
            customProvider.URL = url;
        }

        public static bool IsValidURL(string url)
        {
            // HTTP URI schema
            var httpSchema = new Regex(@"^http(s)?://.+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (!httpSchema.IsMatch(url))
            {
                return false;
            }

            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }
    }
}
