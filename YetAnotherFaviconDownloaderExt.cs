using KeePass.Plugins;
using KeePassLib;
using System;
using System.Collections.Generic;
using System.Net;

namespace YetAnotherFaviconDownloader
{
    public class YetAnotherFaviconDownloaderExt : Plugin
    {
        private IPluginHost m_host = null;

        public override bool Initialize(IPluginHost host)
        {
            m_host = host;

            Util.Log("Plugin Initialize");

            var entriesMenu = m_host.MainWindow.EntryContextMenu.Items;
            entriesMenu.Add("Download favicon", null, MenuEntry_Click);

            return true;
        }

        private void MenuEntry_Click(object sender, EventArgs e)
        {
            Util.Log("Menu entry clicked");

            var entries = m_host.MainWindow.GetSelectedEntries();
            if (entries == null)
            {
                Util.Log("No entries selected");
                return;
            }

            // Custom icons that will be added to the database
            var icons = new List<PwCustomIcon>(entries.Length);

            foreach (var entry in entries)
            {
                // Fields
                var title = entry.Strings.ReadSafe("Title");
                var url = entry.Strings.ReadSafe("URL");

                Util.Log("Downloading favicon for:\n" +
                    "Title: {0}\n" +
                    "URL: {1}", title, url);

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
            m_host.Database.CustomIcons.AddRange(icons);

            // Refresh icons
            m_host.Database.UINeedsIconUpdate = true;
            m_host.MainWindow.UpdateUI(false, null, false, null, true, null, true);
        }

        public override void Terminate()
        {
            Util.Log("Plugin Terminate");
            return;
        }
    }
}
