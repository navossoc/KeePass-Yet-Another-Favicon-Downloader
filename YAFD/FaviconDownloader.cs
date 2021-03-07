using KeePassLib.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private static readonly string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.122 Safari/537.36";

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

            // Enable TLS 1.1 and TLS 1.2.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }

        public FaviconDownloader()
        {
            cookieContainer = new CookieContainer();
        }

        public byte[] GetIcon(string url)
        {
            // We prefer https first (just to preserve the original link)
            string origURL = url;

            // Check if the URL could be a site address
            if (!IsValidURL(ref url, "https://"))
            {
                throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
            }

        retry_http:
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

                        // Check if the data is a valid image
                        if (IsValidImage(data))
                        {
                            return data;
                        }
                    }
                    catch (WebException)
                    {
                        // ignore the exception and try the next resource
                    }
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
    }
}
