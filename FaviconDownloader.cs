using KeePassLib.Utility;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace YetAnotherFaviconDownloader
{
    [System.ComponentModel.DesignerCategory("")]
    public sealed class FaviconDownloader : WebClient
    {
        public FaviconDownloader()
        {
            // TODO: assign proxy only one time per "batch"
        }

        public byte[] GetIcon(string url)
        {
            // Check if the URL could be a site address
            if (!IsValidURL(url))
            {
                throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
            }

            try
            {
                // Download
                var page = DownloadPage(url);
                var head = StripPage(page);
                var links = GetIconsUrl(new Uri(url), head);

                // Try to find a valid image
                foreach (var link in links)
                {
                    // Download file
                    var data = DownloadAsset(link);

                    // Invalid data
                    if (data == null)
                    {
                        continue;
                    }

                    // Check if the data is a valid image
                    if (IsValidImage(data))
                    {
                        return data;
                    }
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
                }
                else
                {
                    throw new FaviconDownloaderException();
                }
            }

            // If there is no file available
            throw new FaviconDownloaderException(FaviconDownloaderExceptionStatus.NotFound);
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address) as HttpWebRequest;

            // Set up proxy information
            request.Proxy = Util.GetKeePassProxy();

            // Set up timeout for 20 seconds
            request.Timeout = 20000;

            // Follow redirection responses with an HTTP status code from 300 to 399
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = 4;

            // Sets the cookies associated with the request (security issue?)
            request.CookieContainer = new CookieContainer();

            return request;
        }

        public byte[] DownloadAsset(Uri address)
        {
            // Data URI scheme
            if (address.Scheme == "data")
            {
                var uri = address.ToString();

                // data:[<mediatype>][;base64],<data>
                var match = Regex.Match(uri, @"data:(?<mediatype>.*?)(;(?<base64>.+?))?,(?<data>.+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
                if (match.Success)
                {
                    var data = match.Groups["data"].Value;

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
            }

            // Download file
            return DownloadData(address);
        }

        private bool IsValidURL(string url)
        {
            if (Regex.IsMatch(url, @"^http(s)?://.+", RegexOptions.IgnoreCase | RegexOptions.Compiled))
            {
                Uri result;
                return Uri.TryCreate(url, UriKind.Absolute, out result);
            }

            // TODO: should allow URIs without a schema?

            return false;
        }

        private string DownloadPage(string address)
        {
            // TODO: handle encoding issues
            var html = DownloadString(address);

            return html;
        }

        private string StripPage(string html)
        {
            // This should be enough for most of cases
            const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline;

            // Extract <head> tag
            var match = Regex.Match(html, @"<head\b.*?>.*?</head>", regexOptions);
            if (match.Success)
            {
                // <head> content
                html = match.Value;

                // Remove HTML comments
                html = Regex.Replace(html, @"<!--.*?-->", string.Empty, regexOptions);

                // Remove some unnecessary tags from the page
                html = Regex.Replace(html, @"<(script|style)\b.*?>.*?</\1>", string.Empty, regexOptions);
            }

            return html;
        }

        private bool NormalizeHref(Uri baseUri, string relativeUri, out Uri result)
        {
            var sb = new StringBuilder(relativeUri.Trim());
            sb.Replace("\t", "");
            sb.Replace("\n", "");
            sb.Replace("\r", "");

            relativeUri = sb.ToString();

            // TODO: need improvement
            return Uri.TryCreate(baseUri, relativeUri, out result);
        }

        private IEnumerable<Uri> GetIconsUrl(Uri entryUrl, string html)
        {
            // Since we don't have any collection that accepts unique items on .NET 2.0
            var urls = new Dictionary<Uri, bool>();

            const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.Singleline;

            // Regex for <link> tags
            var linkTags = new Regex(@"<link\b.*?>", regexOptions);

            // Regex for <link> tags with rel attribute
            var relAttribute = new Regex(@"rel\s*=\s*(icon\b|(?<q>'|"")\s*(shortcut\s*\b)?icon\b\s*\k<q>)", regexOptions);

            // Regex for <link> tags with href attribute
            var hrefAttribute = new Regex(@"href\s*=\s*((?<q>'|"")(?<url>.*?)(\k<q>|>)|(?<url>.*?)(\s+|>))", regexOptions);

            Uri faviconUrl;

            // Loops through each <link> tag
            foreach (var linkTag in linkTags.Matches(html))
            {
                // Checks if it has the rel icon attribute
                var linkHtml = linkTag.ToString();
                if (relAttribute.IsMatch(linkHtml))
                {
                    // Extract href attribute value
                    var hrefHtml = hrefAttribute.Match(linkHtml);
                    if (hrefHtml.Length > 1)
                    {
                        var href = hrefHtml.Groups["url"].Value;

                        // Make a valid URL
                        if (NormalizeHref(entryUrl, href, out faviconUrl))
                        {
                            urls.TryAdd(faviconUrl, false);
                        }
                    }
                }
            }

            // Fallback: default location
            if (Uri.TryCreate(entryUrl, "/favicon.ico", out faviconUrl))
            {
                urls.TryAdd(faviconUrl, false);
            }

            return urls.Keys;
        }

        private bool IsValidImage(byte[] data)
        {
            try
            {
                var image = GfxUtil.LoadImage(data);
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
