using System.Collections.Specialized;
using Duende.IdentityServer;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;

namespace IdentityService;

internal class OidcReturnUrlParser : IReturnUrlParser
{
    private readonly IdentityServerOptions _options;
    private readonly IAuthorizeRequestValidator _validator;
    private readonly IUserSession _userSession;
    private readonly IServerUrls _urls;
    private readonly ILogger _logger;
    private readonly IAuthorizationParametersMessageStore? _authorizationParametersMessageStore;

    public OidcReturnUrlParser(
        IdentityServerOptions options,
        IAuthorizeRequestValidator validator,
        IUserSession userSession,
        IServerUrls urls,
        ILogger<OidcReturnUrlParser> logger,
        IAuthorizationParametersMessageStore? authorizationParametersMessageStore = null)
    {
        _options = options;
        _validator = validator;
        _userSession = userSession;
        _urls = urls;
        _logger = logger;
        _authorizationParametersMessageStore = authorizationParametersMessageStore;
    }

    public async Task<AuthorizationRequest?> ParseAsync(string returnUrl)
    {
        _logger.LogInformation("Parsing request to Oidc returnUrl");
        if (IsValidReturnUrl(returnUrl))
        {
            var parameters = returnUrl.ReadQueryStringAsNameValueCollection();
            if (_authorizationParametersMessageStore != null)
            {
                var messageStoreId = parameters["authzId"];
                var entry = await _authorizationParametersMessageStore.ReadAsync(messageStoreId);
                parameters = entry?.Data.FromFullDictionary() ?? new NameValueCollection();
            }

            var user = await _userSession.GetUserAsync();
            var result = await _validator.ValidateAsync(parameters, user);
            if (!result.IsError)
            {
                _logger.LogTrace("AuthorizationRequest being returned");
                return new AuthorizationRequest(result.ValidatedRequest);
            }
        }

        _logger.LogTrace("No AuthorizationRequest being returned");
        return null;
    }

    public bool IsValidReturnUrl(string returnUrl)
    {
        _logger.LogDebug("Validating return URL '{Url}", returnUrl);
        if (_options.UserInteraction.AllowOriginInReturnUrl && returnUrl.IsUri())
        {
            _logger.LogDebug("Validating origin '{Url}", returnUrl);
            var host = _urls.Origin;
            if (returnUrl.StartsWith(host, StringComparison.OrdinalIgnoreCase))
            {
                returnUrl = returnUrl.Substring(host.Length);
            }
        }
            
        if (returnUrl.IsLocalUrl())
        {
            _logger.LogDebug("Validating local URL '{Url}", returnUrl);
            {
                var index = returnUrl.IndexOf('?');
                if (index >= 0)
                {
                    returnUrl = returnUrl.Substring(0, index);
                }
            }
            {
                var index = returnUrl.IndexOf('#');
                if (index >= 0)
                {
                    returnUrl = returnUrl.Substring(0, index);
                }
            }

            if (returnUrl.EndsWith(IdentityServerConstants.ProtocolRoutePaths.Authorize, StringComparison.Ordinal) ||
                returnUrl.EndsWith(IdentityServerConstants.ProtocolRoutePaths.AuthorizeCallback, StringComparison.Ordinal))
            {
                _logger.LogDebug("returnUrl is valid");
                return true;
            }
        }

        _logger.LogDebug("returnUrl is not valid");
        return false;
    }
}