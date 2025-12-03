using System.Security.Cryptography;
using System.Text.Json;
using ClientApplication;
using IdentityModel;
using IdentityService;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;


internal class Program
{
    public static void Main(string[] args)
    {
        Console.Title = "Client Application";

        IdentityModelEventSource.ShowPII = true;
        var authority = Environment.GetEnvironmentVariable("IDP_AUTHORITY") ?? "http://localhost:7000";

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, lc) => lc
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(ctx.Configuration));

        builder.Services.AddControllersWithViews();

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = DomainConstants.IdsrvDefaultAuthenticationScheme;
        }).AddCookie(opt =>
        {
            opt.LogoutPath = "/user/Logout";
            opt.AccessDeniedPath = "/user/AccessDenied";
            opt.SlidingExpiration = true;

        }).AddOpenIdConnect(DomainConstants.IdsrvDefaultAuthenticationScheme, options =>
        {
            options.Authority = authority;
            options.MetadataAddress = "http://identity/.well-known/openid-configuration";

            options.ClientId = "localhost-addoidc-client";

            options.ClientSecret = "secret";
            options.ResponseType = "code";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("employee_info");
            // options.Scope.Add(DomainConstants.DomainPrefix);
            // options.Scope.Add($"{Constants.DomainPrefix}bonus");
            // options.Scope.Add($"{Constants.DomainPrefix}park");
            // options.Scope.Add($"{Constants.DomainPrefix}member");
            // options.Scope.Add("offline_access");

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;
            options.RequireHttpsMetadata = false;

            options.AccessDeniedPath = "/User/AccessDenied";

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role,
                AudienceValidator = (audiences, securityToken, validationParameters) =>
                {
                    // Accept the configured client_id OR any dynamic client_id
                    // In production, you may want to validate against a whitelist
                    var validAudiences = new[] { "localhost-addoidc-client", "extra-client" };
                    return audiences.Any(aud => validAudiences.Contains(aud));
                }
            };

            options.Events.OnRedirectToIdentityProvider = context =>
            {
                Log.Information("context.Properties: {Serialize}", JsonSerializer.Serialize(context.Properties));

                if (context.Properties.Items.TryGetValue("idp", out var idp))
                {
                    context.ProtocolMessage.AcrValues = $"idp:{idp}";
                }

                if (context.Properties.Items.TryGetValue("client_id", out var clientId))
                {
                    context.ProtocolMessage.ClientId = clientId;
                }

                context.ProtocolMessage.IssuerAddress =  $"{authority}/connect/authorize";
                return Task.CompletedTask;
            };

            options.Events.OnAuthorizationCodeReceived = context =>
            {
                Log.Information("OnAuthorizationCodeReceived - Properties: {Serialize}", JsonSerializer.Serialize(context.Properties));

                // Override token request client credentials if dynamic client_id was used
                if (context.Properties.Items.TryGetValue("client_id", out var clientId))
                {
                    context.TokenEndpointRequest.ClientId = clientId;

                    // If you need different client secrets per client, retrieve it here
                    // For now, using the same secret - you may need to configure this differently
                    context.TokenEndpointRequest.ClientSecret = context.Options.ClientSecret;
                }

                return Task.CompletedTask;
            };

            options.Events.OnRedirectToIdentityProviderForSignOut = context =>
            {
                context.ProtocolMessage.IssuerAddress = $"{authority}/connect/endsession";
                return Task.CompletedTask;
            };


            options.BackchannelHttpHandler = new BackChannelListener();
            options.BackchannelTimeout = TimeSpan.FromSeconds(30);
        });

        var app = builder.Build();

        app.UseDeveloperExceptionPage();

        app.UseSerilogRequestLogging();

        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }

}
