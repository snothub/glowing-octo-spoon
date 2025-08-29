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

            options.ClientSecret = "mysecret";
            options.ResponseType = "code";

            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("employee_info");
            options.Scope.Add(DomainConstants.DomainPrefix);
            // options.Scope.Add($"{Constants.DomainPrefix}bonus");
            // options.Scope.Add($"{Constants.DomainPrefix}park");
            // options.Scope.Add($"{Constants.DomainPrefix}member");
            options.Scope.Add("offline_access");

            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;
            options.RequireHttpsMetadata = false;

            options.AccessDeniedPath = "/User/AccessDenied";

            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = JwtClaimTypes.Name,
                RoleClaimType = JwtClaimTypes.Role
            };

            options.Events.OnRedirectToIdentityProvider = context =>
            {
                Log.Information("context.Properties: {Serialize}", JsonSerializer.Serialize(context.Properties));
                if (context.Properties.Items.TryGetValue("idp", out var idp))
                {
                    context.ProtocolMessage.AcrValues = $"idp:{idp}";
                }
                context.ProtocolMessage.IssuerAddress =  $"{authority}/connect/authorize";
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
