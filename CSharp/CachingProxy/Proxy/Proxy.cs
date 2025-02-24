using System.Net;
using System.Net.Http;
using System.Text;
using caching_proxy.Cache.Models;
using CachingProxy.Caching;

namespace CachingProxy.Proxy;
/// <summary>
/// A simple HTTP proxy that listens for incoming HTTP requests and forwards them to a specified origin.
/// </summary>
public class Proxy
{
    private const string PREFIX = "http://localhost";
    private readonly string _origin;
    private static readonly HttpClient _httpClient = new();
    private readonly HttpListener _httpListener = new();
    private readonly Cache _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Proxy"/> class.
    /// </summary>
    /// <param name="port">The port on which the proxy will listen.</param>
    /// <param name="origin">The target origin to which requests will be forwarded.</param>
    public Proxy(int port, string origin)
    {
        _httpListener.Prefixes.Add($"{PREFIX}:{port}/");
        _origin = origin;
    }

    /// <summary>
    /// Starts the proxy server and begins listening for incoming HTTP requests.
    /// </summary>
    /// <param name="token">A cancellation token to stop the proxy server.</param>
    public async Task Start(CancellationToken token = default)
    {
        try
        {
            _httpListener.Start();
            while (!token.IsCancellationRequested)
            {
                Console.WriteLine("Listening...");
                var httpContext = await _httpListener.GetContextAsync();
                Console.WriteLine("Request received.");
                _ = HandleClient(httpContext, token);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occured. \n {e.Message}");
        }
        finally
        {
            _httpListener.Stop();
        }
    }

    /// <summary>
    /// Handles an incoming HTTP request and forwards it to the target origin.
    /// </summary>
    /// <param name="context">The HTTP request context.</param>
    /// <param name="token">A cancellation token to cancel the request.</param>
    private async Task HandleClient(HttpListenerContext context, CancellationToken token)
    {
        var request = context.Request;
        string targetUrl = $"{_origin}{request.Url.PathAndQuery}";

        if (_cache.HasKey(targetUrl))
        {
            Console.WriteLine("X-cache: HIT");
            context.Response.Headers["X-cache"] = "HIT";
            var item = _cache.GetValue(targetUrl);

            MapMetadataToContext(context, item.StatusCode, item.ContentType, item.Headers);
            await WriteResponse(context, item.Content, token);

            return;
        }

        try
        {
            Console.WriteLine("X-cache: MISS");
            context.Response.Headers["X-cache"] = "MISS";
            var forwardedRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), targetUrl);
            using var forwardResponse = await _httpClient.SendAsync(forwardedRequest, token);
            string responseBody = await forwardResponse.Content.ReadAsStringAsync(token);

            await UpdateCache(targetUrl, forwardResponse);

            MapMetadataToContext(
                context,
                (int)forwardResponse.StatusCode,
                forwardResponse.Content.Headers.ContentType.MediaType,
                [.. forwardResponse.Headers]
                );

            await WriteResponse(context, responseBody, token);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\n An error occured during the request forwarding");
            Console.WriteLine("Message: {0} ", e.Message);
        }
    }

    private async Task WriteResponse(HttpListenerContext context, string responseBody, CancellationToken token)
    {

        using var streamWriter = new StreamWriter(context.Response.OutputStream);
        await streamWriter.WriteAsync(responseBody);
    }

    private static void MapMetadataToContext(HttpListenerContext context, int statusCode, string contentType, List<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = contentType;
        foreach (var header in headers)
        {
            foreach (var value in header.Value)
            {
                context.Response.AppendHeader(header.Key, value);
            }
        }
    }

    private async Task UpdateCache(string url, HttpResponseMessage forwardResponse)
    {
        var cachedItem = new CachedItem
        {
            Content = await forwardResponse.Content.ReadAsStringAsync(),
            StatusCode = (int)forwardResponse.StatusCode,
            ContentType = forwardResponse.Content.Headers.ContentType.MediaType
        };

        foreach (var header in forwardResponse.Headers)
        {
            cachedItem.Headers.Add(header);
        }
        _cache.AddEntry(url, cachedItem);
        Console.WriteLine($"{url}: cache updated!");
    }
}