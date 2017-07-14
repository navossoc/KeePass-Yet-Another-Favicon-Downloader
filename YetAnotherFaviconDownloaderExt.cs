using KeePass.Plugins;
using KeePassLib;
using System;
using System.Collections.Generic;

namespace YetAnotherFaviconDownloader
{
    public sealed class YetAnotherFaviconDownloaderExt : Plugin
    {
        public override string UpdateUrl
        {
            get { return "https://raw.githubusercontent.com/navossoc/KeePass-Yet-Another-Favicon-Downloader/master/VERSION"; }
        }

        private IPluginHost m_host = null;

        public override bool Initialize(IPluginHost host)
        {
            m_host = host;

            Util.Log("Plugin Initialize");

            var entriesMenu = m_host.MainWindow.EntryContextMenu.Items;
            entriesMenu.Add("Download favicon", null, MenuEntry_Click);
#if DEBUG
            // A little helper to make my tests easier
            entriesMenu.Add("Clear all favicon", null, MenuEntry2_Click);
#endif

            return true;
        }

        private void MenuEntry_Click(object sender, EventArgs e)
        {
            Util.Log("Menu entry clicked");

            var entries = m_host.MainWindow.GetSelectedEntries();
            if (entries == null)
            {
                Util.Log("No entries selected");

#if DEBUG
                // Download the entire group if there are no entries selected
                var group = m_host.MainWindow.GetSelectedGroup();
                if (group == null)
                {
                    return;
                }

                // Convert PwObjectList<PwEntry> to PwEntry[]
                entries = new List<PwEntry>(group.GetEntries(true)).ToArray();
#else
                return;
#endif
            }

            // Run all the work in a new thread
            var downloader = new FaviconDialog(m_host);
            downloader.Run(entries);
        }

#if DEBUG
        private void MenuEntry2_Click(object sender, EventArgs e)
        {
            Util.Log("Menu entry 2 clicked");

            // Reset icons from all groups
            var groups = m_host.Database.RootGroup.GetGroups(true);
            foreach (var group in groups)
            {
                group.IconId = PwIcon.Folder;
                group.CustomIconUuid = PwUuid.Zero;
                group.Touch(true, false);
            }

            // Reset icons from all entries
            var entries = m_host.Database.RootGroup.GetEntries(true);
            foreach (var entry in entries)
            {
                entry.IconId = PwIcon.Key;
                entry.CustomIconUuid = PwUuid.Zero;
                entry.Touch(true, false);
            }

            // Remove all custom icons from database
            m_host.Database.CustomIcons.Clear();

            // Refresh icons
            m_host.MainWindow.UpdateUI(false, null, true, null, true, null, true);
        }
#endif

        public override void Terminate()
        {
            Util.Log("Plugin Terminate");
            return;
        }
    }
}
