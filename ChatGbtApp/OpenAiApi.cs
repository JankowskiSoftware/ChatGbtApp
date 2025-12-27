using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ChatGbtApp.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace ChatGbtApp;

public class OpenAiApi : IOpenAiApi
{
    private readonly HttpClient _httpClient;
    private readonly IResponseParser _responseParser;
    private readonly string _apiKey;
    private readonly string _apiUrl;
    private readonly ILogger? _logger;
    private readonly RetryPolicy _policy;

    public OpenAiApi(HttpClient httpClient, IResponseParser responseParser, ILogger<OpenAiApi> logger, string apiKey, string apiUrl = "https://api.openai.com/v1/responses")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _apiUrl = apiUrl ?? throw new ArgumentNullException(nameof(apiUrl));
        _logger = logger;
        
        _httpClient.Timeout = TimeSpan.FromMinutes(3);
        
        _policy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1))
            );
    }

    public async Task<string> AskAsync(string input, string model)
    {
        var request = CreateRequest(input, model);
        
        string respText = "";
        await _policy.Execute(async () =>
        {
            using var resp = await _httpClient.SendAsync(request);
            resp.EnsureSuccessStatusCode();
            respText = await resp.Content.ReadAsStringAsync(); 
        });
        

        try
        {
            using var doc = JsonDocument.Parse(respText);
            if (_responseParser.TryExtractText(doc.RootElement, out var parsedText))
                return parsedText;
        }
        catch (JsonException ex)
        {
            _logger?.LogError("Failed to parse JSON response: {ResponseText}", respText);
            _logger?.LogError("Failed to parse JSON response: {ex}", ex);
        }

        return respText;
    }

    private HttpRequestMessage CreateRequest(string input, string model)
    {
        var payload = new { model, input };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        return request;
    }
}