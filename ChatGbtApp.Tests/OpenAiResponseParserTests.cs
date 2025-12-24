using System.Text.Json;
using ChatGbtApp.Services;

namespace ChatGbtApp.Tests;

public class OpenAiResponseParserTests
{
    private readonly OpenAiResponseParser _parser = new();

    [Fact]
    public void TryExtractText_WithOutputAndTextProperty_ReturnsTrue()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Hello World""}]}]}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.True(result);
        Assert.Equal("Hello World", text);
    }

    [Fact]
    public void TryExtractText_WithMultipleTexts_ConcatenatesAll()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Part1 ""}, {""text"": ""Part2""}]}]}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.True(result);
        Assert.Equal("Part1 Part2", text);
    }

    [Fact]
    public void TryExtractText_WithNestedContent_ExtractsNestedText()
    {
        var json = @"{""output"": [{""content"": [{""content"": [{""text"": ""Nested""}]}]}]}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.True(result);
        Assert.Equal("Nested", text);
    }

    [Fact]
    public void TryExtractText_WithOutputTextProperty_ReturnsOutputText()
    {
        var json = @"{""output_text"": ""Direct text""}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.True(result);
        Assert.Equal("Direct text", text);
    }

    [Fact]
    public void TryExtractText_WithNoTextProperties_ReturnsFalse()
    {
        var json = @"{""status"": ""ok""}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.False(result);
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void TryExtractText_WithNullText_SkipsNull()
    {
        var json = @"{""output"": [{""content"": [{""text"": ""Valid""}, {""text"": null}]}]}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.True(result);
        Assert.Equal("Valid", text);
    }

    [Fact]
    public void TryExtractText_WithNonStringText_IgnoresNonString()
    {
        var json = @"{""output"": [{""content"": [{""text"": 123}, {""text"": ""Valid""}]}]}";
        using var doc = JsonDocument.Parse(json);
        
        var result = _parser.TryExtractText(doc.RootElement, out var text);
        
        Assert.True(result);
        Assert.Equal("Valid", text);
    }
}
