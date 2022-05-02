using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nest;
using NestClientFactory;

namespace NestClientFactoryTests
{
    [TestClass]
    public class QueryBuilderTests
    {
        [TestMethod]
        public async Task Can_query()
        {
            ////Arrange
            IElasticClient client = new ElasticClient();
            
            var queryA = new TestAuxilioQuery("a");
            var queryB = new TestAuxilioQuery("b");

            ////Act
            await client.Find(queryA, queryB);

            ////Assert
            var resultA = queryA.Documents;
            Assert.AreEqual(resultA,"a");
        }
    }

    public class TestAuxilioQuery : IAuxilioQuery
    {
        public TestAuxilioQuery(string testName)
        {
            Documents = testName;
        }

        public void Build(MultiSearchDescriptor descriptor)
        {
        }

        public void Extract(IMultiSearchResponse multiSearchResponse)
        {
        }

        public string Documents { get; set; }
    }
}
