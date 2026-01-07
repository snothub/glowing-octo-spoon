using System.Net.Security;
using Serilog;
using Yarp.ReverseProxy.Forwarder;

namespace ClientApplication;

/// <summary>
/// Custom YARP forwarder HTTP client factory that creates HTTP clients with SSL certificate validation disabled.
/// WARNING: Only for development purposes!
/// </summary>
public class CustomForwarderHttpClientFactory : IForwarderHttpClientFactory
{
    public HttpMessageInvoker CreateClient(ForwarderHttpClientContext context)
    {
        Log.Logger.ForContext("SourceContext", "CustomForwarderHttpClientFactory")
            .Information("Creating HTTP client with SSL validation disabled for cluster: {ClusterId}", context.ClusterId);

        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.None,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            SslOptions = new SslClientAuthenticationOptions
            {
                // Bypass SSL certificate validation for development
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    if (errors != System.Net.Security.SslPolicyErrors.None)
                    {
                        Log.Logger.ForContext("SourceContext", "CustomForwarderHttpClientFactory")
                            .Warning("Bypassing SSL errors: {Errors} for certificate: {Subject}",
                                errors, certificate?.Subject ?? "null");
                    }
                    return true;
                }
            }
        };

        return new HttpMessageInvoker(handler, disposeHandler: true);
    }
}
