using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace ClientApplication;

public class BackChannelListener : DelegatingHandler
{
    public BackChannelListener() : base(new HttpClientHandler())
    {
        // Find the innermost handler (which is responsible for the actual network request)
        HttpMessageHandler innerMostHandler = InnerHandler;
        while (innerMostHandler is DelegatingHandler delegating)
        {
            innerMostHandler = delegating.InnerHandler;
        }

        if (innerMostHandler is HttpClientHandler httpClientHandler)
        {
            httpClientHandler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        else if (innerMostHandler is SocketsHttpHandler socketsHttpHandler)
        {
            socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback =
                (_, _, _, _) => true;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        
        var s = JsonSerializer.Serialize(request);
        Log.Logger.ForContext("SourceContext", "BackChannelListener")
            .Information(s);
        
        var sw = new Stopwatch();
        sw.Start();

        var response = await base.SendAsync(request, cancellationToken);

        sw.Stop();

        // Read the response content (make sure to await it)
        var responseContent = await response.Content.ReadAsStringAsync();

        // HACK: log the response body; Never run this in production
        Log.Logger.ForContext("SourceContext", "BackChannelListener")
        .Information("#####################################");
        Log.Logger.ForContext("SourceContext", "BackChannelListener")
        .Information(responseContent);
        Log.Logger.ForContext("SourceContext", "BackChannelListener")
        .Information("#####################################");

        Log.Logger.ForContext("SourceContext", "BackChannelListener")
                   .Information($"### BackChannel request to {request?.RequestUri?.AbsoluteUri} took {sw.ElapsedMilliseconds.ToString()} ms");

        return response;
    }
}