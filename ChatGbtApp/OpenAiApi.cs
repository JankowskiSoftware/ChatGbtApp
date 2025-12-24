using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ChatGbtApp;

using Environment = Environment;

public class OpenAiApi
{
    private readonly string API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public async Task<string> AskAsync(string input, string model = "gpt-4.1-mini") // "gpt-5.2")
    {
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromMinutes(3);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", API_KEY);

        var payload = new { model, input };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await http.PostAsync("https://api.openai.com/v1/responses", content);
        resp.EnsureSuccessStatusCode();
        var respText = await resp.Content.ReadAsStringAsync();

        // Parse the JSON response to extract plain text instead of returning escaped JSON.
        try
        {
            using var doc = JsonDocument.Parse(respText);
            if (TryExtractText(doc.RootElement, out var parsedText))
                return parsedText;
        }
        catch (JsonException)
        {
            // Fall through to return the raw response text if parsing fails.
        }

        return respText;
    }

    private static bool TryExtractText(JsonElement root, out string text)
    {
        var sb = new StringBuilder();
        if (root.TryGetProperty("output", out var output))
            foreach (var outElem in output.EnumerateArray())
            {
                if (!outElem.TryGetProperty("content", out var contents))
                    continue;

                foreach (var cont in contents.EnumerateArray())
                    if (cont.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                        sb.Append(t.GetString());
                    else if (cont.TryGetProperty("content", out var inner))
                        foreach (var innerItem in inner.EnumerateArray())
                            if (innerItem.TryGetProperty("text", out var it) && it.ValueKind == JsonValueKind.String)
                                sb.Append(it.GetString());
            }

        if (sb.Length > 0)
        {
            text = sb.ToString();
            return true;
        }

        if (root.TryGetProperty("output_text", out var outText) && outText.ValueKind == JsonValueKind.String)
        {
            text = outText.GetString() ?? string.Empty;
            return true;
        }

        text = string.Empty;
        return false;
    }
}