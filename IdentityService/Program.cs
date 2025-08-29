using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using IdentityService;
using IdentityService.Pages;
using Microsoft.IdentityModel.Tokens;
using Serilog;


Console.Title = "IdentityService";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting up");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .Enrich.FromLogContext()
    .ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddRazorPages();

var isBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Authentication.CookieAuthenticationScheme = DomainConstants.IdsrvDefaultAuthenticationScheme;
    options.IssuerUri = "https://identity";

    options.UserInteraction.ConsentUrl = Environment.GetEnvironmentVariable("IDP_CONSENTURL") ?? "http://localhost:8001/consent";
    options.UserInteraction.LoginUrl = Environment.GetEnvironmentVariable("IDP_LOGINURL") ?? "/ui/login";
    options.UserInteraction.LogoutUrl = Environment.GetEnvironmentVariable("IDP_LOGOUTURL") ?? "/ui/logout";
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
    options.EmitStaticAudienceClaim = true;
    options.KeyManagement.Enabled = true;
    options.KeyManagement.PropagationTime = TimeSpan.FromDays(7);
    options.KeyManagement.RotationInterval = TimeSpan.FromDays(90);
    options.KeyManagement.RetentionDuration = TimeSpan.FromDays(7);
    options.KeyManagement.DeleteRetiredKeys = true;
    options.KeyManagement.DataProtectKeys = false;

})
.AddTestUsers(TestUsers.Users) 
    ;

isBuilder.Services.AddTransient<IReturnUrlParser, OidcReturnUrlParser>();
isBuilder.Services.AddTransient<ICustomAuthorizeRequestValidator, DomainCustomAuthorizeRequestValidator>();

// in-memory, code config
isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
isBuilder.AddInMemoryClients(Config.Clients);
isBuilder.Services.AddKeyedTransient<IResourceStore, DomainResourceStore>(DomainConstants.KeyedService);

builder.Services.AddAuthentication()
    .AddCookie(DomainConstants.IdsrvDefaultAuthenticationScheme, options => {
        options.SlidingExpiration = true;
    })
    .AddCookie(DomainConstants.ExternalScheme, options => {
        options.Cookie.Name = DomainConstants.ExternalScheme;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    })
    .AddOpenIdConnect(DomainConstants.AdAuthenticationScheme, "Microsoft Entra ID", options =>
    {
        options.SignInScheme = DomainConstants.ExternalScheme;
        options.Authority = DomainConstants.CAuth;
        options.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = false };
        options.ClientId = DomainConstants.Cid;
        options.ClientSecret = $"{DomainConstants.Cs}";
        options.ResponseType = "id_token";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("user.read");
        options.CallbackPath = new PathString("/aad-callback");
    });

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseSerilogRequestLogging();

app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

app.UseIdentityServer();
app.UseCors(p =>
{
    p.WithOrigins("http://localhost:3001");
    p.AllowAnyHeader();
    p.AllowCredentials();
});

app.UseAuthorization();

app.MapRazorPages()
    .RequireAuthorization();

app.Run();
