using System.Text.Json;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Duende.IdentityServer.Stores;
using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Cors;

namespace IdentityService.Controllers
{
    [DisableCors]
    public class LoginController : Controller
    {
        private readonly IUserSession _userSession;
        private readonly ISessionManagementService _sessionManagementService;
        private readonly ILogger<LoginController> _logger;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly TestUserStore _users;

        public LoginController(IUserSession userSession, ISessionManagementService sessionManagementService, ILogger<LoginController> logger, IIdentityServerInteractionService interaction, IEventService events, TestUserStore? users = null)
        {
            _userSession = userSession;
            _sessionManagementService = sessionManagementService;
            _logger = logger;
            _interaction = interaction;
            _events = events;
            _users = users ?? throw new InvalidOperationException("Please call 'AddTestUsers(TestUsers.Users)' on the IIdentityServerBuilder in Startup or remove the TestUserStore from the AccountController.");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet, Route("/logout")]
        public async Task<IActionResult> Logout([FromQuery] string logoutId)
        {
            var logout = await _interaction.GetLogoutContextAsync(logoutId);
            if (logout.SubjectId is null)
            {
                _logger.LogWarning("Logout context is missing");
                return BadRequest();
            }

            await HttpContext.SignOutAsync(DomainConstants.IdsrvDefaultAuthenticationScheme);
            return Redirect(logout.PostLogoutRedirectUri ?? "~/");

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet, Route("/login/session")]
        public async Task<IActionResult> GetLoginSession()
        {
            var sq = new SessionQuery();
//            var sq = new SessionQuery {SubjectId = "subjectId"};
            var qr = await _sessionManagementService.QuerySessionsAsync(sq);
            var sess = qr.Results.FirstOrDefault();
            var clients = string.Join(',', sess!.ClientIds.ToArray());
            return Ok(new { sess.SessionId, sess.SubjectId, Clients = clients });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet, Route("/login/s1")]
        public async Task<IActionResult> Session1()
        {
            await _userSession.AddClientIdAsync("m2m");
            
            return Ok(JsonSerializer.Serialize(await _userSession.GetClientListAsync()));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpGet, Route("/login/context")]
        public async Task<IActionResult> GetLoginContext([FromQuery] string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
            {
                return BadRequest(new { error = "ReturnUrl is required" });
            }

            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context == null)
            {
                return BadRequest(new { error = "Invalid return URL" });
            }

            // Check for IdP in ACR values
            string? requestedIdP = context.IdP;
            if (string.IsNullOrEmpty(requestedIdP) && context.AcrValues != null)
            {
                var idpAcr = context.AcrValues.FirstOrDefault(x => x.StartsWith("idp:"));
                if (idpAcr != null)
                {
                    requestedIdP = idpAcr.Substring(4); // Remove "idp:" prefix
                }
            }

            // If external provider is requested, redirect to it
            if (!string.IsNullOrEmpty(requestedIdP) && 
                requestedIdP != IdentityServerConstants.LocalIdentityProvider)
            {
                return Ok(new { 
                    requiresExternalLogin = true, 
                    externalProvider = requestedIdP,
                    returnUrl = returnUrl
                });
            }

            return Ok(new { 
                requiresExternalLogin = false,
                returnUrl = returnUrl
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [HttpPost, Route("/login")]
        public async Task<IActionResult> Index([FromBody] LoginPayload request)
        {
            _logger.LogDebug("Payload received: {ReturnUrl} - {UserName}", request.ReturnUrl, request.Username);
            
            var context = await _interaction.GetAuthorizationContextAsync(request.ReturnUrl);
            if (context is null)
            {
                _logger.LogWarning("Invalid return URL - {Uri}", request.ReturnUrl);
                return BadRequest(new { error = $"Invalid return URL: {request.ReturnUrl}" });
            }

            if (!_users.ValidateCredentials(request.Username, request.Password))
            {
                _logger.LogWarning("Invalid credentials");
                return BadRequest(new { error = "Invalid credentials" });
            }

            var user = _users.FindByUsername(request.Username);
            await _events.RaiseAsync(new UserLoginSuccessEvent(user.Username, user.SubjectId, user.Username, clientId: context?.Client.ClientId));

            var props = new AuthenticationProperties();

            var isuser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username
            };

            await HttpContext.SignInAsync(isuser, props);

            if (context != null)
            {
                // This "can't happen", because if the ReturnUrl was null, then the context would be null
                ArgumentNullException.ThrowIfNull(request.ReturnUrl, nameof(request.ReturnUrl));
            }

            if (string.IsNullOrEmpty(request.ReturnUrl))
            {
                _logger.LogWarning("Empty return url");
                return BadRequest(new { error = "Empty return url" });
            }

            return Ok(new { request.ReturnUrl});
        }
    }
}
