using Configuration.Core.Repository;

namespace Configuration.Core.Factory
{
    public interface IConfigurationRepositoryFactory
    {
        IConfigurationRepository CreateRepository(string connectionString);
    }
}

