using System.Net;

namespace CachingProxy.Proxy;
/// <summary>
/// A simple HTTP proxy that listens for incoming HTTP requests and forwards them to a specified origin.
/// </summary>
public class Proxy
{
  private const string PREFIX = "http://localhost";
  private readonly string _origin;
  private static readonly HttpClient _httpClient = new();
  private readonly HttpListener _httpListener;

  /// <summary>
  /// Initializes a new instance of the <see cref="Proxy"/> class.
  /// </summary>
  /// <param name="port">The port on which the proxy will listen.</param>
  /// <param name="origin">The target origin to which requests will be forwarded.</param>
  public Proxy(int port, string origin)
  {
    _httpListener = new HttpListener();
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

    var forwardedRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), targetUrl);

    CopyHeadersFrom(request, forwardedRequest);

    // using HttpResponseMessage response = await _httpClient.SendAsync(forwardedRequest, token);
    // response.EnsureSuccessStatusCode();
    // string responseBody = await response.Content.ReadAsStringAsync();

    try
    {
      string responseBody = await _httpClient.GetStringAsync(new Uri(targetUrl), token);
      Console.WriteLine(responseBody);
    }
    catch (HttpRequestException e)
    {
      Console.WriteLine("\n An error occured during the request forwarding");
      Console.WriteLine("Message: {0} ", e.Message);
    }


  }

  /// <summary>
  /// Copies headers from the original HTTP request to the forwarded request.
  /// </summary>
  /// <param name="fromRequest">The original incoming HTTP request.</param>
  /// <param name="toForwardedRequest">The HTTP request to be forwarded.</param>
  private static void CopyHeadersFrom(HttpListenerRequest fromRequest, HttpRequestMessage toForwardedRequest)
  {
    foreach (string header in fromRequest.Headers)
    {
      if (!WebHeaderCollection.IsRestricted(header)) // Avoid restricted headers
      {
        toForwardedRequest.Headers.TryAddWithoutValidation(header, fromRequest.Headers[header]);
      }
    }
  }
}