using Microsoft.IdentityModel.Tokens;

namespace IdentityService;

public static partial class DomainConstants
{
    public const string DomainPrefix = "agva";
    public const string IdsrvDefaultAuthenticationScheme = "dockerauth";
    public const string AdAuthenticationScheme = "ad3";
    public const string ExternalScheme = "corp";
   
    public const string KeyedService = "CustomStore";    

    public const string JwtClientId = "jwt.client.sample";    
    public static JsonWebKey ClientJwk { get; set; }
    
    public const string IdpAuthority = "http://localhost:7000";
    public static string SigningJwk { get; set; }
}