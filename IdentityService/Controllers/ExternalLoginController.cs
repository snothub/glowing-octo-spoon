using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdentityService.Controllers
{
    [AllowAnonymous]
    public class ExternalLoginController : Controller
    {
        private readonly ILogger<ExternalLoginController> _logger;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly TestUserStore _users;

        public ExternalLoginController(
            ILogger<ExternalLoginController> logger,
            IIdentityServerInteractionService interaction,
            IEventService events,
            TestUserStore users)
        {
            _logger = logger;
            _interaction = interaction;
            _events = events;
            _users = users;
        }

        /// <summary>
        /// Initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        [Route("/ExternalLogin/Challenge")]
        public IActionResult Challenge(string scheme, string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl)) returnUrl = "~/";

            // validate returnUrl - either it is a valid OIDC URL or back to a local page
            if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false)
            {
                // user might have clicked on a malicious link - should be logged
                throw new Exception("invalid return URL");
            }

            // start challenge and roundtrip the return URL and scheme 
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback)),
                Items =
                {
                    { "returnUrl", returnUrl },
                    { "scheme", scheme },
                }
            };

            return Challenge(props, scheme);
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        [Route("/ExternalLogin/Callback")]
        public async Task<IActionResult> Callback()
        {
            // read external identity from the temporary cookie
            var result = await HttpContext.AuthenticateAsync(DomainConstants.ExternalScheme);
            if (result.Succeeded != true)
            {
                _logger.LogWarning("External login failed: {Result}", JsonSerializer.Serialize(result));
                throw new Exception("External authentication error");
            }
            _logger.LogInformation("External login success");

            var externalClaims = result.Principal.Claims.Select(c => $"{c.Type}: {c.Value}");
            _logger.LogDebug("External claims: {@claims}", JsonSerializer.Serialize(externalClaims));

            // lookup our user and external provider info
            var (user, provider, providerUserId, claims) = FindUserFromExternalProvider(result);
            if (user == null)
            {
                _logger.LogDebug("Provision user");
                // this might be where you might initiate a custom workflow for user registration
                // in this sample we don't show how that would be done, as our sample implementation
                // simply auto-provisions new external user
                user = AutoProvisionUser(provider, providerUserId, claims);
            }

            _logger.LogDebug("User: Subj:{Subj}, ProvSubj:{PSubj}, UName: {UName}, Claims: {Claims}", user.SubjectId, user.ProviderSubjectId, user.Username, user.Claims.Select(c => $"{c.Type}: {c.Value}"));
            // this allows us to collect any additional claims or properties
            // for the specific protocols used and store them in the local auth cookie.
            // this is typically used to store data needed for signout from those protocols.
            var additionalLocalClaims = new List<Claim>();
            var localSignInProps = new AuthenticationProperties();
            CaptureExternalLoginContext(result, additionalLocalClaims, localSignInProps);

            // issue authentication cookie for user
            var isuser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username,
                IdentityProvider = provider,
                AdditionalClaims = additionalLocalClaims
            };
            _logger.LogDebug("localSignInProps: {@SignInProps}", JsonSerializer.Serialize(localSignInProps));

            await HttpContext.SignInAsync(isuser, localSignInProps);

            _logger.LogDebug("Deleting temporary cookie");
            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync(DomainConstants.ExternalScheme);

            // retrieve return URL
            var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";

            // check if external login is in the context of an OIDC request
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.SubjectId, user.Username, true, context?.Client.ClientId));
            
            return Redirect(returnUrl);
        }

        private (TestUser user, string provider, string providerUserId, IEnumerable<Claim> claims) FindUserFromExternalProvider(AuthenticateResult result)
        {
            var externalUser = result.Principal;
            _logger.LogDebug("Getting external user");

            // try to determine the unique id of the external user (issued by the provider)
            // the most common claim type for that are the sub claim and the NameIdentifier
            // depending on the external provider, some other claim type might be used
            var userIdClaim = externalUser.FindFirst(JwtClaimTypes.Subject) ??
                              externalUser.FindFirst(ClaimTypes.NameIdentifier) ??
                              throw new Exception("Unknown userid");

            // remove the user id claim so we don't include it as an extra claim if/when we provision the user
            var claims = externalUser.Claims.ToList();
            claims.Remove(userIdClaim);

            var provider = result.Properties.Items["scheme"];
            var providerUserId = userIdClaim.Value;

            _logger.LogDebug("Looking for external user {Id} with provider {Pro}", providerUserId, provider);
            // find external user
            var user = _users.FindByExternalProvider(provider, providerUserId);
            _logger.LogInformation("User detected: {User}", user?.Username ?? user?.SubjectId);

            return (user, provider, providerUserId, claims);
        }

        private TestUser AutoProvisionUser(string provider, string providerUserId, IEnumerable<Claim> claims)
        {
            var user = _users.AutoProvisionUser(provider, providerUserId, claims.ToList());
            return user;
        }

        private void CaptureExternalLoginContext(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
        {
            // if the external system sent a session id claim, copy it over
            // so we can use it for single sign-out
            var sid = externalResult.Principal.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null)
            {
                localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
            }

            // if the external provider issued an id_token, we'll keep it for signout
            var idToken = externalResult.Properties.GetTokenValue("id_token");
            if (idToken != null)
            {
                localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
            }
        }
    }

    public static class LoadingPageExtensions
    {
        public static IActionResult LoadingPage(this Controller controller, string redirectUri)
        {
            controller.HttpContext.Response.StatusCode = 200;
            controller.HttpContext.Response.Headers["Location"] = "";
            
            return controller.Content($@"
    <html>
    <head>
        <meta http-equiv='refresh' content='0;url={redirectUri}'>
    </head>
    <body>
        <p>Loading...</p>
        <script>window.location.href = '{redirectUri}';</script>
    </body>
    </html>", "text/html");
        }
    }
}