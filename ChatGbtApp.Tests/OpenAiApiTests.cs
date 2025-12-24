using System.Net;
using System.Text;
using System.Text.Json;
using ChatGbtApp.Interfaces;
using ChatGbtApp.Services;
using Moq;
using Moq.Protected;

namespace ChatGbtApp.Tests;

public class OpenAiApiTests
{
    private const string TestApiKey = "test-api-key-12345";

    [Fact]
    public async Task AskAsync_WithValidResponse_ReturnsExtractedText()
    {
        var api = CreateApi(@"{""output"": [{""content"": [{""text"": ""Hello, this is a test response.""}]}]}");
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Hello, this is a test response.", result);
    }

    [Fact]
    public async Task AskAsync_WithMultipleContentItems_ConcatenatesText()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""First part. ""}, {""text"": ""Second part. ""}, {""text"": ""Third part.""}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("First part. Second part. Third part.", result);
    }

    [Fact]
    public async Task AskAsync_WithMultipleOutputItems_ConcatenatesAllText()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Output 1. ""}]}, {""content"": [{""text"": ""Output 2.""}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Output 1. Output 2.", result);
    }

    [Fact]
    public async Task AskAsync_WithNestedContent_ExtractsNestedText()
    {
        var json = @"{""output"": [{""content"": [{""content"": [{""text"": ""Nested text 1. ""}, {""text"": ""Nested text 2.""}]}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Nested text 1. Nested text 2.", result);
    }

    [Fact]
    public async Task AskAsync_WithMixedContentTypes_OnlyExtractsTextFields()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Valid text. ""}, {""image"": ""base64data""}, {""text"": ""More valid text.""}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Valid text. More valid text.", result);
    }

    [Fact]
    public async Task AskAsync_WithOutputTextProperty_ReturnsOutputText()
    {
        var api = CreateApi(@"{""output_text"": ""Direct output text response.""}");
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Direct output text response.", result);
    }

    [Fact]
    public async Task AskAsync_WithEmptyOutput_ReturnsRawJson()
    {
        var json = @"{""status"": ""completed""}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal(json, result);
    }

    [Fact]
    public async Task AskAsync_WithInvalidJson_ReturnsRawResponse()
    {
        var text = "This is not valid JSON";
        var api = CreateApi(text);
        
        var result = await api.AskAsync("Test");
        
        Assert.Equal(text, result);
    }

    [Fact]
    public async Task AskAsync_WithCustomModel_UsesSpecifiedModel()
    {
        string? captured = null;
        var api = CreateApiWithCapture(@"{""output_text"": ""Response""}", c => captured = c);
        
        await api.AskAsync("Test", "gpt-5.2");
        
        Assert.Contains("\"model\":\"gpt-5.2\"", captured);
    }

    [Fact]
    public async Task AskAsync_WithDefaultModel_UsesGpt41Mini()
    {
        string? captured = null;
        var api = CreateApiWithCapture(@"{""output_text"": ""Response""}", c => captured = c);
        
        await api.AskAsync("Test");
        
        Assert.Contains("\"model\":\"gpt-4.1-mini\"", captured);
    }

    [Fact]
    public async Task AskAsync_WithHttpError_ThrowsHttpRequestException() =>
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateApi("Error", HttpStatusCode.BadRequest).AskAsync("Test"));

    [Fact]
    public async Task AskAsync_With401Unauthorized_ThrowsHttpRequestException() =>
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateApi("Error", HttpStatusCode.Unauthorized).AskAsync("Test"));

    [Fact]
    public async Task AskAsync_With500ServerError_ThrowsHttpRequestException() =>
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateApi("Error", HttpStatusCode.InternalServerError).AskAsync("Test"));

    [Fact]
    public async Task AskAsync_WithEmptyString_SendsEmptyInput()
    {
        string? captured = null;
        var api = CreateApiWithCapture(@"{""output_text"": ""Response""}", c => captured = c);
        
        await api.AskAsync("");
        
        Assert.Contains("\"input\":\"\"", captured);
    }

    [Fact]
    public async Task AskAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        var api = CreateApi("{\"output\": [{\"content\": [{\"text\": \"Response with quotes\"}]}]}");
        
        var result = await api.AskAsync("Input with quotes");
        
        Assert.Contains("Response with", result);
    }

    [Fact]
    public async Task AskAsync_WithNullTextValue_SkipsNullText()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Valid text. ""}, {""text"": null}, {""text"": ""More text.""}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Valid text. More text.", result);
    }

    [Fact]
    public async Task AskAsync_WithNonStringTextValue_DoesNotExtract()
    {
        var json = @"{""output"": [{""content"": [{""text"": 12345}, {""text"": ""Valid text.""}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Valid text.", result);
    }

    [Fact]
    public async Task AskAsync_WithEmptyContentArray_ReturnsRawJson() =>
        Assert.Equal(@"{""output"": [{""content"": []}]}", await CreateApi(@"{""output"": [{""content"": []}]}").AskAsync("Test"));

    [Fact]
    public async Task AskAsync_WithMissingContentProperty_ReturnsRawJson() =>
        Assert.Equal(@"{""output"": [{""message"": ""text""}]}", await CreateApi(@"{""output"": [{""message"": ""text""}]}").AskAsync("Test"));

    [Fact]
    public async Task AskAsync_WithComplexNestedStructure_ExtractsAllText()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Level 1a. ""}, {""content"": [{""text"": ""Level 2a. ""}, {""text"": ""Level 2b. ""}]}]}, {""content"": [{""text"": ""Level 1b.""}]}]}";
        var api = CreateApi(json);
        
        var result = await api.AskAsync("Test question");
        
        Assert.Equal("Level 1a. Level 2a. Level 2b. Level 1b.", result);
    }

    [Fact]
    public async Task AskAsync_SendsCorrectRequestFormat() =>
        await CreateApi(@"{""output_text"": ""Response""}").AskAsync("Test");

    [Fact]
    public async Task AskAsync_WithLongInput_HandlesCorrectly() =>
        Assert.Equal("Response", await CreateApi(@"{""output_text"": ""Response""}").AskAsync(new string('A', 10000)));

    [Fact]
    public async Task AskAsync_WithUnicodeCharacters_HandlesCorrectly() =>
        Assert.Equal("你好世界 🌍 Привет мир", await CreateApi(@"{""output_text"": ""你好世界 🌍 Привет мир""}").AskAsync("Hello 世界"));

    private IOpenAiApi CreateApi(string response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = CreateMockHandler(response, status);
        var httpClient = new HttpClient(handler.Object, false);
        var parser = new OpenAiResponseParser();
        return new OpenAiApi(httpClient, parser, null, TestApiKey);
    }

    private IOpenAiApi CreateApiWithCapture(string response, Action<string> capture)
    {
        var handler = CreateMockHandlerWithCapture(response, capture);
        var httpClient = new HttpClient(handler.Object, false);
        var parser = new OpenAiResponseParser();
        return new OpenAiApi(httpClient, parser, null, TestApiKey);
    }

    private Mock<HttpMessageHandler> CreateMockHandler(string response, HttpStatusCode status)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            });
        return mockHandler;
    }

    private Mock<HttpMessageHandler> CreateMockHandlerWithCapture(string response, Action<string> capture)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.Content != null) capture(req.Content.ReadAsStringAsync().Result);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(response, Encoding.UTF8, "application/json")
                };
            });
        return mockHandler;
    }
}
