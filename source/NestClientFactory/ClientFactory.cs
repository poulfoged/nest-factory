using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        private void Info(string format, params object[] args)
        {
            if (_infoLoggingEnabled)
                _logger(format, args);
        }

        public IDisposable Temporary()
        {
            return null;
        }

        public async Task<IElasticClient> CreateClient()
        {
            Info("Running {0} init-steps, statuses are stored in {1}", _initializers.Count, _lifeStyle.GetType().Name);

            var client = _clientConstructor.Invoke();

            foreach (var initializer in _initializers)
                await RunInitializer(initializer, client);
            
            return client;
        }

        private async Task RunInitializer(KeyValuePair<string, Initializer> initializer, IElasticClient client)
        {
            Info("** Initializing {0} **", initializer.Key);

            if (!_lifeStyle.TryAdd(initializer.Key, new TaskCompletionSource<object>()))
            {
                Info("Internal status ok for {0} - waiting", initializer.Key);

                var source = _lifeStyle.TryGet<TaskCompletionSource<object>>(initializer.Key);
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
                    throw new UnableToProbeException("Probe-function for " + initializer.Key + " failed to execute.", ex);
                }

                Info("Checking external status for {0} was {1}", initializer.Key, result);

                if (!result)
                {
                    Info("Initializing for {0}", initializer.Key);

                    await initializer.Value.ActionFunc(client);
                }

                Info("External status for {0} was ok - storing internally", initializer.Key);
            }
            finally
            {
                var source = _lifeStyle.TryGet<TaskCompletionSource<object>>(initializer.Key);
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
                ProbeFunc = async client => (await probeFunc(client)).Mapping != null;
                return this;
            }

            public IInitializer Action(Func<IElasticClient, Task<bool>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result = await actionFunc(client);

                    if (!result)
                        throw new UnableToExecuteActionException(string.Format("Action-function for {0} failed. Result was false", _name));

                };

                return this;
            }

            public IInitializer Action(Func<IElasticClient, Task<IIndicesOperationResponse>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result = await actionFunc(client);

                    if (!result.IsValid)
                        throw new UnableToExecuteActionException(string.Format("Action-function for {0} failed. {1}", _name, result.ServerError != null ? result.ServerError.Error : null));

                };

                return this;
            }

            public IInitializer Action(Func<IElasticClient, Task<IIndicesResponse>> actionFunc)
            {
                ActionFunc = async client =>
                {
                    var result =  await actionFunc(client);

                    if (!result.IsValid)
                        throw new UnableToExecuteActionException(string.Format("Action-function for {0} failed. {1}", _name, result.ServerError != null ? result.ServerError.Error : null));
                                       
                };

                return this;
            }
        }

        public IClientFactory LogTo(Action<string, object[]> logger)
        {
            _logger = logger;
            return this;
        }

        public IClientFactory ConstructUsing(Func<IElasticClient> func)
        {
            _clientConstructor = func;
            return this;
        }

        public IClientFactory EnableInfoLogging()
        {
            _infoLoggingEnabled = true;
            return this;
        }
    }
}