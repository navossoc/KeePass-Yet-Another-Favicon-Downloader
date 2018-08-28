using KeePass.Plugins;
using KeePass.UI;
using KeePassLib;
using KeePassLib.Interfaces;
using System;
using System.Collections.Generic;
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

            private int _total;
            public int Total { get { return _total; } private set { _total = value; } }
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
            BackgroundWorker worker = sender as BackgroundWorker;
            PwEntry[] entries = e.Argument as PwEntry[];

            // Progress information
            ProgressInfo progress = new ProgressInfo(entries.Length);

            // Custom icons that will be added to the database
            PwCustomIcon[] icons = new PwCustomIcon[entries.Length];

            // Set up proxy information for all WebClients
            FaviconDownloader.Proxy = Util.GetKeePassProxy();

            using (ManualResetEvent waiter = new ManualResetEvent(false))
            {
                for (int j = 0; j < entries.Length; j++)
                {
                    PwEntry entry = entries[j];
                    ThreadPool.QueueUserWorkItem(delegate (object notUsed)
                    {
                        // Checks whether the user pressed the cancel button or the close button
                        if (!logger.ContinueWork())
                        {
                            e.Cancel = true;
                        }
                        else
                        {
                            int i = Interlocked.Increment(ref progress.Current) - 1;

                            // Fields
                            string url = entry.Strings.ReadSafe(PwDefs.UrlField);

                            if (url == string.Empty)
                            {
                                // If the user wants to use the title field, let's give it a try
                                if (YetAnotherFaviconDownloaderExt.Config.GetUseTitleField())
                                {
                                    url = entry.Strings.ReadSafe(PwDefs.TitleField);
                                }
                            }

                            // Empty URL field
                            if (url == string.Empty)
                            {
                                // Can't find an icon
                                Interlocked.Increment(ref progress.NotFound);
                            }
                            else
                            {
                                Util.Log("Downloading: {0}", url);

                                using (FaviconDownloader fd = new FaviconDownloader())
                                {
                                    try
                                    {
                                        // Download favicon
                                        byte[] data = fd.GetIcon(url);
                                        Util.Log("Icon downloaded with success");

                                        // Hash icon data (avoid duplicates)
                                        byte[] hash = Util.HashData(data);

                                        // Creates an icon only if your UUID does not exist
                                        PwUuid uuid = new PwUuid(hash);
                                        if (!pluginHost.Database.CustomIcons.Exists(delegate (PwCustomIcon x) { return x.Uuid.Equals(uuid); }))
                                        {
                                            // Add icon
                                            icons[i] = new PwCustomIcon(uuid, data);
                                        }

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
            status = String.Format("YAFD: Success: {0} / Not Found: {1} / Error: {2}.", progress.Success, progress.NotFound, progress.Error);

            // Prevents inserting duplicate icons
            MergeCustomIcons(icons);

            // Refresh icons on database
            pluginHost.Database.UINeedsIconUpdate = true;

            // Waits long enough until we can see the output
            Thread.Sleep(3000);
        }

        private void ReportProgress(ProgressInfo progress)
        {
            logger.SetProgress((uint)progress.Percent);
            logger.SetText(String.Format("YAFD: Success: {0} / Not Found: {1} / Error: {2} / Remaining: {3}", progress.Success, progress.NotFound, progress.Error, progress.Remaining), LogStatusType.Info);
        }

        private void MergeCustomIcons(PwCustomIcon[] icons)
        {
            List<PwCustomIcon> customIcons = pluginHost.Database.CustomIcons;

            // Removes duplicate downloaded icons
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] == null)
                {
                    continue;
                }

                for (int j = i + 1; j < icons.Length; j++)
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
            customIcons.RemoveAll(delegate (PwCustomIcon x) { return x == null; });
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
