using Microsoft.Extensions.Logging;

namespace ChatGgtApp.Crawler.Core;

/// <summary>
/// Collects logs during job processing instead of writing them immediately.
/// This prevents log interleaving when multiple threads process jobs in parallel.
/// </summary>
public class LogCollector
{
    private readonly List<LogEntry> _logs = [];

    public record LogEntry(LogLevel Level, string Message);

    public void Log(LogLevel level, string message)
    {
        _logs.Add(new LogEntry(level, message));
    }

    public void LogInformation(string message) => Log(LogLevel.Information, message);
    public void LogWarning(string message) => Log(LogLevel.Warning, message);
    public void LogDebug(string message) => Log(LogLevel.Debug, message);
    public void LogError(string message) => Log(LogLevel.Error, message);
    public void LogCritical(string message) => Log(LogLevel.Critical, message);

    public IReadOnlyList<LogEntry> GetLogs() => _logs.AsReadOnly();

    public void WriteLogsTo(ILogger logger, string? prefix = null)
    {
        foreach (var entry in _logs)
        {
            var message = prefix != null ? $"[{prefix}] {entry.Message}" : entry.Message;
            logger.Log(entry.Level, message);
        }
    }
}
