using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityModel;
using IdentityModel.Client;
using IdentityService;
using Microsoft.IdentityModel.Tokens;


namespace ConsoleClient;

public class ClientRunner
{
    public async Task<string?> GetToken()
    {
        var response = await RequestTokenAsync();

        // await CallServiceAsync(response.AccessToken);
        return response.AccessToken;
    }
    
    async Task<TokenResponse> RequestTokenAsync()
    {
        var client = new HttpClient();

        var clientToken = CreateClientToken(DomainConstants.JwtClientId, "http://identity");
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = $"{DomainConstants.IdpAuthority}/connect/token",

            ClientAssertion =
            {
                Type = OidcConstants.ClientAssertionTypes.JwtBearer,
                Value = clientToken
            },

            Scope = $"{DomainConstants.DomainPrefix}park {DomainConstants.DomainPrefix}member",
        });

        if (response.IsError) throw new Exception(response.Error);
        return response;
    }

    string CreateClientToken(string clientId, string audience)
    {
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            clientId,
            audience,
            new List<Claim>
            {
                new(JwtClaimTypes.JwtId, Guid.NewGuid().ToString()),
                new(JwtClaimTypes.Subject, clientId),
                new(JwtClaimTypes.IssuedAt, now.ToEpochTime().ToString(), ClaimValueTypes.Integer64)
            },
            now,
            now.AddMinutes(1),
            new SigningCredentials(DomainConstants.ClientJwk, "RS256")
        )
        {
            Header =
            {
                [JwtClaimTypes.TokenType] = "client-authentication+jwt"
            }
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var clientToken = tokenHandler.WriteToken(token);
        return clientToken;
    }

    
    public async Task<string> CallServiceAsync(string token)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:7700")
        };
    
        client.SetBearerToken(token);
        return await client.GetStringAsync("api");
    }
}