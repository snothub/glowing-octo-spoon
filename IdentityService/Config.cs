using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityService;

public static class Config
{ 
    public static IEnumerable<IdentityResource> IdentityResources
    {
        get
        {
            return new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email
                {
                    Emphasize = false,
                    Required = true
                },
                new IdentityResources.Phone(),
                new IdentityResource()
                {
                    Name = "employee_info",
                    DisplayName = "Employee information",
                    Description = "Employee information including seniority and status...",
                    Emphasize = false,
                    Enabled = true,
                    Required = false,
                    ShowInDiscoveryDocument = true,
                    UserClaims = new List<string>
                    {
                        "employment_start",
                        "seniority",
                        "email",
                        "contractor",
                        "employee",
                        "role",
                    }
                },
                new IdentityResource()
                {
                    Name = DomainConstants.DomainPrefix,
                    DisplayName = $"{DomainConstants.DomainPrefix} information",
                    Description = $"{DomainConstants.DomainPrefix} information...",
                    Emphasize = true,
                    Enabled = true,
                    Required = false,
                    ShowInDiscoveryDocument = true,
                    UserClaims = new List<string>
                    {
                        "seniority",
                        "email",
                        "employee",
                    }
                }
            };
        }
    }

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("scope1"),
            new ApiScope("scope2"),
            new ApiScope($"{DomainConstants.DomainPrefix}park") {Description = $"{DomainConstants.DomainPrefix} parking"},
            new ApiScope($"{DomainConstants.DomainPrefix}bonus") {Description = $"{DomainConstants.DomainPrefix} bonus"},
            new ApiScope($"{DomainConstants.DomainPrefix}member") {Description = $"{DomainConstants.DomainPrefix} member", Emphasize = true},
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // m2m client credentials flow client
            new Client
            {
                ClientId = "m2m.client",
                ClientName = "Client Credentials Client",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()) },

                AllowedScopes = { "scope1" }
            },

            // interactive client using code flow + pkce
            new Client
            {
                ClientId = "interactive",
                RequireConsent = true,
                ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,

                RedirectUris = { "https://localhost:44300/signin-oidc" },
                FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "scope2" }
            },
            
            //Client application for the Introduction to OpenID Connect and Oauth (1 day) course
            //Authorization code flow + Refresh token,
            //same as oidc-client, but with 15 second access token lifetime
            //Used for AddOpenIdConnect authentication for the localhost domain
            new Client
            {
                ClientId = "localhost-addoidc-client",
                ClientName = "OIDC AddOpenIDConnect localtest.me demo client",

                RedirectUris = new List<string>()
                {
                    "https://localhost/signin-oidc",
                    "https://localhost:5001/signin-oidc",
                    "http://localhost/signin-oidc",
                    "http://localhost:5000/signin-oidc"
                },

                PostLogoutRedirectUris = new List<string>()
                {
                    "https://localhost/signout-callback-oidc",
                    "https://localhost:5001/signout-callback-oidc"
                },

                ClientSecrets = { new Secret("mysecret".Sha256()) },
                RequireConsent = true,
                AllowRememberConsent=true,
                AllowedGrantTypes = GrantTypes.CodeAndClientCredentials,
                AlwaysIncludeUserClaimsInIdToken = true,

                RequirePkce = false,
                AllowedScopes =
                {
                    //Standard scopes
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Phone,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    "employee_info",
                    $"{DomainConstants.DomainPrefix}",
                    $"{DomainConstants.DomainPrefix}park",
                    $"{DomainConstants.DomainPrefix}member",
                    $"{DomainConstants.DomainPrefix}bonus",
                    "api"
                },

                AllowOfflineAccess = true,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,


                AccessTokenLifetime = 3600,      //1 hour
                AlwaysSendClientClaims = true

            }
        };
}