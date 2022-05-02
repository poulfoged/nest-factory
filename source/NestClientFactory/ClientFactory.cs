using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nest;
using NestClientFactory.Lifestyle;

namespace NestClientFactory
{
    public class ClientFactory : IClientFactory
    {
        private Func<IElasticClient> _clientConstructor = () => new ElasticClient();
        private ILifestyle _lifeStyle = new StaticLifestyle();
        private readonly IDictionary<string, Initializer> _initializers = new Dictionary<string, Initializer>(StringComparer.InvariantCultureIgnoreCase);
        private Action<string, object[]> _logger = (format, args) => Trace.WriteLine(string.Format(format, args));
        private bool _infoLoggingEnabled;
        private Uri _url = new Uri("http://localhost:9200");
        private ConnectionSettings _settings;
        private Func<ConnectionSettings, IElasticClient> _clientConstructorWithSettings;
        private IConnectionSettingsConfigurator[] _connectionConfigurators;
        private readonly IClientConfigurator[] _clientConfigurators;

        public ClientFactory(IEnumerable<IClientConfigurator> clientConfigurators, IEnumerable<IConnectionSettingsConfigurator> connectionConfigurators) { 
            _connectionConfigurators = connectionConfigurators.ToArray();
            _clientConfigurators = clientConfigurators.ToArray();
        }

        private void Info(string format, params object[] args)
        {
            if (_infoLoggingEnabled)
                _logger(format, args);
        }

        public async Task<IElasticClient> CreateClient()
        {
            this.Info("Running {0} init-steps, statuses are stored in {1}", (object)this._initializers.Count, (object)this._lifeStyle.GetType().Name);
            IElasticClient client;
            if (this._clientConstructorWithSettings != null)
            {
                IConnectionSettingsConfigurator[] settingsConfiguratorArray = this._connectionConfigurators;
                for (int index = 0; index < settingsConfiguratorArray.Length; ++index)
                {
                    IConnectionSettingsConfigurator connectionSettingsConfigurator = settingsConfiguratorArray[index];
                    connectionSettingsConfigurator.Configure(_settings);
                }
                settingsConfiguratorArray = null;
                client = _clientConstructorWithSettings(_settings);
            }
            else
                client = _clientConstructor();

            foreach (KeyValuePair<string, Initializer> initializer1 in _initializers)
            {
                KeyValuePair<string, ClientFactory.Initializer> initializer = initializer1;
                await this.RunInitializer(initializer, client);
                initializer = new KeyValuePair<string, ClientFactory.Initializer>();
            }
            return client;
        }

        public IClientFactory Discover(params string[] assemblyNames)
        {
            if (!this._lifeStyle.TryAdd<ManualResetEvent>("_discovery", new ManualResetEvent(false)))
            {
                this._lifeStyle.TryGet<ManualResetEvent>("_discovery").WaitOne();
                foreach (IClientConfigurator clientConfigurator in this._lifeStyle.TryGet<IClientConfigurator[]>("_configurators"))
                    clientConfigurator.Configure((IClientFactory)this);
                this._connectionConfigurators = this._lifeStyle.TryGet<IConnectionSettingsConfigurator[]>("_connectionConfigurators");
                return (IClientFactory)this;
            }
            IClientConfigurator[] array = ((IEnumerable<string>)assemblyNames).Select<string, Assembly>(new Func<string, Assembly>(Assembly.Load)).SelectMany<Assembly, Type>((Func<Assembly, IEnumerable<Type>>)(s => (IEnumerable<Type>)s.GetTypes())).Where<Type>((Func<Type, bool>)(myType => myType.IsClass && !myType.IsAbstract && !myType.IsInterface && typeof(IClientConfigurator).IsAssignableFrom(myType))).Select<Type, IClientConfigurator>((Func<Type, IClientConfigurator>)(type => (IClientConfigurator)Activator.CreateInstance(type))).ToArray<IClientConfigurator>();
            this._connectionConfigurators = ((IEnumerable<string>)assemblyNames).Select<string, Assembly>(new Func<string, Assembly>(Assembly.Load)).SelectMany<Assembly, Type>((Func<Assembly, IEnumerable<Type>>)(s => (IEnumerable<Type>)s.GetTypes())).Where<Type>((Func<Type, bool>)(myType => myType.IsClass && !myType.IsAbstract && !myType.IsInterface && typeof(IConnectionSettingsConfigurator).IsAssignableFrom(myType))).Select<Type, IConnectionSettingsConfigurator>((Func<Type, IConnectionSettingsConfigurator>)(type => (IConnectionSettingsConfigurator)Activator.CreateInstance(type))).ToArray<IConnectionSettingsConfigurator>();
            this._lifeStyle.TryAdd<IConnectionSettingsConfigurator[]>("_connectionConfigurators", this._connectionConfigurators);
            this._lifeStyle.TryAdd<IClientConfigurator[]>("_configurators", array);
            foreach (IClientConfigurator clientConfigurator in array)
                clientConfigurator.Configure((IClientFactory)this);
            this._lifeStyle.TryGet<ManualResetEvent>("_discovery").Set();
            return (IClientFactory)this;
        }

        public IClientFactory Inject()
        {
            if (!this._lifeStyle.TryAdd<ManualResetEvent>("_discovery", new ManualResetEvent(false)))
            {
                this._lifeStyle.TryGet<ManualResetEvent>("_discovery").WaitOne();
                foreach (IClientConfigurator clientConfigurator in this._lifeStyle.TryGet<IClientConfigurator[]>("_configurators"))
                    clientConfigurator.Configure((IClientFactory)this);
                this._connectionConfigurators = this._lifeStyle.TryGet<IConnectionSettingsConfigurator[]>("_connectionConfigurators");
                return (IClientFactory)this;
            }
            this._lifeStyle.TryAdd<IConnectionSettingsConfigurator[]>("_connectionConfigurators", this._connectionConfigurators);
            this._lifeStyle.TryAdd<IClientConfigurator[]>("_configurators", this._clientConfigurators);
            foreach (IClientConfigurator clientConfigurator in _clientConfigurators)
                clientConfigurator.Configure((IClientFactory)this);
            this._lifeStyle.TryGet<ManualResetEvent>("_discovery").Set();
            return (IClientFactory)this;
        }

        private async Task RunInitializer(KeyValuePair<string, Initializer> initializer, IElasticClient client)
        {
            var localKey = string.Format("{0}%{1}", _url, initializer.Key);


            Info("** Initializing {0} **", localKey);

            if (!_lifeStyle.TryAdd(localKey, new TaskCompletionSource<object>()))
            {
                Info("Internal status ok for {0} - waiting", localKey);

                var source = _lifeStyle.TryGet<TaskCompletionSource<object>>(localKey);
                await source.Task;
                return;
            }

            try
            {
                bool result;
                try
                {
                    result = await initializer.Value.ProbeFunc(client);
                }
                catch (Exception ex)
                {
                    throw new UnableToProbeException("Probe-function for " + localKey + " failed to execute.", ex);
                }

                Info("Checking external status for {0} was {1}", localKey, result);

                if (!result)
                {
                    Info("Initializing for {0}", localKey);

                    await initializer.Value.ActionFunc(client);
                }

                Info("External status for {0} was ok - storing internally", localKey);
            }
            finally
            {
                var source = _lifeStyle.TryGet<TaskCompletionSource<object>>(localKey);
                if (source != null)
                    source.SetResult(null);
            }

        }

        public IClientFactory Initialize(string name, Func<IInitializer, IInitializer> func)
        {
            Info("Adding {0}", name);

            _initializers.Add(name, func.Invoke(new Initializer(name)) as Initializer);
            return this;
        }

        public IClientFactory InitializationLifeStyle(ILifestyle lifestyle)
        {
            _lifeStyle = lifestyle;
            return this;
        }

        private class Initializer : IInitializer
        {
            private readonly string _name;

            public Initializer(string name)
            {
                _name = name;
            }

            public Func<IElasticClient, Task<bool>> ProbeFunc { get; private set; }

            public Func<IElasticClient, Task> ActionFunc { get; private set; }

            public IInitializer Probe(Func<IElasticClient, Task<bool>> probeFunc)
            {
                ProbeFunc = probeFunc;
                return this;
            }

            public IInitializer Probe(Func<IElasticClient, Task<IExistsResponse>> probeFunc)
            {
                ProbeFunc = async client => (await probeFunc(client)).Exists;
                return this;
            }

            public IInitializer Probe(Func<IElasticClient, Task<IGetMappingResponse>> probeFunc)
            {
                this.ProbeFunc = async client =>
                {
                    IGetMappingResponse getMappingResponse = await probeFunc(client);
                    return getMappingResponse.Indices != null;
                };

                return this;
            }

            public IInitializer Action(Func<IElasticClient, Task<bool>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result = await actionFunc(client);

                    if (!result)
                        throw new UnableToExecuteActionException($"Action-function for {_name} failed. Result was false");

                };

                return this;
            }

            public IInitializer Action(Func<IElasticClient, Task<ICreateIndexResponse>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result = await actionFunc(client);

                    if (!result.IsValid)
                        throw new UnableToExecuteActionException($"Action-function for {_name} failed.", result.OriginalException);

                };

                return this;
            }

            public IInitializer Action(Func<IElasticClient, Task<IIndicesResponse>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result =  await actionFunc(client);

                    if (!result.IsValid)
                        throw new UnableToExecuteActionException($"Action-function for {_name} failed. {result.ServerError?.Error}", result.OriginalException);
                                       
                };

                return this;
            }


            public IInitializer Action(Func<IElasticClient, Task<IBulkAliasResponse>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result = await actionFunc(client);

                    if (!result.IsValid)
                        throw new UnableToExecuteActionException($"Action-function for {_name} failed. {result.ServerError?.Error}", result.OriginalException);

                };

                return this;
            }
        }

        public IClientFactory LogTo(Action<string, object[]> logger)
        {
            _logger = logger;
            return this;
        }

        [Obsolete("Use ConstructUsing(func, url). Url is used for distinquising multiple servers in a single setup")]
        public IClientFactory ConstructUsing(Func<IElasticClient> func)
        {
            _clientConstructor = func;
            return this;
        }

        public IClientFactory ConstructUsing(Func<IElasticClient> func, Uri url)
        {
            this._url = url;
            _clientConstructor = func;
            return this;
        }

        public IClientFactory ConstructUsing(
   ConnectionSettings settings,
   Func<ConnectionSettings, IElasticClient> func,
   Uri url)
        {
            this._settings = settings;
            this._clientConstructorWithSettings = func;
            return (IClientFactory)this;
        }

        public IClientFactory EnableInfoLogging()
        {
            _infoLoggingEnabled = true;
            return this;
        }
    }
}