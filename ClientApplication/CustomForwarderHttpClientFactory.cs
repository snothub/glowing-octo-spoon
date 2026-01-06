using System.Net.Security;
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
        var handler = new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = false,
            AutomaticDecompression = System.Net.DecompressionMethods.None,
            UseCookies = false,
            // ActivityHeadersPropagator = new System.Net.Http.Headers.DistributedContextPropagator(),
            ConnectTimeout = TimeSpan.FromSeconds(15),
            SslOptions = new SslClientAuthenticationOptions
            {
                // Bypass SSL certificate validation for development
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
            }
        };

        return new HttpMessageInvoker(handler, disposeHandler: true);
    }
}
