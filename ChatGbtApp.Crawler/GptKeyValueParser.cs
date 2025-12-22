using System.Globalization;

namespace ChatGgtApp.Crawler;

public sealed class ParsedJobFit
{
    public string? Company { get; set; }
    public string? JobTitle { get; set; }
    public int? MatchScore { get; set; }    // normalized to 0..10 (nullable if not parseable)
    public string? SeniorityFit { get; set; }   // "low", "medium", "high" (nullable if not parseable)
    public IReadOnlyList<string> MissingSkills { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> MissingAtsKeywoards { get; set; } = Array.Empty<string>(); // NOTE: key name matches your prompt typo
    public IReadOnlyList<string> Strengths { get; set; } = Array.Empty<string>();
    public string? Recommendation { get; set; }
    public string? Remote { get; set; }
    public string? Summary { get; set; }
    public string? Frontend { get; set; }
    public string? DotNetRole { get; set; }
}

public class GptKeyValueParser
{
    private const string StartMarker = "<<<RESULTS>>>";
    private const string EndMarker   = "<<<END>>>";

    // Canonical keys (output should use these; parser accepts case-insensitive variants)
    private readonly Dictionary<string, string> CanonicalKeyMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["company"] = "company",
            ["jobTitle"] = "jobTitle",
            ["matchScore"] = "matchScore",
            ["seniorityFit"] = "seniorityFit",
            ["missingSkills"] = "missingSkills",
            ["missingAtsKeywoards"] = "missingAtsKeywoards", // keep exact spelling
            ["strengths"] = "strengths",
            ["recommendation"] = "recommendation",
            ["remote"] = "remote",
            ["summary"] = "summary",
            ["frontend"] = "frontend",
            ["dotNetRole"] = "dotNetRole"
        };

    private readonly HashSet<string> AllowedSeniority =
        new(StringComparer.OrdinalIgnoreCase) { "low", "medium", "high" };

    /// <summary>
    /// Parses a ChatGPT response that ends with:
    /// <<<RESULTS>>>
    /// key=value
    /// ...
    /// <<<END>>>
    ///
    /// Returns null if it cannot find a well-formed results block.
    /// Never throws.
    /// </summary>
    public ParsedJobFit? ParseOrNull(string? fullResponse)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fullResponse))
                return null;

            if (!TryExtractBlock(fullResponse, out var block))
                return null;

            var kv = ParseKeyValues(block);

            // Pull values (null if missing / empty)
            var company = GetNullIfEmpty(kv, "company");
            var jobTitle = GetNullIfEmpty(kv, "jobTitle");

            var matchScore = ParseMatchScoreTo0To10(GetNullIfEmpty(kv, "matchScore"));

            var seniority = GetNullIfEmpty(kv, "seniorityFit");
            if (seniority is not null && !AllowedSeniority.Contains(seniority))
                seniority = null;
            else if (seniority is not null)
                seniority = seniority.ToLowerInvariant();

            var missingSkills = ParseCsvList(GetNullIfEmpty(kv, "missingSkills"));
            var missingAts = ParseCsvList(GetNullIfEmpty(kv, "missingAtsKeywoards"));
            var strengths = ParseCsvList(GetNullIfEmpty(kv, "strengths"));

            var recommendation = GetNullIfEmpty(kv, "recommendation");
            var summary = GetNullIfEmpty(kv, "summary");
            var remote = GetNullIfEmpty(kv, "remote");
            var dotNetRole = GetNullIfEmpty(kv, "dotNetRole");
            var frontend = GetNullIfEmpty(kv, "frontend");

            return new ParsedJobFit
            {
                Company = company,
                JobTitle = jobTitle,
                MatchScore = matchScore,
                SeniorityFit = seniority,
                MissingSkills = missingSkills,
                MissingAtsKeywoards = missingAts,
                Strengths = strengths,
                Recommendation = recommendation,
                Remote = remote,
                Summary = summary,
                DotNetRole = dotNetRole,
                Frontend = frontend
            };
        }
        catch
        {
            // Hard guarantee: no exceptions leak out
            return null;
        }
    }

    private bool TryExtractBlock(string fullResponse, out string block)
    {
        block = string.Empty;

        var start = fullResponse.IndexOf(StartMarker, StringComparison.Ordinal);
        if (start < 0) return false;

        var end = fullResponse.IndexOf(EndMarker, start + StartMarker.Length, StringComparison.Ordinal);
        if (end < 0) return false;

        var blockStart = start + StartMarker.Length;
        if (end <= blockStart) return false;

        block = fullResponse.Substring(blockStart, end - blockStart);
        return true;
    }

    private Dictionary<string, string> ParseKeyValues(string block)
    {
        // Returns canonical-key -> value (keeps first occurrence; ignores duplicates)
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);

        var lines = block
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        foreach (var line in lines)
        {
            var eq = line.IndexOf('=');
            if (eq <= 0) continue; // skip invalid line

            var rawKey = line.Substring(0, eq).Trim();
            var value  = line.Substring(eq + 1).Trim(); // may be empty

            if (rawKey.Length == 0) continue;

            // Normalize key to canonical if known; otherwise ignore unknown keys
            if (!CanonicalKeyMap.TryGetValue(rawKey, out var canonicalKey))
                continue;

            // Keep first value; ignore duplicates to stay stable
            if (!dict.ContainsKey(canonicalKey))
                dict[canonicalKey] = value;
        }

        return dict;
    }

    private string? GetNullIfEmpty(Dictionary<string, string> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v)) return null;
        v = v.Trim();
        return v.Length == 0 ? null : v;
    }

    private IReadOnlyList<string> ParseCsvList(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Array.Empty<string>();

        // Split by comma, trim, drop empties
        var items = csv
            .Split(',')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();

        return items;
    }

    private int? ParseMatchScoreTo0To10(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        // Accept "7", "7.0" (rare) by trying int first, then double
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return NormalizeScore(i);

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            return NormalizeScore((int)Math.Round(d, MidpointRounding.AwayFromZero));

        return null;

        static int? NormalizeScore(int value)
        {
            // Primary contract: 0..10
            if (value >= 0 && value <= 10)
                return value;

            // Tolerate accidental 0..100 (because some prompts mix this up)
            if (value >= 0 && value <= 100)
            {
                var scaled = (int)Math.Round(value / 10.0, MidpointRounding.AwayFromZero);
                if (scaled < 0) scaled = 0;
                if (scaled > 10) scaled = 10;
                return scaled;
            }

            return null;
        }
    }
}