using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YetAnotherFaviconDownloader;

namespace Tests
{
    [TestClass]
    public class FaviconDownloaderTest
    {
        [TestMethod]
        public void TestCookieContainer()
        {
            // Eelke76
            // https://github.com/navossoc/KeePass-Yet-Another-Favicon-Downloader/pull/54

            var url = "https://mijn.ing.nl/internetbankieren/SesamLoginServlet";

            using (FaviconDownloader fd = new FaviconDownloader())
            {
                // Download favicon
                byte[] data = fd.GetIcon(url);
                Assert.IsNotNull(data);
            }
        }

        [TestMethod]
        public void TestTryDomainRootFolder()
        {
            // Eelke76
            // https://github.com/navossoc/KeePass-Yet-Another-Favicon-Downloader/pull/54

            var url = "https://www.asnbank.nl/onlinebankieren";

            using (FaviconDownloader fd = new FaviconDownloader())
            {
                // Download favicon
                byte[] data = fd.GetIcon(url);
                Assert.IsNotNull(data);
            }
        }
    }
}
