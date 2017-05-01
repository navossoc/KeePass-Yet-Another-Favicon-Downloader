using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace YetAnotherFaviconDownloader
{
    class FaviconDownloader
    {
        private readonly IPluginHost pluginHost;
        private readonly BackgroundWorker bgWorker;
        private readonly IStatusLogger logger;

        private class ProgressInfo
        {
            public int Success;
            public int NotFound;
            public int Error;
            public int Current;
            public int Remaining;

            public int Total { get; private set; }
            public float Percent => ((Total - Remaining) * 100f) / Total;

            public ProgressInfo(int total)
            {
                Total = Remaining = total;
            }
        }

        public FaviconDownloader(IPluginHost host)
        {
            // KeePass plugin host
            pluginHost = host;

            // Set up BackgroundWorker
            bgWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            // BackgroundWorker Events
            bgWorker.DoWork += BgWorker_DoWork;
            bgWorker.ProgressChanged += BgWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += BgWorker_RunWorkerCompleted;

            // Status Progress Form
            Form fStatusDialog;
            logger = StatusUtil.CreateStatusDialog(pluginHost.MainWindow, out fStatusDialog, "Yet Another Favicon Downloader", "Downloading favicons...", true, false);
        }

        public void Run(PwEntry[] entries)
        {
            bgWorker.RunWorkerAsync(entries);
        }

        private void BgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var entries = e.Argument as PwEntry[];

            // Progress information
            var progress = new ProgressInfo(entries.Length);

            // Custom icons that will be added to the database
            var icons = new PwCustomIcon[entries.Length];

            foreach (var entry in entries)
            {
                // Checks whether the user pressed the cancel button or the close button
                if (worker.CancellationPending || !logger.ContinueWork())
                {
                    e.Cancel = true;
                    break;
                }

                var i = Interlocked.Increment(ref progress.Current) - 1;

                // Fields
                var url = entry.Strings.ReadSafe("URL");

                Util.Log("Downloading: {0}", url);

                using (var wc = new WebClient())
                {
                    try
                    {
                        // Download
                        var data = wc.DownloadData(url + "favicon.ico");
                        Util.Log("Icon downloaded with success");

                        // Create icon
                        var uuid = new PwUuid(true);
                        var icon = new PwCustomIcon(uuid, data);

                        // Add icon
                        icons[i] = icon;

                        // Associate with this entry
                        entry.CustomIconUuid = uuid;

                        // Save it
                        entry.Touch(true, false);

                        // Icon downloaded with success
                        Interlocked.Increment(ref progress.Success);
                    }
                    catch (WebException ex)
                    {
                        Util.Log("Failed to download favicon");

                        var response = ex.Response as HttpWebResponse;
                        if (response?.StatusCode == HttpStatusCode.NotFound)
                        {
                            // Can't find an icon
                            Interlocked.Increment(ref progress.NotFound);
                        }
                        else
                        {
                            // Some other error (network, etc)
                            Interlocked.Increment(ref progress.Error);
                        }
                    }
                }

                // Progress
                Interlocked.Decrement(ref progress.Remaining);
                worker.ReportProgress((int)progress.Percent, progress);
            }

            // Add all icons to the database
            pluginHost.Database.CustomIcons.AddRange(icons);

            // Remove invalid entries
            pluginHost.Database.CustomIcons.RemoveAll(x => x == null);

            // Refresh icons
            pluginHost.Database.UINeedsIconUpdate = true;

            // Waits long enough until we can see the output
            Thread.Sleep(3000);
        }

        private void BgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Util.Log("Progress: {0}%", e.ProgressPercentage);
            logger.SetProgress((uint)e.ProgressPercentage);

            var state = e.UserState as ProgressInfo;
            var status = String.Format("Success: {0} / Not Found: {1} / Error: {2} / Remaining: {3}", state.Success, state.NotFound, state.Error, state.Remaining);
            logger.SetText(status, LogStatusType.Info);
        }

        private void BgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                Util.Log("Cancelled");
            }
            else if (e.Error != null)
            {
                Util.Log("Error: {0}", e.Error.Message);
            }
            else
            {
                Util.Log("Done");
            }

            // Refresh icons
            pluginHost.MainWindow.UpdateUI(false, null, false, null, true, null, true);

            logger.EndLogging();
        }
    }
}
