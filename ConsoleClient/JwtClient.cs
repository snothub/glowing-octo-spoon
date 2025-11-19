using System.Security.Cryptography;
using System.Text.Json;
using IdentityService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ConsoleClient;

public class JwtClient : BackgroundService
{
    private readonly ILogger<JwtClient> _logger;
    private readonly ClientRunner _runner;

    public JwtClient(ILogger<JwtClient> logger, ClientRunner runner)
    {
        _logger = logger;
        _runner = runner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CreateJwt();
        Console.ReadLine();
        Console.Clear();
        
        await Task.Delay(2000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("JwtClient running at: {time}", DateTimeOffset.Now);

            var token = await _runner.GetToken();
            _logger.LogInformation(token);
        
            var resp = await _runner.CallServiceAsync(token);
            _logger.LogInformation(resp);

            await Task.Delay(2000, stoppingToken);
        }
    }
    private static void CreateJwt()
    {
        string publicJwkJson;
            
        // Generate new RSA key pair
        using (var rsa = new RSACryptoServiceProvider(2048))
        {
            var rsaKey = new RsaSecurityKey(RSA.Create(2048));
            var jsonWebKey = JsonWebKeyConverter.ConvertFromSecurityKey(rsaKey);
            jsonWebKey.Alg = "PS256";
            DomainConstants.SigningJwk = JsonSerializer.Serialize(jsonWebKey);
            
            var parameters = rsa.ExportParameters(false); // false = public key only
            var publicJwk = new JsonWebKey()
            {
                Kty = "RSA",
                E = Base64UrlEncoder.Encode(parameters.Exponent),
                N = Base64UrlEncoder.Encode(parameters.Modulus),
                Kid = Guid.NewGuid().ToString(),
                Alg = "RS256",
                Use = "sig"
            };

            publicJwkJson = JsonSerializer.Serialize(publicJwk, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            
            parameters = rsa.ExportParameters(true);
            DomainConstants.ClientJwk = new JsonWebKey()
            {
                Kty = "RSA",
                E = Base64UrlEncoder.Encode(parameters.Exponent),
                N = Base64UrlEncoder.Encode(parameters.Modulus),
                D = Base64UrlEncoder.Encode(parameters.D),
                P = Base64UrlEncoder.Encode(parameters.P),
                Q = Base64UrlEncoder.Encode(parameters.Q),
                DP = Base64UrlEncoder.Encode(parameters.DP),
                DQ = Base64UrlEncoder.Encode(parameters.DQ),
                QI = Base64UrlEncoder.Encode(parameters.InverseQ),
                Kid = Guid.NewGuid().ToString(),
                Alg = "RS256",
                Use = "sig"
            };
        }

        Console.WriteLine(publicJwkJson);
    }
}