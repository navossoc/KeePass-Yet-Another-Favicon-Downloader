namespace YetAnotherFaviconDownloader
{
    /// <summary>
    /// Represents a collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    public partial class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>
    {
        /// <summary>
        /// Adds the specified key and value to the dictionary if it does not exist.
        /// </summary>
        /// <param name="key">The key of the element to add.</param>
        /// <param name="value">The value of the element to add. The value can be null for reference types.</param>
        /// <returns>true if the <see cref="Dictionary{TKey, TValue}"/> inserts an element with the specified key; otherwise, false.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            if (!ContainsKey(key))
            {
                Add(key, value);
                return true;
            }

            return false;
        }
    }
}
