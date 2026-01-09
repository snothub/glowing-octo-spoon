// Copyright (c) Duende Software. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IdentityModel.Tokens.Jwt;
using IdentityService;
using Microsoft.IdentityModel.JsonWebTokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddAuthentication("token")
    .AddJwtBearer("token", options =>
    {
        options.Authority = DomainConstants.IdpAuthority;
        options.TokenValidationParameters.ValidIssuer = DomainConstants.IdpAuthority;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.SignatureValidator = (token, parameters) =>
        {
            Log.Information("Validating token {Token}", token);
            return new JsonWebToken(token);
        };
        
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiCaller", policy =>
    {
        policy.RequireClaim("scope", "api1");
    });

    options.AddPolicy("RequireInteractiveUser", policy =>
    {
        policy.RequireClaim("sub");
    });
});

var app = builder.Build();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireAuthorization("ApiCaller");

app.Run();
