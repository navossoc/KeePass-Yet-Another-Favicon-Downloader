using KeePass.Plugins;
using KeePassLib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace YetAnotherFaviconDownloader
{
    public sealed class YetAnotherFaviconDownloaderExt : Plugin
    {
        public override string UpdateUrl
        {
            get { return "https://raw.githubusercontent.com/navossoc/KeePass-Yet-Another-Favicon-Downloader/master/VERSION"; }
        }

        // Plugin host interface
        private IPluginHost pluginHost;

        // Icon used by YAFD menus
        private Image menuImage;

        // Entry Context Menu
        private ToolStripSeparator entrySeparator;
        private ToolStripMenuItem entryDownloadFaviconsItem;

        // Group Context Menu
        private ToolStripSeparator groupSeparator;
        private ToolStripMenuItem groupDownloadFaviconsItem;

#if DEBUG
        // Tools Menu
        private ToolStripSeparator toolsMenuSeparator;
        private ToolStripMenuItem[] toolsMenuDropDownItems;
        private ToolStripMenuItem toolsMenuYAFD;
#endif

        public override bool Initialize(IPluginHost host)
        {
            Util.Log("Plugin Initialize");

            Debug.Assert(host != null);
            if (host == null)
            {
                return false;
            }
            pluginHost = host;

            // Load menus icon resource
            menuImage = Properties.Resources.Download_32;

            // Add Entry Context menu items
            entrySeparator = new ToolStripSeparator();
            entryDownloadFaviconsItem = new ToolStripMenuItem("Download Favicons", menuImage, DownloadFaviconsEntry_Click);
            pluginHost.MainWindow.EntryContextMenu.Items.Add(entrySeparator);
            pluginHost.MainWindow.EntryContextMenu.Items.Add(entryDownloadFaviconsItem);

            // Add Group Context menu items
            groupSeparator = new ToolStripSeparator();
            groupDownloadFaviconsItem = new ToolStripMenuItem("Download Favicons", menuImage, DownloadFaviconsGroup_Click);
            pluginHost.MainWindow.GroupContextMenu.Items.Add(groupSeparator);
            pluginHost.MainWindow.GroupContextMenu.Items.Add(groupDownloadFaviconsItem);

#if DEBUG
            // Add Tools menu items
            toolsMenuSeparator = new ToolStripSeparator();
            toolsMenuDropDownItems = new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("Reset Icons", null, ResetIconsMenu_Click)
            };
            toolsMenuYAFD = new ToolStripMenuItem("Yet Another Favicon Downloader", menuImage, toolsMenuDropDownItems);

            pluginHost.MainWindow.ToolsMenu.DropDownItems.Add(toolsMenuSeparator);
            pluginHost.MainWindow.ToolsMenu.DropDownItems.Add(toolsMenuYAFD);
#endif

            return true;
        }

        public override void Terminate()
        {
            Util.Log("Plugin Terminate");

            // This should never happen but better safe than sorry
            Debug.Assert(pluginHost != null);
            if (pluginHost == null)
            {
                return;
            }

            // Dispose resources
            if (menuImage != null)
            {
                menuImage.Dispose();
            }

            // Remove Entry Context menu items
            pluginHost.MainWindow.EntryContextMenu.Items.Remove(entrySeparator);
            pluginHost.MainWindow.EntryContextMenu.Items.Remove(entryDownloadFaviconsItem);

            // Remove Group Context menu items
            pluginHost.MainWindow.GroupContextMenu.Items.Remove(groupSeparator);
            pluginHost.MainWindow.GroupContextMenu.Items.Remove(groupDownloadFaviconsItem);

#if DEBUG
            // Remove Tools menu items
            pluginHost.MainWindow.ToolsMenu.DropDownItems.Remove(toolsMenuSeparator);
            pluginHost.MainWindow.ToolsMenu.DropDownItems.Remove(toolsMenuYAFD);
#endif
        }

        #region MenuItem EventHandler
        private void DownloadFaviconsEntry_Click(object sender, EventArgs e)
        {
            Util.Log("Entry Context Menu -> Download Favicons clicked");

            var entries = pluginHost.MainWindow.GetSelectedEntries();
            DownloadFavicons(entries);
        }

        private void DownloadFaviconsGroup_Click(object sender, EventArgs e)
        {
            Util.Log("Group Context Menu -> Download Favicons clicked");

            var group = pluginHost.MainWindow.GetSelectedGroup();
            if (group == null)
            {
                Util.Log("No group selected");
                return;
            }

            // Get all entries from the group
            bool subEntries = KeePass.Program.Config.MainWindow.ShowEntriesOfSubGroups;
            var entriesInGroup = group.GetEntries(subEntries);
            if (entriesInGroup == null || entriesInGroup.UCount == 0)
            {
                Util.Log("No entries in group");
                return;
            }

            // Copy PwObjectList<PwEntry> to PwEntry[]
            var entries = entriesInGroup.CloneShallowToList().ToArray();
            DownloadFavicons(entries);
        }

#if DEBUG
        private void ResetIconsMenu_Click(object sender, EventArgs e)
        {
            Util.Log("Tools Menu -> Reset Icons clicked");

            // Checks if there is an open database
            if (!pluginHost.Database.IsOpen)
            {
                Util.Log("Database not open");
                return;
            }

            // Reset icons from all groups
            var groups = pluginHost.Database.RootGroup.GetGroups(true);
            foreach (var group in groups)
            {
                //  Recycle bin
                if (group.Uuid.Equals(pluginHost.Database.RecycleBinUuid))
                {
                    group.IconId = PwIcon.TrashBin;
                }
                else
                {
                    group.IconId = PwIcon.Folder;
                }
                group.CustomIconUuid = PwUuid.Zero;
                group.Touch(true, false);
            }

            // Reset icons from all entries
            var entries = pluginHost.Database.RootGroup.GetEntries(true);
            foreach (var entry in entries)
            {
                entry.IconId = PwIcon.Key;
                entry.CustomIconUuid = PwUuid.Zero;
                entry.Touch(true, false);
            }

            // Remove all custom icons from database
            pluginHost.Database.CustomIcons.Clear();

            // Refresh icons
            pluginHost.MainWindow.UpdateUI(false, null, true, null, true, null, true);
        }
#endif
        #endregion

        private void DownloadFavicons(PwEntry[] entries)
        {
            if (entries == null || entries.Length == 0)
            {
                Util.Log("No entries selected");
                return;
            }

            // Run all the work in a new thread
            var downloader = new FaviconDialog(pluginHost);
            downloader.Run(entries);
        }
    }
}
