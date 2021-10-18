using System.Collections.Generic;

namespace YetAnotherFaviconDownloader
{
    public class Provider
    {
        public string Name { get; set; }
        public string URL { get; set; }

        public Provider(string name, string url)
        {
            Name = name;
            URL = url;
        }

        public override bool Equals(object obj)
        {
            Provider provider = obj as Provider;
            return provider != null &&
                   URL == provider.URL;
        }

        public override int GetHashCode()
        {
            return -1251312914 + EqualityComparer<string>.Default.GetHashCode(URL);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
