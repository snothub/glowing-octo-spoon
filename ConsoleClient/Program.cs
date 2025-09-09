using System.Security.Cryptography;
using System.Text.Json;
using IdentityService;
using Microsoft.IdentityModel.Tokens;

namespace ConsoleClient;

class Program
{
    static async Task Main(string[] args)
    {
        var cr = new ClientRunner();
        CreateJwt();
        
        Console.ReadLine();
        Console.Clear();
        
        var token = await cr.GetToken();
        Console.WriteLine(token);
        
        Console.ReadLine();
        Console.Clear();
        
        var resp = await cr.CallServiceAsync(token);
        Console.WriteLine(resp);
        
        Console.ReadLine();
        Console.Clear();
    }

    private static void CreateJwt()
    {
        string publicJwkJson;
            
        // Generate new RSA key pair
        using (var rsa = new RSACryptoServiceProvider(2048))
        {
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