using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

namespace IdentityService;

public class AllowAnyRedirectUriValidator : IRedirectUriValidator
{
    public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client) =>
        Task.FromResult(true);

    public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client) =>
        Task.FromResult(true);
}