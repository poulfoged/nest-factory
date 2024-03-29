![](https://raw.githubusercontent.com/poulfoged/nest-factory/master/logo-large.png) &nbsp; ![](https://ci.appveyor.com/api/projects/status/x95khbu1him0stwi/branch/master?svg=true) &nbsp; ![](http://img.shields.io/nuget/v/nest-factory.svg?style=flat)# NEST Client Factory  
A factory for  [Elasticsearch-NEST][nest]'s client that provides easy initial creation of Elasticsearch resources such as indices, mappings and aliases.When connecting to Elasticsearch you sometimes want to make sure that all indices, mappings and aliases have been correctly created before making the actual connection.## How to
### This factory will* Ensure a minimum number of requests.* Store the state of initialization for the duration of the current process (can be customized).* Perform all initialization before a IElasticClient is returned.* Ensure that subsequent requests for clients are halted until initial initialization has occurred.* Not contain any state - the default ILifestyle-manager uses a static dictionary internally. So factories can be created and destroyed at will.The factory is configured via a fluent interface - here the simplest example, just creating a client:```c#var client = await new ClientFactory()	.CreateClient();```### Configure initialization-stepsWhen configuring initialization-steps a key is provided, this key is used to store initialization status and should be unique.Each step contains a probe-section that is called if internal status is unknown. The probe-section should return a either a bool or one of NEST's build in responses indicating whether the action-section should be executed. To configure some initialization steps - here we setup a index with mapping and an alias:```c# var elasticClient = await new ClientFactory()
    .ConstructUsing(() => new ElasticClient())
    .EnableInfoLogging()
    .LogTo((format, args) => Trace.WriteLine(string.Format(format, args)))
    .InitializationLifeStyle(new StaticLifestyle())
    .Initialize("my-index", i => i
        .Probe(async client => await client.IndexExistsAsync(Indices.Index("test_index")))
        .Action(async client => await client.CreateIndexAsync("test_index")))
    .Initialize("my-mapping", i => i
        .Probe(async client => await client.GetMappingAsync<dynamic>(m => m.Index("test_index").Type("my-type")))
        .Action(async client => await client.MapAsync<dynamic>(m => m.Index("test_index").Type("my-type").Properties(p => p.String(s => s.Name("hello"))))))
    .Initialize("my-alias", i => i
        .Probe(async client => await client.AliasExistsAsync(a => a.Name("test_read")))
        .Action(async client => await client.AliasAsync(a => a.Add(b => b.Alias("test_read").Index("test_index")))))
    .CreateClient();```### Configure factory methodWhen the actual ElasticClient is created it defaults to just calling new() - this can be overridden with the ConstructUsing statement. The address of the server should also be passed in - this enables the factory to keep track of initialization against multiple clusters within a single process:```c#
var serverAddress = new Uri("http://localhost:9200");
var elasticClient = await new ClientFactory()    .ConstructUsing(() => new ElasticClient(new ConnectionSettings(serverAddress).EnableTrace()), serverAddress)    .CreateClient();```###LoggingFor debugging purposes, logging can be enabled. Defaults to write to trace but can be configured with an alternative logger (arguments passed are similar to String.Format:```c#var elasticClient = await new ClientFactory()    .EnableInfoLogging()    .LogTo((format, args) => Trace.WriteLine(string.Format(format, args)))    .CreateClient();```###Lifestyle
Lifestyle defaults to StaticLifestyle that simply stores status of each initialization in a ConcurrentDictionary (using TaskCompletionSource's to synchronize across threads). This can be customized.Changing lifestyle to NullLifestyle will completely disable state and cause each and every client-creation to execute all initialization-probe-steps.## Install

Simply add the Nuget package:

`PM> Install-Package nest-factory`

## Requirements

You'll need .NET Framework 4.5.1 and NEST 2.x or later to use the precompiled binaries.

## License

NEST Client Factory is under the MIT license. 
[nest]: https://github.com/elastic/elasticsearch-net  "Elasticsearch.Net & NEST"

