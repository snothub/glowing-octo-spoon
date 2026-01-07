using Microsoft.IdentityModel.Tokens;

namespace IdentityService;

///
/// Domain-wide constants
/// 
/// Can be extended in partial classes for different environments, e.g.
/// DomainConstants.Local.cs
/*
namespace IdentityService;

public  static partial class DomainConstants
{
    public const string Cid = "external-client-id";
    public const string Cs = "some-client-secret";
    public const string CAuth = "https://externaldomain.onmicrosoft.com/v2.0";   
}
*/

public static partial class DomainConstants
{
    public const string DomainPrefix = "agva";
    public const string IdsrvDefaultAuthenticationScheme = "dockerauth";
    public const string AdAuthenticationScheme = "ad3";
    public const string ExternalScheme = "corp";
   
    public const string KeyedService = "CustomStore";    

    public const string JwtClientId = "jwt.client.sample";    
    public static JsonWebKey ClientJwk { get; set; }
    
    public const string IdpAuthority = "https://idp.local:7001";
    public const string ApiRootUri = "https://api.local:7701";
    public static string SigningJwk { get; set; }
}