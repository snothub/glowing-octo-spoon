// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClientApplication;
using Duende.Bff.Yarp;
using IdentityService;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthorization();

builder.Services
    .AddBff()
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

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
        options.DefaultSignOutScheme = "oidc";
    })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = DomainConstants.IdpAuthority;
        options.ClientId = DomainConstants.ClientId;
        options.ClientSecret = "secret";
        options.ResponseType = "code";
        options.Scope.Add("api1");
        options.Scope.Add("offline_access");
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = DomainConstants.IdpAuthority.StartsWith("https://");
        options.BackchannelHttpHandler = new BackChannelListener();
    });

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseBff();

app.UseAuthorization();

app.MapBffManagementEndpoints();

// Uncomment this for Controller support
// app.MapControllers()
//     .AsBffApiEndpoint();

app.MapGet("/local/identity", LocalIdentityHandler)
    .AsBffApiEndpoint();

app.MapRemoteBffApiEndpoint("/remote", "https://api.local:6002")
    .RequireAccessToken(Duende.Bff.TokenType.User);

app.Run();

[Authorize]
static IResult LocalIdentityHandler(ClaimsPrincipal user, HttpContext context)
{
    var name = user.FindFirst("name")?.Value ?? user.FindFirst("sub")?.Value;
    return Results.Json(new { message = "Local API Success!", user = name });
}
