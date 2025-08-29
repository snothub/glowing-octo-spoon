using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;

namespace IdentityService;

public class DomainCustomAuthorizeRequestValidator : ICustomAuthorizeRequestValidator
{
    private readonly ILogger<DomainCustomAuthorizeRequestValidator> _logger;
    private readonly IResourceStore _resourceStore;

    public DomainCustomAuthorizeRequestValidator(ILogger<DomainCustomAuthorizeRequestValidator> logger,[FromKeyedServices(DomainConstants.KeyedService)]  IResourceStore resourceStore)
    {
        _logger = logger;
        _resourceStore = resourceStore;
    }
    public async Task ValidateAsync(CustomAuthorizeRequestValidationContext context)
    {
        var requested = context.Result!.ValidatedRequest.RequestedScopes;
        if (!requested.Any())
        {
            _logger.LogInformation("No scopes requested");
            return;
        }

        var dataPortScopes = requested.Where(s => s.StartsWith($"{DomainConstants.DomainPrefix}")).ToList();
        if (!dataPortScopes.Any())
        {
            _logger.LogInformation($"No {DomainConstants.DomainPrefix} scopes requested");
            return;
        }

        var allScopes = (await _resourceStore.FindApiScopesByNameAsync(dataPortScopes)).Select(scope => scope.Name).ToList();
        _logger.LogInformation($"All {DomainConstants.DomainPrefix} scopes: {{Scopes}}", allScopes);
        allScopes.Add("openid");
        context.Result.ValidatedRequest.RequestedScopes.Clear();
        context.Result.ValidatedRequest.RequestedScopes.AddRange(allScopes);
        context.Result.ValidatedRequest.Raw["scope"] = string.Join(" ", allScopes);
    }
}