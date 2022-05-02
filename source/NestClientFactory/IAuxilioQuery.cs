using Nest;

namespace NestClientFactory
{
    public interface IAuxilioQuery
    {
        void Build(MultiSearchDescriptor descriptor);
        void Extract(IMultiSearchResponse multiSearchResponse);
    }
}