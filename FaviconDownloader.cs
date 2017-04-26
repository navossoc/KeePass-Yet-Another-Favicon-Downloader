using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Windows.Forms;

namespace YetAnotherFaviconDownloader
{
    class FaviconDownloader
    {
        private readonly IPluginHost pluginHost;
        private readonly BackgroundWorker bgWorker;
        private readonly IStatusLogger logger;

        public FaviconDownloader(IPluginHost host)
        {
            // KeePass plugin host
            pluginHost = host;

            // Set up BackgroundWorker
            bgWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false
            };

            // BackgroundWorker Events
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;

            // Status Progress Form
            Form fStatusDialog;
            logger = StatusUtil.CreateStatusDialog(pluginHost.MainWindow, out fStatusDialog, "Yet Another Favicon Downloader", "Downloading favicons...", false, true);
        }

        public void Run(PwEntry[] entries)
        {
            bgWorker.RunWorkerAsync(entries);
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var entries = e.Argument as PwEntry[];

            // Custom icons that will be added to the database
            var icons = new List<PwCustomIcon>(entries.Length);

            foreach (var entry in entries)
            {
                // Fields
                var title = entry.Strings.ReadSafe("Title");
                var url = entry.Strings.ReadSafe("URL");

                Util.Log("Downloading: {0}", url);

                WebClient wc = new WebClient();
                try
                {
                    // Download
                    var data = wc.DownloadData(url + "favicon.ico");
                    Util.Log("Icon downloaded with success");

                    // Create icon
                    var uuid = new PwUuid(true);
                    var icon = new PwCustomIcon(uuid, data);
                    icons.Add(icon);

                    // Associate with this entry
                    entry.CustomIconUuid = uuid;

                    // Save it
                    entry.Touch(true);
                }
                catch (WebException)
                {
                    Util.Log("Failed to download favicon");
                }
            }

            // Add all icons to the database
            pluginHost.Database.CustomIcons.AddRange(icons);

            // Refresh icons
            pluginHost.Database.UINeedsIconUpdate = true;
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Util.Log("Done");

            // Refresh icons
            pluginHost.MainWindow.UpdateUI(false, null, false, null, true, null, true);

            logger.EndLogging();
        }
    }
}
