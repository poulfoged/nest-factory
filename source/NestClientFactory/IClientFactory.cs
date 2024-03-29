﻿using System;
using System.Threading.Tasks;
using Nest;
using NestClientFactory.Lifestyle;

namespace NestClientFactory
{
    /// <summary>
    /// Provides a factory for creating ElasticClient
    /// </summary>
    public interface IClientFactory
    {
        /// <summary>
        /// Creates a new client while executing the initializers
        /// </summary>
        /// <returns>Newly created client</returns>
        Task<IElasticClient> CreateClient();

        /// <summary>
        /// Adds a initializer to the client factory
        /// </summary>
        /// <param name="name">Name of the initializer, used for to store the status</param>
        /// <param name="func">Initialization functions</param>
        /// <returns>Factory for chaining</returns>
        IClientFactory Initialize(string name, Func<IInitializer, IInitializer> func);

        /// <summary>
        /// Changes the default lifestyle of the initializations
        /// </summary>
        /// <param name="lifestyle">New lifestyle</param>
        /// <returns>Factory for chaining</returns>
        IClientFactory InitializationLifeStyle(ILifestyle lifestyle);

        /// <summary>
        /// Changes the logger, process will be logged to this logger if enabled
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>Factory for chaining</returns>
        IClientFactory LogTo(Action<string, object[]> logger);

        /// <summary>
        /// 
        /// </summary>
        IClientFactory Discover(params string[] assemblyNames);

        /// <summary>
        /// 
        /// </summary>
        IClientFactory Inject();

        /// <summary>
        /// Allows custom functionality to be added when creating the actual elastic-client
        /// </summary>
        /// <param name="func">Function for constructing the ElasticClient</param>
        /// <returns>Factory for chaining</returns>
        [Obsolete("Use ConstructUsing(func, url). Url is used for distinquising multiple servers in a single setup")]
        IClientFactory ConstructUsing(Func<IElasticClient> func);


        /// <summary>
        /// Allows custom functionality to be added when creating the actual elastic-client, url should be provided for distinquising multiple servers
        /// </summary>
        /// <param name="func">Function for constructing the ElasticClient</param>
        /// <param name="url">Used url, used to provide lifestyle in case of multiple servers</param>
        /// <returns>Factory for chaining</returns>
        IClientFactory ConstructUsing(Func<IElasticClient> func, Uri url);

        IClientFactory ConstructUsing(ConnectionSettings settings, Func<ConnectionSettings, IElasticClient> func, Uri url);

        /// <summary>
        /// Enables informational logging
        /// </summary>
        /// <returns>Factory for chaining</returns>
        IClientFactory EnableInfoLogging();
    }
}