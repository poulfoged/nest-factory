namespace NestClientFactory.Lifestyle
{
    /// <summary>
    /// Maintains state of initializations against Elasticsearch
    /// </summary>
    public interface ILifestyle
    {
        /// <summary>
        /// Has this item been initialized
        /// </summary>
        /// <param name="key">Key of the initialization</param>
        /// <returns>Boolean indicating if item has been initialized or not</returns>
        T TryGet<T>(string key);

        /// <summary>
        /// Sets a new item as initialized
        /// </summary>
        /// <param name="key">Key of the item</param>
        /// <param name="arg"></param>
        bool TryAdd<T>(string key, T arg);
    }
}