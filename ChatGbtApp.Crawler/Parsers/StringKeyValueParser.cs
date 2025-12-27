namespace ChatGgtApp.Crawler.Parsers;

public sealed class StringKeyValueParser
{
    private readonly Dictionary<string, string?> _values;

    private StringKeyValueParser(Dictionary<string, string?> values)
    {
        _values = values;
    }

    /// <summary>
    /// Parses lines in format: fieldName;value (one per line).
    /// - Splits on the FIRST semicolon only.
    /// - Trims whitespace.
    /// - Treats literal "null" (case-insensitive) as null.
    /// - Ignores empty/invalid lines.
    /// - If duplicate keys appear, the last one wins.
    /// </summary>
    public static StringKeyValueParser Parse(string input)
    {
        var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(input))
            return new StringKeyValueParser(dict);

        var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var line = rawLine.Trim();

            var idx = line.IndexOf(':');
            if (idx <= 0) // no semicolon or empty key
                continue;

            var key = line.Substring(0, idx).Trim();
            if (key.Length == 0)
                continue;

            var valuePart = (idx + 1 < line.Length) ? line.Substring(idx + 1) : "";
            var value = valuePart.Trim();

            if (value.Length == 0 || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                dict[key] = null;
            else
                dict[key] = value;
        }

        return new StringKeyValueParser(dict);
    }

    /// <summary>
    /// Returns the value for a field, or null if missing or explicitly "null".
    /// </summary>
    public string? Get(string fieldName)
        => fieldName != null && _values.TryGetValue(fieldName, out var v) ? v : null;

    /// <summary>
    /// Safe lookup without exceptions.
    /// </summary>
    public bool TryGet(string fieldName, out string? value)
    {
        value = null;
        return fieldName != null && _values.TryGetValue(fieldName, out value);
    }
}

// Example usage:
// var parsed = StringKeyValueParser.Parse(resultTextFromModel);
// var title = parsed.Get("jobTitle");
// var hourlyMin = parsed.Get("hourlyMin");
