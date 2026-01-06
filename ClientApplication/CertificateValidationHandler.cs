using System.Net.Security;

namespace ClientApplication;

/// <summary>
/// HTTP handler that bypasses SSL certificate validation for development purposes.
/// WARNING: Never use this in production!
/// </summary>
public class CertificateValidationHandler : DelegatingHandler
{
    public CertificateValidationHandler()
    {
        InnerHandler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
            }
        };
    }

    public CertificateValidationHandler(HttpMessageHandler innerHandler) : base(innerHandler)
    {
        // Find the innermost handler
        HttpMessageHandler innerMostHandler = InnerHandler;
        while (innerMostHandler is DelegatingHandler delegating && delegating.InnerHandler != null)
        {
            innerMostHandler = delegating.InnerHandler;
        }

        // Configure the innermost handler to ignore certificate validation
        if (innerMostHandler is HttpClientHandler httpClientHandler)
        {
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        else if (innerMostHandler is SocketsHttpHandler socketsHttpHandler)
        {
            socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }
    }
}
