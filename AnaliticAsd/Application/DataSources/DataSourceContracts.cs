using AnaliticAsd.Domain.DataSources;

namespace AnaliticAsd.Application.DataSources;

public interface IProviderConnectionRepository
{
    Task<ProviderConnection?> GetLatestAsync(Guid agencyId, DataProvider provider, bool trackChanges,
        CancellationToken cancellationToken = default);

    void Add(ProviderConnection connection);
}
