using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using IdentityService.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    public class ConsentController : Controller
    {
        private readonly ILogger<ConsentController> _logger;
        private readonly IIdentityServerInteractionService _interaction;

        public ConsentController(ILogger<ConsentController> logger, IIdentityServerInteractionService interaction)
        {
            _logger = logger;
            _interaction = interaction;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost, Route("/consent/save"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Consent([FromBody] InputModel consent)
        {
            var request = await _interaction.GetAuthorizationContextAsync(consent.ReturnUrl);
            if (request is null)
            {
                _logger.LogWarning("Invalid consent request");
                return BadRequest();
            }
            
            ConsentResponse? grantedConsent;
            if (consent.Button == "no")
            {
                grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };
            }
            else if (consent.Button == "yes")
            {
                // if the user consented to some scope, build the response model
                if (consent.ScopesConsented.Any())
                {
                    var scopes = consent.ScopesConsented;

                    grantedConsent = new ConsentResponse
                    {
                        RememberConsent = consent.RememberConsent,
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = consent.Description
                    };
                }
                else
                {
                    _logger.LogWarning("Must choose one scope");
                    return BadRequest();
                }
            }
            else
            {
                _logger.LogWarning("Invalid selection");
                return BadRequest();
            }
            
            await _interaction.GrantConsentAsync(request, grantedConsent);
            
            return Ok(new { consent.ReturnUrl });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost, Route("/consent"), IgnoreAntiforgeryToken]
        public async Task<IActionResult> Consent([FromBody] ConsentPayload consent)
        {
            var viewModel = await BuildViewModelAsync(consent.ReturnUrl);
            return Ok(viewModel);
        }

        private async Task<ViewModel> BuildViewModelAsync(string returnUrl, InputModel model = null)
        {
            _logger.LogDebug("Building view model");
            var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (request != null)
            {
                return CreateConsentViewModel(model, request);
            }

            _logger.LogWarning("No consent request matching request: {Req}", returnUrl);
            return null;
        }
        
        private ViewModel CreateConsentViewModel(
            InputModel model,
            AuthorizationRequest request)
        {
            _logger.LogDebug("Building consent view model");
            var vm = new ViewModel
            {
                ClientName = request.Client.ClientName ?? request.Client.ClientId,
                ClientUrl = request.Client.ClientUri,
                ClientLogoUrl = request.Client.LogoUri,
                AllowRememberConsent = request.Client.AllowRememberConsent,
                IdentityScopes = request.ValidatedResources.Resources.IdentityResources
                    .Select(x => CreateScopeViewModel(x, model?.ScopesConsented == null || model.ScopesConsented?.Contains(x.Name) == true))
                    .ToArray()
            };

            var resourceIndicators = request.Parameters.GetValues(OidcConstants.AuthorizeRequest.Resource) ?? Enumerable.Empty<string>();
            var apiResources = request.ValidatedResources.Resources.ApiResources.Where(x => resourceIndicators.Contains(x.Name));

            var apiScopes = new List<ScopeViewModel>();
            foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
            {
                _logger.LogDebug("Parsing scope '{Scope}", parsedScope.ParsedName);
                var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
                if (apiScope != null)
                {
                    _logger.LogDebug("Parsing api scope '{Scope}", apiScope.Name);
                    var scopeVm = CreateScopeViewModel(parsedScope, apiScope, model == null || model.ScopesConsented?.Contains(parsedScope.RawValue) == true);
                    scopeVm.Resources = apiResources.Where(x => x.Scopes.Contains(parsedScope.ParsedName))
                        .Select(x => new ResourceViewModel
                        {
                            Name = x.Name,
                            DisplayName = x.DisplayName ?? x.Name,
                        }).ToArray();
                    apiScopes.Add(scopeVm);
                }
            }
            if (request.ValidatedResources.Resources.OfflineAccess)
            {
                apiScopes.Add(GetOfflineAccessScope(model == null || model.ScopesConsented?.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) == true));
            }
            vm.ApiScopes = apiScopes;
            _logger.LogDebug("Returning consent view model");

            return vm;
        }

        private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check)
        {
            return new ScopeViewModel
            {
                Name = identity.Name,
                Value = identity.Name,
                DisplayName = identity.DisplayName ?? identity.Name,
                Description = identity.Description,
                Emphasize = identity.Emphasize,
                Required = identity.Required,
                Checked = check || identity.Required
            };
        }

        public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
        {
            var displayName = apiScope.DisplayName ?? apiScope.Name;
            if (!string.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
            {
                displayName += ":" + parsedScopeValue.ParsedParameter;
            }

            return new ScopeViewModel
            {
                Name = parsedScopeValue.ParsedName,
                Value = parsedScopeValue.RawValue,
                DisplayName = displayName,
                Description = apiScope.Description,
                Emphasize = apiScope.Emphasize,
                Required = apiScope.Required,
                Checked = check || apiScope.Required
            };
        }

        private ScopeViewModel GetOfflineAccessScope(bool check)
        {
            return new ScopeViewModel
            {
                Value = IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = "offline_access",
                Description = "Tillater tilgang til dine data uten din tilstedeværelse",
                Emphasize = true,
                Checked = check
            };
        }
    }

    public class ConsentPayload
    {
        public string ReturnUrl { get; set; }
    }
}