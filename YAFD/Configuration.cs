using KeePass.App.Configuration;

namespace YetAnotherFaviconDownloader
{
    public sealed class Configuration
    {
        private AceCustomConfig config;

        /// <summary>
        /// Plugin name used on settings to avoid collisions
        /// </summary>
        private const string pluginName = "YetAnotherFaviconDownloader.";

        /// <summary>
        /// Automatic prefix URLs with http:// setting
        /// </summary>
        private const string automaticPrefixURLs = pluginName + "PrefixURLs";
        private bool? m_automaticPrefixURLs = null;
		
        /// <summary>
        /// Automatic prefix URLs with https:// setting
        /// </summary>
        private const string automaticHttpsPrefixURLs = pluginName + "HttpsPrefixURLs";
        private bool? m_automaticHttpsPrefixURLs = null;		
		
        /// <summary>
        /// Use title field if URL field is empty setting
        /// </summary>
        private const string useTitleField = pluginName + "TitleField";
        private bool? m_useTitleField = null;

        public Configuration(AceCustomConfig aceCustomConfig)
        {
            config = aceCustomConfig;
        }

        public bool GetAutomaticPrefixURLs()
        {
            if (!m_automaticPrefixURLs.HasValue)
            {
                m_automaticPrefixURLs = config.GetBool(automaticPrefixURLs, false);
            }

            return m_automaticPrefixURLs.Value;
        }

        public void SetAutomaticPrefixURLs(bool value)
        {
            m_automaticPrefixURLs = value;
            config.SetBool(automaticPrefixURLs, value);
        }
		
		public bool GetAutomaticHttpsPrefixURLs()
        {
            if (!m_automaticHttpsPrefixURLs.HasValue)
            {
                m_automaticHttpsPrefixURLs = config.GetBool(automaticHttpsPrefixURLs, false);
            }

            return m_automaticHttpsPrefixURLs.Value;
        }

        public void SetAutomaticHttpsPrefixURLs(bool value)
        {
            m_automaticHttpsPrefixURLs = value;
            config.SetBool(automaticHttpsPrefixURLs, value);
        }

        public bool GetUseTitleField()
        {
            if (!m_useTitleField.HasValue)
            {
                m_useTitleField = config.GetBool(useTitleField, false);
            }

            return m_useTitleField.Value;
        }

        public void SetUseTitleField(bool value)
        {
            m_useTitleField = value;
            config.SetBool(useTitleField, value);
        }
    }
}
