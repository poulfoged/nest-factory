using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nest;
using NestClientFactory;
using NestClientFactory.Lifestyle;
using NUnit.Framework;

namespace NestClientFactoryTests
{
    [TestFixture]
    public class ClientFactoryTests
    {
        [Test]
        public async Task When_probe_returns_false_action_is_executed()
        {
            ////Arrange
            var wasActionCalled = false;

            ////Act
            await new ClientFactory()
                .InitializationLifeStyle(new TransientLifestyle())
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.FromResult(false))
                    .Action(async elasticClient => await Task.Run(() => wasActionCalled = true)))
                .CreateClient();

            ////Assert
            Assert.That(wasActionCalled);
        }

        [Test]
        public async Task When_probe_returns_true_action_is_not_executed()
        {
            ////Arrange
            var wasActionCalled = false;

            ////Act
            await new ClientFactory()
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.FromResult(true))
                    .Action(async elasticClient => await Task.Run(() => wasActionCalled = true)))
                .CreateClient();

            ////Assert
            Assert.That(wasActionCalled, Is.False);
        }

        [Test]
        public async Task Trancient_lifestyle_calls_every_time()
        {
            ////Arrange
            var wasActionCalled = false;

            await new ClientFactory()
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.FromResult(true))
                    .Action(async elasticClient => await Task.FromResult(true)))
                .CreateClient();

            ////Act
            await new ClientFactory()
                .InitializationLifeStyle(new TransientLifestyle())
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.Run(() => wasActionCalled = true))
                    .Action(async elasticClient => await Task.FromResult(true)))
                .CreateClient();

            ////Assert
            Assert.That(wasActionCalled);
        }

        [Test]
        public async Task When_already_ensured_nothing_will_execute()
        {
            ////Arrange
            var wasActionCalled = false;

            await new ClientFactory()
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.FromResult(true))
                    .Action(async elasticClient => await Task.FromResult(true)))
                .CreateClient();

            ////Act
            await new ClientFactory()
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.Run(() => wasActionCalled = true))
                    .Action(async elasticClient => await Task.FromResult(true)))
                .CreateClient();

            ////Assert
            Assert.That(wasActionCalled, Is.False);
        }

        [Test]
        public async Task With_multiple_threads_statement_is_executed_once()
        {
            ////Arrange
            var count = 0;
            var lifestyle = new TransientLifestyle();
            ////Act
            var tasks = Enumerable.Range(0, 100).Select(r => Task.Factory.StartNew(async () =>
            {
                await new ClientFactory()
                .InitializationLifeStyle(lifestyle)
                .Initialize("my-index", i => i
                    .Probe(async elasticClient => await Task.Run(delegate { count++; return false; }))
                    .Action(async elasticClient => await Task.FromResult(true)))
                .CreateClient();
            }, TaskCreationOptions.LongRunning)).ToList();
            await Task.WhenAll(tasks);

            ////Assert
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public async Task All_calls_will_wait_for_the_first_call()
        {
            ////Arrange
            var started = DateTime.UtcNow;

            IList<TimeSpan> executionTimes = new List<TimeSpan>();

            ////Act
            var tasks = Enumerable.Range(0, 100).Select(r => Task.Factory.StartNew(async () =>
            {
                await new ClientFactory()
                    .Initialize("my-index", i => i
                        .Probe(async elasticClient => await Task.Run(async delegate { await Task.Delay(1200); executionTimes.Add(DateTime.UtcNow-started); return false; }))
                        .Action(async elasticClient => await Task.FromResult(true)))
                    .CreateClient();
            }, TaskCreationOptions.LongRunning)).ToList();
            await Task.WhenAll(tasks);

            ////Assert
            Assert.That(executionTimes.All(e => e.TotalSeconds > 1000));
        }

        [Test, Ignore("Requires server")]
        public async Task Full_interface()
        {
            var elasticClient = await new ClientFactory()
                .ConstructUsing(() => new ElasticClient())
                .EnableInfoLogging()
                .LogTo((format, args) => Trace.WriteLine(string.Format(format, args)))
                .InitializationLifeStyle(new StaticLifestyle())
                .Initialize("my-index", i => i
                    .Probe(async client => await client.IndexExistsAsync(a => a.Index("test_index")))
                    .Action(async client => await client.CreateIndexAsync(a => a.Index("test_index"))))
                .Initialize("my-mapping", i => i
                    .Probe(async client => await client.GetMappingAsync<dynamic>(m => m.Index("test_index").Type("my-type")))
                    .Action(async client => await client.MapAsync<dynamic>(m => m.Index("test_index").Type("my-type").Properties(p => p.String(s => s.Name("hello"))))))
                .Initialize("my-alias", i => i
                    .Probe(async client => await client.AliasExistsAsync(a => a.Name("test_read")))
                    .Action(async client => await client.AliasAsync(a => a.Add(b => b.Alias("test_read").Index("test_index")))))
                .CreateClient();
        }

        [Test, Ignore("Requires server")]
        public async Task Automatic_cleanup()
        {
            var factory =  new ClientFactory()
                .ConstructUsing(() => new ElasticClient())
                .EnableInfoLogging()
                .LogTo((format, args) => Trace.WriteLine(string.Format(format, args)))
                .InitializationLifeStyle(new StaticLifestyle())
                .Initialize("my-index", i => i
                    .Probe(async client => await client.IndexExistsAsync(a => a.Index("test_index")))
                    .Action(async client => await client.CreateIndexAsync(a => a.Index("test_index")))
                    .Cleanup(async client => await client.DeleteIndexAsync(a => a.Index("test_index"))))
                .Initialize("my-mapping", i => i
                    .Probe(async client => await client.GetMappingAsync<dynamic>(m => m.Index("test_index").Type("my-type")))
                    .Action(async client => await client.MapAsync<dynamic>(m => m.Index("test_index").Type("my-type").Properties(p => p.String(s => s.Name("hello"))))))
                .Initialize("my-alias", i => i
                    .Probe(async client => await client.AliasExistsAsync(a => a.Name("test_read")))
                    .Action(async client => await client.AliasAsync(a => a.Add(b => b.Alias("test_read").Index("test_index")))));

            using (factory.AutomaticCleanup())
            {
                var elasticClient = await factory.CreateClient();




            }
        }
    }
}


