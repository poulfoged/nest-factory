using Nest;

namespace NestClientFactory
{
  public interface IConnectionSettingsConfigurator
  {
    void Configure(ConnectionSettings settings);
  }
}
