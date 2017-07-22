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

        private string status;

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

            // Block UI
            pluginHost.MainWindow.UIBlockInteraction(true);
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
                            var url = entry.Strings.ReadSafe(PwDefs.UrlField);

                            // Empty URL field
                            if (url == string.Empty)
                            {
                                // Can't find an icon
                                Interlocked.Increment(ref progress.NotFound);
                            }
                            else
                            {
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

                                        // Creates an icon only if your UUID does not exist
                                        var uuid = new PwUuid(hash);
                                        var icon = pluginHost.Database.CustomIcons.Find(x => x.Uuid.Equals(uuid)) ?? new PwCustomIcon(uuid, data);

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
                        }

                        // Notifies that all downloads are finished
                        if (Interlocked.Decrement(ref progress.Remaining) == 0)
                        {
                            waiter.Set();
                        }
                    });

                }

                // Wait until the downloads are finished
                int lastValue = 0;
                do
                {
                    // Update progress only when needed
                    if (lastValue != progress.Remaining)
                    {
                        ReportProgress(progress);
                        lastValue = progress.Remaining;
                    }
                } while (!waiter.WaitOne(100));
            }

            // Progress 100%
            ReportProgress(progress);
            status = String.Format("Success: {0} / Not Found: {1} / Error: {2}.", progress.Success, progress.NotFound, progress.Error);

            // Prevents inserting duplicate icons
            MergeCustomIcons(icons);

            // Refresh icons on database
            pluginHost.Database.UINeedsIconUpdate = true;

#if DEBUG
            // Waits long enough until we can see the output
            Thread.Sleep(3000);
#endif
        }

        private void ReportProgress(ProgressInfo progress)
        {
            logger.SetProgress((uint)progress.Percent);
            logger.SetText(String.Format("Success: {0} / Not Found: {1} / Error: {2} / Remaining: {3}", progress.Success, progress.NotFound, progress.Error, progress.Remaining), LogStatusType.Info);
        }

        private void MergeCustomIcons(PwCustomIcon[] icons)
        {
            var customIcons = pluginHost.Database.CustomIcons;

            // Removes duplicate downloaded icons
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null)
                {
                    continue;
                }

                for (var j = i + 1; j < icons.Length; j++)
                {
                    if (icons[j] == null)
                    {
                        continue;
                    }

                    if (icons[i].Uuid.Equals(icons[j].Uuid))
                    {
                        icons[j] = null;
                    }
                }
            }

            // Add all icons to the database
            customIcons.AddRange(icons);

            // Remove invalid entries
            customIcons.RemoveAll(x => x == null);
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

            // Unblock UI
            pluginHost.MainWindow.UIBlockInteraction(false);

            // Refresh icons
            pluginHost.MainWindow.RefreshEntriesList();
            pluginHost.MainWindow.UpdateUI(false, null, false, null, false, null, true);

            logger.EndLogging();

            // Report how many icons have been downloaded
            pluginHost.MainWindow.SetStatusEx(status);
        }
    }
}
