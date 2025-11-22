using Configuration.Core.Entity;

namespace Configuration.Core.Repository
{
    public interface IConfigurationRepository
    {
        IEnumerable<ConfigurationRecord> GetAllActiveByApplication(string applicationName);
    }
}
