using System.Net;
using System.Text;

public class Proxy
{
  private const string PREFIX = "http://localhost";
  private readonly string _origin;
  private static readonly HttpClient _httpClient = new();
  private readonly HttpListener _httpListener;

  public Proxy(int port, string origin)
  {
    _httpListener = new HttpListener();
    _httpListener.Prefixes.Add($"{PREFIX}:{port}/");
    _origin = origin;
  }

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

    // foreach (string header in request.Headers)
    // {
    //     if (!WebHeaderCollection.IsRestricted(header)) // Avoid restricted headers
    //     {
    //         forwardedRequest.Headers.TryAddWithoutValidation(header, request.Headers[header]);
    //     }
    // }

    // // Copy body if it's a POST/PUT request
    // if (request.HasEntityBody)
    // {
    //     using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
    //     string body = await reader.ReadToEndAsync();
    //     forwardedRequest.Content = new StringContent(body, Encoding.UTF8, request.ContentType);
    // }

    // // Send request and get response
    // using HttpResponseMessage response = await httpClient.SendAsync(forwardedRequest);

    // // Copy response to client
    // context.Response.StatusCode = (int)response.StatusCode;
    // foreach (var header in response.Headers)
    // {
    //     context.Response.Headers[header.Key] = string.Join(", ", header.Value);
    // }

    // // Copy response body
    // byte[] responseBody = await response.Content.ReadAsByteArrayAsync();
    // context.Response.OutputStream.Write(responseBody, 0, responseBody.Length);
    // context.Response.OutputStream.Close();

  }

  private static void CopyHeadersFrom(HttpListenerRequest fromRequest, HttpRequestMessage ToForwardedRequest)
  {
    foreach (string header in fromRequest.Headers)
    {
      if (!WebHeaderCollection.IsRestricted(header)) // Avoid restricted headers
      {
        ToForwardedRequest.Headers.TryAddWithoutValidation(header, fromRequest.Headers[header]);
      }
    }
  }
}