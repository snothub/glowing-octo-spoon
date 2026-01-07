using System.Text.Json;
using ClientApplication;
using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Configuration;
using Duende.Bff.DynamicFrontends;
using Duende.Bff.Yarp;
using IdentityService;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Logging;
using Serilog;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.Title = "Client Application";

        IdentityModelEventSource.ShowPII = true;

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, lc) => lc
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(ctx.Configuration));

        builder.Services.AddControllersWithViews();

        builder.Services
            .AddBff()
            .AddServerSideSessions()
            .ConfigureOpenIdConnect(options =>
            {
                options.Authority = DomainConstants.IdpAuthority;
                options.ResponseType = "code";
                options.ResponseMode = "query";

                options.RequireHttpsMetadata = DomainConstants.IdpAuthority.StartsWith("https://");
                options.BackchannelHttpHandler = new BackChannelListener();
            })
            .AddFrontends(
                new BffFrontend(BffFrontendName.Parse("interactive"))
                    .WithOpenIdConnectOptions(options =>
                    {
                        options.ClientId = "interactive";
                        options.ClientSecret = DomainConstants.InteractiveClientSecret;
                    })
                    .MapToPath("/frontend1"),
                new BffFrontend(BffFrontendName.Parse("oidc-client"))
                    .WithOpenIdConnectOptions(options =>
                    {
                        options.SaveTokens = true;
                        options.ClientId = "localhost-addoidc-client";
                        options.ClientSecret = "secret";
                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("scope1");
                        options.Scope.Add("offline_access");
                        
                    })
                    .MapToPath("/frontend3")
                    .WithRemoteApis(
                        new RemoteApi("/api", new Uri(DomainConstants.ApiRootUri))
                            .WithAccessToken(RequiredTokenType.User),
                        new RemoteApi("/aud", new Uri(DomainConstants.ApiRootUri))
                            .WithUserAccessTokenParameters(new BffUserAccessTokenParameters { Resource = Resource.Parse("urn:isolated-api") }))
                ,
                new BffFrontend(BffFrontendName.Parse("extra-client"))
                    .WithOpenIdConnectOptions(x => { x.ClientId = "extra-client"; })
                    .MapToPath("/frontend2")
            )
            .AddRemoteApis()
            ;

        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseDeveloperExceptionPage();

        app.UseSerilogRequestLogging();

        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseRouting();

        app.UseBff();

        app.UseAuthorization();
        
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }

    private static Task ParseRequest(RedirectContext context)
    {
        var authority = Environment.GetEnvironmentVariable("IDP_AUTHORITY") ?? DomainConstants.IdpAuthority;

        Log.Information("context.Properties: {Serialize}",
            JsonSerializer.Serialize(context.Properties));

        if (context.Properties.Items.TryGetValue("idp", out var idp))
        {
            context.ProtocolMessage.AcrValues = $"idp:{idp}";
        }

        if (context.Properties.Items.TryGetValue("client_id", out var clientId))
        {
            context.ProtocolMessage.ClientId = clientId;
        }

        context.ProtocolMessage.IssuerAddress = $"{authority}/connect/authorize";
        return Task.CompletedTask;
    }
}