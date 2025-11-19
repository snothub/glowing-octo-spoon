using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

public class TokenExchangeGrantValidator : IExtensionGrantValidator
{
    private readonly ITokenValidator _validator;

    public TokenExchangeGrantValidator(ITokenValidator validator)
    {
        _validator = validator;
    }

    public string GrantType => OidcConstants.GrantTypes.TokenExchange;

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest);

        var token = context.Request.Raw.Get(OidcConstants.TokenRequest.SubjectToken);

        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        // validate the incoming access token with the built-in token validator
        var validationResult = await _validator.ValidateAccessTokenAsync(token);
        if (validationResult.IsError)
        {
            return;
        }
        
        context.Result = new GrantValidationResult();
    }
}