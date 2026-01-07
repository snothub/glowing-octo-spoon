using Duende.IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api;

[Route("/")]
public class IdentityController : ControllerBase
{
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(ILogger<IdentityController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public ActionResult Get()
    {
        var title = "FIRST";
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        _logger.LogInformation("claims: {claims}", claims);

        var scheme = GetAuthorizationScheme(Request);
        var proofToken = GetDPoPProofToken(Request);

        return new JsonResult(new { title, scheme, proofToken, claims });
    }

    [HttpGet]
    [Route("second")]
    public ActionResult GetSecond()
    {
        var title = "SECOND";
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        _logger.LogInformation("claims: {claims}", claims);

        var scheme = GetAuthorizationScheme(Request);
        var proofToken = GetDPoPProofToken(Request);

        return new JsonResult(new { second = title, scheme, proofToken, claims });
    }

    [HttpGet, AllowAnonymous]
    [Route("third")]
    public ActionResult GetThird()
    {
        var title = "THIRD";
        return new JsonResult(new {  title });
    }

    public static string? GetAuthorizationScheme(HttpRequest request)
    {
        return request.Headers.Authorization.FirstOrDefault()?.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)[0];
    }

    public static string? GetDPoPProofToken(HttpRequest request)
    {
        return request.Headers[OidcConstants.HttpHeaders.DPoP].FirstOrDefault();
    }
}
