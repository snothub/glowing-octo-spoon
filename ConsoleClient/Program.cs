using IdentityService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleClient;

class Program
{
    static async Task Main(string[] args)
    {

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddDistributedMemoryCache();
                // Register your services here
                // ConfigureJwtClient(services);
                // services.AddHostedService<DPoPClient>();

                services.AddHostedService<TokenExClient>();
            })
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureJwtClient(IServiceCollection services)
    {
        services.AddTransient<ClientRunner>();
        services.AddClientCredentialsTokenManagement()
            .AddClient(DomainConstants.JwtClientId, client =>
            {
                client.ClientId = DomainConstants.JwtClientId;
                client.TokenEndpoint = "http://localhost:7000/connect/token";
                client.DPoPJsonWebKey = DomainConstants.SigningJwk;
                client.Scope = $"{DomainConstants.DomainPrefix}member";
            })
            ;

        services.AddClientCredentialsHttpClient(DomainConstants.JwtClientId, DomainConstants.JwtClientId, client =>
        {
            client.BaseAddress = new Uri("http://localhost:7700");
        });
        services.AddHostedService<JwtClient>();
    }
}