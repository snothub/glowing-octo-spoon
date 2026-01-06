using Duende.Bff.AccessTokenManagement;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using IdentityService;

internal class TrumfAccessTokenRetriever(IAccessTokenRetriever inner) : IAccessTokenRetriever
{
    public async Task<AccessTokenResult> GetAccessTokenAsync(AccessTokenRetrievalContext context, CancellationToken ct = default)
    {
        var p = context.UserTokenRequestParameters;
        var result = await inner.GetAccessTokenAsync(context, ct);

        if (result is BearerTokenResult bearerToken)
        {
            var client = new HttpClient();
            var exchangeResponse = await client.RequestTokenExchangeTokenAsync(new TokenExchangeTokenRequest
            {
                Address = $"{DomainConstants.IdpAuthority}/connect/token",
                GrantType = OidcConstants.GrantTypes.TokenExchange,

                ClientId = "bff",
                ClientSecret = "secret",

                SubjectToken = bearerToken.AccessToken,
                SubjectTokenType = OidcConstants.TokenTypeIdentifiers.AccessToken
            }, cancellationToken: ct);
            if (exchangeResponse.AccessToken is null)
            {
                return new AccessTokenRetrievalError
                {
                    Error = "Token exchanged failed. Access token is null"
                };
            }

            if (exchangeResponse.IsError)
            {
                return new AccessTokenRetrievalError
                {
                    Error = exchangeResponse.Error ?? "Failed to get access token",
                    ErrorDescription = exchangeResponse.ErrorDescription
                };
            }

            return new BearerTokenResult
            {
                AccessToken = AccessToken.Parse(exchangeResponse.AccessToken)
            };
        }

        return result;
    }
}
