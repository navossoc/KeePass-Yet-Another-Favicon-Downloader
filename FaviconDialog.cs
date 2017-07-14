using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Interfaces;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace YetAnotherFaviconDownloader
{
    public sealed class FaviconDialog
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
            public float Percent
            {
                get { return ((Total - Remaining) * 100f) / Total; }
            }

            public ProgressInfo(int total)
            {
                Total = Remaining = total;
            }
        }

        public FaviconDialog(IPluginHost host)
        {
            // KeePass plugin host
            pluginHost = host;

            // Set up BackgroundWorker
            bgWorker = new BackgroundWorker();

            // BackgroundWorker Events
            bgWorker.DoWork += BgWorker_DoWork;
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

            // Set up proxy information for all WebClients
            FaviconDownloader.Proxy = Util.GetKeePassProxy();

            using (var waiter = new ManualResetEvent(false))
            {
                foreach (var entry in entries)
                {
                    ThreadPool.QueueUserWorkItem(notUsed =>
                    {
                        // Checks whether the user pressed the cancel button or the close button
                        if (!logger.ContinueWork())
                        {
                            e.Cancel = true;
                        }
                        else
                        {
                            var i = Interlocked.Increment(ref progress.Current) - 1;

                            // Fields
                            var url = entry.Strings.ReadSafe("URL");

                            Util.Log("Downloading: {0}", url);

                            using (var fd = new FaviconDownloader())
                            {
                                try
                                {
                                    // Download favicon
                                    var data = fd.GetIcon(url);
                                    Util.Log("Icon downloaded with success");

                                    // Hash icon data (avoid duplicates)
                                    var hash = Util.HashData(data);

                                    // Create icon
                                    var uuid = new PwUuid(hash);
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
                                catch (FaviconDownloaderException ex)
                                {
                                    Util.Log("Failed to download favicon");

                                    if (ex.Status == FaviconDownloaderExceptionStatus.NotFound)
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
                        }

                        // Notifies that all downloads are finished
                        if (Interlocked.Decrement(ref progress.Remaining) == 0)
                        {
                            waiter.Set();
                        }
                    });

                }

                // Wait until the downloads are finished
                do
                {
                    ReportProgress(progress);
                } while (!waiter.WaitOne(100));
            }

            // Progress 100%
            ReportProgress(progress);

            // Add all icons to the database
            pluginHost.Database.CustomIcons.AddRange(icons);

            // Remove invalid entries
            pluginHost.Database.CustomIcons.RemoveAll(x => x == null);

            // Refresh icons
            pluginHost.Database.UINeedsIconUpdate = true;

            // Waits long enough until we can see the output
            Thread.Sleep(3000);
        }

        private void ReportProgress(ProgressInfo progress)
        {
            logger.SetProgress((uint)progress.Percent);
            logger.SetText(String.Format("Success: {0} / Not Found: {1} / Error: {2} / Remaining: {3}", progress.Success, progress.NotFound, progress.Error, progress.Remaining), LogStatusType.Info);
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
