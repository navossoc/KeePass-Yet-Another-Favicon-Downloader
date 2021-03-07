using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using YetAnotherFaviconDownloader;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            //            var url = "https://www.asnbank.nl/home.html";
            var url = "https://mijn.ing.nl/internetbankieren/SesamLoginServlet"; // "https://www.asnbank.nl/onlinebankieren/secure/loginparticulier.html";
            using (FaviconDownloader fd = new FaviconDownloader())
            {
                // Download favicon
                byte[] data = fd.GetIcon(url);
                Assert.IsNotNull(data);
            }
        }
    }

}
