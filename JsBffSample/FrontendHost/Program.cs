// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Claims;
using ClientApplication;
using Duende.Bff.Yarp;
using IdentityService;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddBff()
    .AddRemoteApis();

// Configure YARP to bypass SSL certificate validation (DEV ONLY)
var forwarderFactoryDescriptor = builder.Services.FirstOrDefault(d =>
    d.ServiceType == typeof(Yarp.ReverseProxy.Forwarder.IForwarderHttpClientFactory));
if (forwarderFactoryDescriptor != null)
{
    builder.Services.Remove(forwarderFactoryDescriptor);
}
builder.Services.AddSingleton<Yarp.ReverseProxy.Forwarder.IForwarderHttpClientFactory>(
    new CustomForwarderHttpClientFactory());

// registers HTTP client that uses the managed user access token
builder.Services.AddUserAccessTokenHttpClient("api_client", configureClient: client =>
{
    client.BaseAddress = new Uri("https://api.local:6002/");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "cookie";
    options.DefaultChallengeScheme = "oidc";
    options.DefaultSignOutScheme = "oidc";
})
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "__Host-bff";
        options.Cookie.SameSite = SameSiteMode.Strict;
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = DomainConstants.IdpAuthority;
        options.RequireHttpsMetadata = DomainConstants.IdpAuthority.StartsWith("https://");
        options.BackchannelHttpHandler = new BackChannelListener();

        options.ClientId = "interactive.confidential";
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.ResponseMode = "query";

        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.SaveTokens = true;
        options.DisableTelemetry = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("api");
        options.Scope.Add("offline_access");

        options.TokenValidationParameters = new()
        {
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

app.MapGet("/local/identity", LocalIdentityHandler)
    .AsBffApiEndpoint();

// if you want the TODOs API local
// app.MapControllers()
//     .RequireAuthorization()
//     .AsBffApiEndpoint();

// if you want the TODOs API remote
app.MapRemoteBffApiEndpoint("/todos", "https://api.local:6002/todos")
    .RequireAccessToken(Duende.Bff.TokenType.User);

app.Run();

[Authorize]
static IResult LocalIdentityHandler(ClaimsPrincipal user)
{
    var name = user.FindFirst("name")?.Value ?? user.FindFirst("sub")?.Value;
    return Results.Json(new { message = "Local API Success!", user = name });
}