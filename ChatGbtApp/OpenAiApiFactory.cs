using ChatGbtApp.Interfaces;
using ChatGbtApp.Services;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace ChatGbtApp;

public class OpenAiApiFactory
{
    private readonly ILogger<OpenAiApi> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IResponseParser _responseParser;

    public OpenAiApiFactory(
        ILogger<OpenAiApi> logger,
        IHttpClientFactory httpClientFactory,
        IResponseParser responseParser)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _responseParser = responseParser;
    }

    public IOpenAiApi Create(string? apiKey = null, string? apiUrl = null)
    {
        apiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                   ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set");

        apiUrl ??= "https://api.openai.com/v1/responses";

        var httpClient = _httpClientFactory.CreateClient("OpenAI");

        return new OpenAiApi(httpClient, _responseParser, _logger, apiKey, apiUrl);
    }
}
