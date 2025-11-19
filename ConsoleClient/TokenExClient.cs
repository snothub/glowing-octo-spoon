using IdentityModel;
using IdentityModel.Client;
using IdentityService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleClient;

public class TokenExClient : BackgroundService
{
    private readonly ILogger<TokenExClient> _logger;

    public TokenExClient(ILogger<TokenExClient> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = await GetAccessToken();
        Console.WriteLine(token);
        Console.ReadLine();
        Console.Clear();
        
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Exchanging token at: {time}", DateTimeOffset.Now);

            token = await ExchangeToken(token!);
            Console.Clear();
            Console.WriteLine(token);
            Console.ReadLine();

            await Task.Delay(2000, stoppingToken);
        }
    }

    private async Task<string?> ExchangeToken(string token)
    {
        var client = new HttpClient();
        var response = await client.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
        {
            SubjectToken = token,
            SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken,
            Address = $"{DomainConstants.IdpAuthority}/connect/token",
            ClientId = "localhost-addoidc-client",
            ClientSecret = "secret",
            Scope = $"{DomainConstants.DomainPrefix}park {DomainConstants.DomainPrefix}bonus",
        });

        if (response.IsError)
        {
            foreach (var header in response.HttpResponse!.Headers)
            {
                _logger.LogWarning("{Header}: {Value}", header.Key, header.Value);
            }

            return null;
        }
        return response.AccessToken;
    }

    private async Task<string?> GetAccessToken()
    {
        var client = new HttpClient();
        var response = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = $"{DomainConstants.IdpAuthority}/connect/token",
            ClientId = "localhost-addoidc-client",
            ClientSecret = "secret",
            Scope = $"{DomainConstants.DomainPrefix}member",
        });

        return response.IsError ? null : response.AccessToken;
    }
}