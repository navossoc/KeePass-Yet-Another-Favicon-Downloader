using KeePassLib.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace YetAnotherFaviconDownloader
{
    [System.ComponentModel.DesignerCategory("")]
    public sealed class FaviconDownloader : WebClient
    {
        // Proxy
        private static IWebProxy _proxy;
        public static new IWebProxy Proxy { get { return _proxy; } set { _proxy = value; } }
        // User Agent
        private static readonly string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.71 Safari/537.36";

        // Regular expressions
        private static readonly Regex dataSchema, httpSchema;
        private static readonly Regex headTag, baseTag, commentTag, scriptStyleTag;
        private static readonly Regex linkTags, relAttribute, hrefAttribute;

        // URI after redirection
        private Uri responseUri;
        private CookieContainer cookieContainer;

        static FaviconDownloader()
        {
            // Data URI schema
            dataSchema = new Regex(@"data:(?<mediatype>.*?)(;(?<base64>.+?))?,(?<data>.+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);

            // HTTP URI schema
            httpSchema = new Regex(@"^http(s)?://.+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            // <head> tag
            headTag = new Regex(@"<head\b.*?>.*?</head>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

            // <!-- --> comment tags
            commentTag = new Regex(@"<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);

            // <script> or <style> tags
            scriptStyleTag = new Regex(@"<(script|style)\b.*?>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

            // <base> tags
            baseTag = new Regex(@"<base\b.*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

            // <link> tags
            linkTags = new Regex(@"<link\b.*?>", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline);

            // <link> tags with rel attribute
            relAttribute = new Regex(@"rel\s*=\s*(icon\b|(?<q>'|"")\s*(shortcut\s*\b)?icon\b\s*\k<q>)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Singleline);

            // <link> tags with href attribute
            hrefAttribute = new Regex(@"href\s*=\s*((?<q>'|"")(?<url>.*?)(\k<q>|>)|(?<url>.*?)(\s+|>))", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Singleline);

            // Enable TLS for newer .NET versions
            // Copy and paste from KeePass 2.46 source
            try
            {
                SecurityProtocolType spt = (SecurityProtocolType.Ssl3 |
                    SecurityProtocolType.Tls);

                // The flags Tls11 and Tls12 in SecurityProtocolType have been
                // introduced in .NET 4.5 and must not be set when running under
                // older .NET versions (otherwise an exception is thrown)
                Type tSpt = typeof(SecurityProtocolType);
                string[] vSpt = Enum.GetNames(tSpt);
                foreach (string strSpt in vSpt)
                {
                    if (strSpt.Equals("Tls11", StrUtil.CaseIgnoreCmp) ||
                        strSpt.Equals("Tls12", StrUtil.CaseIgnoreCmp) ||
                        strSpt.Equals("Tls13", StrUtil.CaseIgnoreCmp))  // .NET 4.8
                        spt |= (SecurityProtocolType)Enum.Parse(tSpt, strSpt, true);
                }

                ServicePointManager.SecurityProtocol = spt;
            }
            catch (Exception) { }
        }

        public FaviconDownloader()
        {
            cookieContainer = new CookieContainer();
        }

        public byte[] GetIcon(string url)
        {
            // This is how this is supposed to work:

            // 1. If a scheme is specified in the URL, we will use it for all requests, otherwise we will
            // try to prefix the URL with https (if user option is enabled) and later fallback to http.

            // 2. Try to simplify the URL striping the path and query.

            // For example, given the following input:
            // user:password@www.contoso.com:80/Home/Index.htm?q1=v1&q2=v2

            // Change to https scheme
            // Scheme (https)
            // 1a. https://user:password@www.contoso.com:80/Home/Index.htm?q1=v1&q2=v2

            // Strip path and query
            // Host
            // 2a. https://user:password@www.contoso.com:80/

            // Fallback to http scheme
            // Scheme (http)
            // 1b. http://user:password@www.contoso.com:80/Home/Index.htm?q1=v1&q2=v2

            // Strip path and query
            // Host
            // 2b. http://user:password@www.contoso.com:80/

            ////////////////////////////////////////////////////////////////////////////////

            // We prefer https first (just to preserve the original link)
            string origURL = url;

            // Check if the URL could be a site address
            if (!IsValidURL(ref url, "https://"))
            {
                throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
            }

            int attemps = 0;

        retry_http:

            // Just to avoid some weird looping
            if (++attemps > 4)
            {
                throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.Error);
            }
            Util.Log("Attempt {0}: {1}", attemps, url);

            try
            {
                Uri address = new Uri(url);

                // Download
                string page = DownloadPage(address);
                string head = StripPage(page);
                IEnumerable<Uri> links = GetIconsUrl(responseUri, head);

                // Try to find a valid image
                foreach (Uri link in links)
                {
                    try
                    {
                        // Download file
                        byte[] data = DownloadAsset(link);

                        // Check if the data is a valid image and then try to resize it
                        if (ResizeImage(ref data))
                        {
                            return data;
                        }
                    }
                    catch (WebException)
                    {
                        // ignore the exception and try the next resource
                    }
                }

                // No valid image found

                // Inspect the URL if it has a path or query the problem might be that
                if (address.PathAndQuery != "/")
                {
                    // Let's try just without a path and query
                    url = address.GetLeftPart(UriPartial.Authority);
                    goto retry_http;
                }
            }
            catch (WebException ex)
            {
                // Retry with HTTP prefix (*only* if user has automatic prefix enabled)
                if (!httpSchema.IsMatch(origURL) && url.StartsWith("https://"))
                {
                    // Restore original URL
                    url = origURL;

                    // Change the scheme from HTTPS to HTTP
                    if (IsValidURL(ref url, "http://"))
                    {
                        goto retry_http;
                    }
                }

                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
                }
                else
                {
                    throw new FaviconDownloaderException(ex);
                }
            }

            // If there is no file available
            throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
        }

        public byte[] GetIconCustomProvider(string url)
        {
            // Get the hostname from the requested URL
            var hostname = GetValidHost(url);

            // Custom provider settings
            var providerURL = YetAnotherFaviconDownloaderExt.Config.GetCustomDownloadProvider();
            var iconSize = YetAnotherFaviconDownloaderExt.Config.GetMaximumIconSize().ToString();

            // Follows KeePass placeholders convention
            // https://keepass.info/help/base/placeholders.html

            // Maybe in the future we can give full/proper support, well, not today, for now it's enough
            providerURL = Regex.Replace(providerURL, "{URL:HOST}", hostname, RegexOptions.IgnoreCase);
            providerURL = Regex.Replace(providerURL, "{YAFD:ICON_SIZE}", iconSize, RegexOptions.IgnoreCase);

            Uri address = new Uri(providerURL);

            Util.Log("CustomProvider: {0} => {1}", url, providerURL);

            try
            {
                // Download file
                byte[] data = DownloadData(address);

                // Check if the data is a valid image and then try to resize it
                if (ResizeImage(ref data))
                {
                    return data;
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
                }
                else
                {
                    throw new FaviconDownloaderException(ex);
               }
            }

            // If there is no file available
            throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
        }

        public string GetValidHost(string url)
        {
            if (!httpSchema.IsMatch(url))
            {
                // Prefix the URL with a valid schema
                url = "http://" + url;
            }

            Uri result;
            if (Uri.TryCreate(url, UriKind.Absolute, out result))
            {
                return result.Host;
            }

            // we shouldn't see this case
            return "";
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;

            // Set up proxy information
            request.Proxy = Proxy;

            // Set up timeout values (1/5 of the default values)
            request.Timeout = 20 * 1000;
            request.ReadWriteTimeout = 60 * 1000;
            request.ContinueTimeout = 1000;

            // Follow redirection responses with an HTTP status code from 300 to 399
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 10;

            // Sets the cookies associated with the request
            request.CookieContainer = cookieContainer;

            // Sets a fake user agent
            request.UserAgent = userAgent;

            // Set up additional headers (to looks more like a real browser)
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.9");
            request.AutomaticDecompression |= DecompressionMethods.GZip | DecompressionMethods.Deflate; // Accept-Encoding

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = null;
            try
            {
                response = base.GetWebResponse(request);
                // keeps track about base path 
                responseUri = response.ResponseUri;
            }
            catch (WebException)
            {
                // not handling here
            }

            return response;
        }

        private byte[] DownloadAsset(Uri address)
        {
            // Data URI scheme
            if (address.Scheme == "data")
            {
                string uri = address.ToString();

                // data:[<mediatype>][;base64],<data>
                Match match = dataSchema.Match(uri);
                if (match.Success)
                {
                    string data = match.Groups["data"].Value;

                    try
                    {
                        return Convert.FromBase64String(data);
                    }
                    catch (FormatException)
                    {
                        // For now, consider as invalid data
                        return null;
                    }
                }

                return null;
            }

            // HTTP//HTTPS scheme
            if (address.Scheme == "http" || address.Scheme == "https")
            {
                // Download file
                return DownloadData(address);
            }

            // TODO: Should allow other protocols here? (need research)
            return null;
        }

        private bool IsValidURL(ref string url, string prefix)
        {
            if (!httpSchema.IsMatch(url))
            {
                // If the user doesn't want to add the prefix, there is nothing I can do about
                if (!YetAnotherFaviconDownloaderExt.Config.GetAutomaticPrefixURLs())
                {
                    return false;
                }

                // Prefix the URL with a valid schema
                string old = url;
                url = prefix + url;
                Util.Log("AutoPrefix: {0} => {1}", old, url);
            }

            Uri result;
            return Uri.TryCreate(url, UriKind.Absolute, out result);
        }

        private string DownloadPage(Uri address)
        {
            // TODO: handle encoding issues
            string html = DownloadString(address);

            return html;
        }

        private string StripPage(string html)
        {
            // Extract <head> tag
            Match match = headTag.Match(html);
            if (match.Success)
            {
                // <head> content
                html = match.Value;

                // Remove HTML comments
                html = commentTag.Replace(html, string.Empty);

                // Remove some unnecessary tags from the page
                html = scriptStyleTag.Replace(html, string.Empty);
            }

            return html;
        }

        private bool NormalizeHref(Uri baseUri, string relativeUri, out Uri result)
        {
            StringBuilder sb = new StringBuilder(relativeUri.Trim());
            sb.Replace("\t", "");
            sb.Replace("\n", "");
            sb.Replace("\r", "");

            relativeUri = sb.ToString();

            // TODO: need improvement
            if (Uri.TryCreate(baseUri, relativeUri, out result))
            {
                // Only allow this schemes (for now)
                switch (result.Scheme)
                {
                    case "data":
                    case "http":
                    case "https":
                        return true;
                }
            }

            return false;
        }

        private IEnumerable<Uri> GetIconsUrl(Uri entryUrl, string html)
        {
            // Extract <base> tag
            Match match = baseTag.Match(html);
            if (match.Success)
            {
                string baseHtml = match.Value;

                // Extract href attribute value
                Match hrefHtml = hrefAttribute.Match(baseHtml);
                if (hrefHtml.Success)
                {
                    string href = hrefHtml.Groups["url"].Value;

                    // Make a valid URL
                    Uri baseUrl;
                    if (NormalizeHref(entryUrl, href, out baseUrl))
                    {
                        entryUrl = baseUrl;
                    }
                }
            }

            // TODO: refactor code

            // List of possible icons
            List<Uri> urls = new List<Uri>();

            Uri faviconUrl;

            // Loops through each <link> tag
            foreach (Match linkTag in linkTags.Matches(html))
            {
                // Checks if it has the rel icon attribute
                string linkHtml = linkTag.Value;
                if (relAttribute.IsMatch(linkHtml))
                {
                    // Extract href attribute value
                    Match hrefHtml = hrefAttribute.Match(linkHtml);
                    if (hrefHtml.Success)
                    {
                        string href = hrefHtml.Groups["url"].Value;

                        // Make a valid URL
                        if (NormalizeHref(entryUrl, href, out faviconUrl))
                        {
                            urls.Add(faviconUrl);
                        }
                    }
                }
            }

            // Fallback: default location
            if (Uri.TryCreate(entryUrl, "/favicon.ico", out faviconUrl))
            {
                urls.Add(faviconUrl);
            }

            // Since there is no collection that only accepts unique items
            urls = Util.RemoveDuplicates(urls);

            return urls;
        }

        private bool IsValidImage(byte[] data)
        {
            // Invalid data
            if (data == null)
            {
                return false;
            }

            try
            {
                Image image = GfxUtil.LoadImage(data);
                return true;
            }
            catch (Exception)
            {
                // Invalid image format
            }

            return false;
        }

        private bool ResizeImage(ref byte[] data)
        {
            // Invalid data
            if (data == null)
            {
                return false;
            }

            // KeePassLib.PwCustomIcon
            // Recommended maximum sizes, not obligatory
            //const int MaxWidth = 128;
            //const int MaxHeight = 128;

            int MaxWidth, MaxHeight;
            MaxWidth = MaxHeight = YetAnotherFaviconDownloaderExt.Config.GetMaximumIconSize();

            Image image;
            try
            {
                image = GfxUtil.LoadImage(data);
            }
            catch (Exception)
            {
                // Invalid image format
                return false;
            }

            // Checks if we need to resize
            if (image.Width <= MaxWidth && image.Height <= MaxHeight)
            {
                // don't need to resize the image
                // data = (original image)
                return true;
            }

            // Try to resize the image to png
            try
            {
                double ratioWidth = MaxWidth / (double)image.Width;
                double ratioHeight = MaxHeight / (double)image.Height;

                double ratioImage = Math.Min(ratioHeight, ratioWidth);
                int h = (int)Math.Round(image.Height * ratioImage);
                int w = (int)Math.Round(image.Width * ratioImage);

                image = GfxUtil.ScaleImage(image, w, h);

                using (var ms = new MemoryStream())
                {
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                    // If it's all ok
                    // data = (resized image)
                    data = ms.ToArray();
                    return true;
                }
            }
            catch (Exception)
            {
                // Can't resize image
                // data = (original image)
                return true;
            }
        }
    }
}
