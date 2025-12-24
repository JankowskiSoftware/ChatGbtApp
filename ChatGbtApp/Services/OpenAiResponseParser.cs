using System.Text;
using System.Text.Json;
using ChatGbtApp.Interfaces;

namespace ChatGbtApp.Services;

public class OpenAiResponseParser : IResponseParser
{
    public bool TryExtractText(JsonElement root, out string text)
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
