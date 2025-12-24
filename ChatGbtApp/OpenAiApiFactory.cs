using System.ComponentModel.Design;
using ChatGbtApp.Interfaces;
using ChatGbtApp.Services;
using Microsoft.Extensions.Logging;

namespace ChatGbtApp;

public class OpenAiApiFactory(ILogger<OpenAiApi> logger)
{
    public IOpenAiApi Create(string? apiKey = null, string? apiUrl = null)
    {
        apiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                   ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set");
        
        var httpClient = new HttpClient();
        var responseParser = new OpenAiResponseParser();
        
        
        return new OpenAiApi(httpClient, responseParser, logger, apiKey,  "https://api.openai.com/v1/responses");
    }
}
