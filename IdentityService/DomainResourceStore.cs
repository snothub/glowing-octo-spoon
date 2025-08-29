using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace IdentityService;

public class DomainResourceStore : IResourceStore
{
    private readonly IResourceStore _inner;
    private readonly ILogger<DomainResourceStore> _logger;

    public DomainResourceStore(IResourceStore inner,  ILogger<DomainResourceStore> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        return _inner.FindIdentityResourcesByScopeNameAsync(scopeNames);
    }

    public async Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        var names = scopeNames.ToList();
        if (!names.Any())
        {
            return new List<ApiScope>();
        }

        _logger.LogDebug("Scopes requested: {Scopes}", string.Join(", ", names));
        var scopeSearch = names.FirstOrDefault(s => s.Equals($"{DomainConstants.DomainPrefix}"));
        if (string.IsNullOrWhiteSpace(scopeSearch))
        {
            _logger.LogDebug("No custom scopes to filter");
            return await _inner.FindApiScopesByNameAsync(names);
        }

        _logger.LogDebug("Scopes to filter: {Scopes}", scopeSearch);

        var all = await _inner.GetAllResourcesAsync();

        var query =
            from scope in all.ApiScopes
            where scope.Name.StartsWith(scopeSearch)
            select scope;

        var apiScopes = query.ToList();
        _logger.LogDebug("Scopes from store: {Scopes}", apiScopes.Select(scope => scope.Name));

        return apiScopes;
    }

    public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        return _inner.FindApiResourcesByScopeNameAsync(scopeNames);
    }

    public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        return _inner.FindApiResourcesByNameAsync(apiResourceNames);
    }

    public Task<Resources> GetAllResourcesAsync()
    {
        return _inner.GetAllResourcesAsync();
    }
}