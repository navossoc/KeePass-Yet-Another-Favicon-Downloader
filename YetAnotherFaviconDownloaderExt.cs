using KeePass.Plugins;
using System;

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

            // Run all the work in a new thread
            var downloader = new FaviconDownloader(m_host);
            downloader.Run(entries);
        }

        public override void Terminate()
        {
            Util.Log("Plugin Terminate");
            return;
        }
    }
}
