using KeePass.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace YetAnotherFaviconDownloader
{
    public class YetAnotherFaviconDownloaderExt : Plugin
    {
        public override bool Initialize(IPluginHost host)
        {
            Log("Plugin Initialize");
            return true;
        }

        public override void Terminate()
        {
            Log("Plugin Terminate");
            return;
        }

        private void Log(string format, params object[] args)
        {
            Debug.Print("[YAFD] " + format, args);
        }
    }
}
