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
        /// Automatic prefix URLs with http(s):// setting (https first, then http)
        /// </summary>
        private const string automaticPrefixURLs = pluginName + "PrefixURLs";
        private bool? m_automaticPrefixURLs = null;

        /// <summary>
        /// Use title field if URL field is empty setting
        /// </summary>
        private const string useTitleField = pluginName + "TitleField";
        private bool? m_useTitleField = null;

        /// <summary>
        /// Update last modified date when adding/updating icons
        /// </summary>
        private const string updateLastModified = pluginName + "UpdateLastModified";
        private bool? m_updateLastModified = null;

        /// <summary>
        /// Use Google S2 API to fetch 16x16 icons
        /// </summary>
        private const string useGoogleAPI = pluginName + "UseGoogleAPI";
        private bool? m_useGoogleAPI = null;

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

        public bool GetUpdateLastModified()
        {
            if (!m_updateLastModified.HasValue)
            {
                m_updateLastModified = config.GetBool(updateLastModified, true);
            }

            return m_updateLastModified.Value;
        }

        public void SetUpdateLastModified(bool value)
        {
            m_updateLastModified = value;
            config.SetBool(updateLastModified, value);
        }

        public bool GetUseGoogleAPI()
        {
            if (!m_useGoogleAPI.HasValue)
            {
                m_useGoogleAPI = config.GetBool(useGoogleAPI, true);
            }

            return m_useGoogleAPI.Value;
        }

        public void SetUseGoogleAPI(bool value)
        {
            m_useGoogleAPI = value;
            config.SetBool(useGoogleAPI, value);
        }
    }
}
