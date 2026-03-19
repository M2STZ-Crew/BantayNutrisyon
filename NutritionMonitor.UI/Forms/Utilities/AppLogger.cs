using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Utilities;

/// <summary>
/// Centralized structured logging wrapper.
/// Ensures every log entry is consistently formatted and
/// always includes the calling context automatically.
/// </summary>
public static class AppLogger
{
    // ── Log levels ────────────────────────────────────────────────────────────

    public static void Info(string message, params object?[] args)
    {
        SerilogLog.Information(message, args);
    }

    public static void Warn(string message, params object?[] args)
    {
        SerilogLog.Warning(message, args);
    }

    public static void Error(string message, params object?[] args)
    {
        SerilogLog.Error(message, args);
    }

    public static void Error(Exception ex, string message, params object?[] args)
    {
        SerilogLog.Error(ex, message, args);
    }

    public static void Fatal(Exception ex, string message, params object?[] args)
    {
        SerilogLog.Fatal(ex, message, args);
    }

    public static void Debug(string message, params object?[] args)
    {
        SerilogLog.Debug(message, args);
    }

    // ── Contextual helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Logs a user action with consistent formatting.
    /// </summary>
    public static void UserAction(string user, string action, string detail = "")
    {
        SerilogLog.Information(
            "[USER ACTION] {User} → {Action} {Detail}",
            user, action, detail);
    }

    /// <summary>
    /// Logs a navigation event.
    /// </summary>
    public static void Navigation(string from, string to)
    {
        SerilogLog.Debug("[NAV] {From} → {To}", from, to);
    }

    /// <summary>
    /// Logs the start of a data operation.
    /// </summary>
    public static void DataOperation(string operation, string entity, string detail = "")
    {
        SerilogLog.Information(
            "[DATA] {Operation} on {Entity} — {Detail}",
            operation, entity, detail);
    }

    /// <summary>
    /// Logs a validation failure.
    /// </summary>
    public static void ValidationFailed(string context, string reason)
    {
        SerilogLog.Warning(
            "[VALIDATION] {Context} failed: {Reason}",
            context, reason);
    }

    // ── Log file path ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the path to the log directory for the current day.
    /// </summary>
    public static string GetLogDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NutritionMonitor", "Logs");
    }

    /// <summary>
    /// Returns all existing log files sorted newest first.
    /// </summary>
    public static IEnumerable<FileInfo> GetLogFiles()
    {
        var dir = GetLogDirectory();
        if (!Directory.Exists(dir))
            return Enumerable.Empty<FileInfo>();

        return new DirectoryInfo(dir)
            .GetFiles("app-*.log")
            .OrderByDescending(f => f.LastWriteTime);
    }
}