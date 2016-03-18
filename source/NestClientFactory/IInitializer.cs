using System;
using System.Threading.Tasks;
using Nest;

namespace NestClientFactory
{
    /// <summary>
    /// Interface for chaining initializations to ElasticClientFactory
    /// </summary>
    public interface IInitializer
    {
        /// <summary>
        /// A method for testing if condition against ElasticSearch is meet, should return a bool indicating status
        /// </summary>
        /// <param name="probeFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Probe(Func<IElasticClient, Task<bool>> probeFunc);

        /// <summary>
        /// A method for testing if condition against ElasticSearch is meet, should return a IExistsResponse indicating status
        /// </summary>
        /// <param name="probeFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Probe(Func<IElasticClient, Task<IExistsResponse>> probeFunc);

        /// <summary>
        /// A method for testing if condition against ElasticSearch is meet, should return a IGetMappingResponse indicating status
        /// </summary>
        /// <param name="probeFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Probe(Func<IElasticClient, Task<IGetMappingResponse>> probeFunc);

        /// <summary>
        /// A method for executing an action against ElasticSearch, should return a bool indicating status
        /// </summary>
        /// <param name="actionFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Action(Func<IElasticClient, Task<bool>> actionFunc);

        /// <summary>
        /// A method for executing an action against ElasticSearch, should return a IIndicesOperationResponse indicating status
        /// </summary>
        /// <param name="actionFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Action(Func<IElasticClient, Task<ICreateIndexResponse>> actionFunc);

        /// <summary>
        /// A method for executing an action against ElasticSearch, should return a IIndicesResponse indicating status
        /// </summary>
        /// <param name="actionFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Action(Func<IElasticClient, Task<IIndicesResponse>> actionFunc);

        /// <summary>
        /// A method for executing an action against ElasticSearch, should return a IBulkAliasResponse indicating status
        /// </summary>
        /// <param name="actionFunc">Function to execute</param>
        /// <returns>Initialize for chaining</returns>
        IInitializer Action(Func<IElasticClient, Task<IBulkAliasResponse>> actionFunc);
    }
}