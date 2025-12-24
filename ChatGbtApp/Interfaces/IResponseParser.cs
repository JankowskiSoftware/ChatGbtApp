using System.Text.Json;

namespace ChatGbtApp.Interfaces;

public interface IResponseParser
{
    bool TryExtractText(JsonElement root, out string text);
}
