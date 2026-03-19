using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Utilities;

/// <summary>
/// Central error handling utility for the UI layer.
///
/// Responsibilities:
///   1. Display user-friendly error dialogs
///   2. Log every error to Serilog automatically
///   3. Classify errors by severity
///   4. Provide safe async execution wrappers
/// </summary>
public static class ErrorHandler
{
    // ── Severity ──────────────────────────────────────────────────────────────

    public enum Severity
    {
        Info,
        Warning,
        Error,
        Fatal
    }

    // ── Core display methods ──────────────────────────────────────────────────

    /// <summary>
    /// Shows an error dialog and logs the message.
    /// </summary>
    public static void Show(
        string message,
        string title = "Error",
        Severity severity = Severity.Error,
        Exception? ex = null)
    {
        // Log first — always, even if the dialog crashes
        LogBySeverity(severity, message, ex);

        var icon = severity switch
        {
            Severity.Info => MessageBoxIcon.Information,
            Severity.Warning => MessageBoxIcon.Warning,
            Severity.Fatal => MessageBoxIcon.Stop,
            _ => MessageBoxIcon.Error
        };

        MessageBox.Show(
            BuildUserMessage(message, ex),
            title,
            MessageBoxButtons.OK,
            icon);
    }

    /// <summary>
    /// Shows a fatal error dialog, logs it, and optionally exits.
    /// </summary>
    public static void ShowFatal(
        string message,
        Exception? ex = null,
        bool exitApplication = false)
    {
        SerilogLog.Fatal(ex, "[FATAL] {Message}", message);
        SerilogLog.CloseAndFlush();

        MessageBox.Show(
            $"A critical error has occurred and the application cannot continue.\n\n" +
            $"{message}\n\n" +
            (ex != null ? $"Details: {ex.Message}\n\n" : string.Empty) +
            $"Logs are saved to:\n{AppLogger.GetLogDirectory()}",
            "Fatal Error — NutritionMonitor",
            MessageBoxButtons.OK,
            MessageBoxIcon.Stop);

        if (exitApplication)
            Application.Exit();
    }

    /// <summary>
    /// Shows a validation warning — no exception, lighter styling.
    /// </summary>
    public static void ShowValidation(string message, string title = "Validation Error")
    {
        SerilogLog.Warning("[VALIDATION] {Message}", message);

        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    /// <summary>
    /// Asks a yes/no confirmation. Returns true if Yes.
    /// </summary>
    public static bool Confirm(
        string message,
        string title = "Confirm",
        bool defaultYes = false)
    {
        var result = MessageBox.Show(
            message,
            title,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            defaultYes
                ? MessageBoxDefaultButton.Button1
                : MessageBoxDefaultButton.Button2);

        return result == DialogResult.Yes;
    }

    // ── Safe execution wrappers ───────────────────────────────────────────────

    /// <summary>
    /// Executes an async action safely.
    /// Any exception is caught, logged, and displayed.
    /// Returns true on success, false on failure.
    /// </summary>
    public static async Task<bool> TryAsync(
        Func<Task> action,
        string context,
        Control? uiOwner = null,
        bool showDialog = true)
    {
        try
        {
            await action();
            return true;
        }
        catch (OperationCanceledException)
        {
            SerilogLog.Debug("[CANCELLED] {Context}", context);
            return false;
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "[ERROR] {Context}", context);

            if (showDialog)
            {
                string msg = $"An error occurred in: {context}\n\n{ex.Message}";
                if (uiOwner != null && uiOwner.InvokeRequired)
                    uiOwner.Invoke(() => ShowError(msg));
                else
                    ShowError(msg);
            }

            return false;
        }
    }

    /// <summary>
    /// Executes an async function safely, returning a result.
    /// Returns default(T) on failure.
    /// </summary>
    public static async Task<T?> TryAsync<T>(
        Func<Task<T>> action,
        string context,
        Control? uiOwner = null,
        bool showDialog = true)
    {
        try
        {
            return await action();
        }
        catch (OperationCanceledException)
        {
            SerilogLog.Debug("[CANCELLED] {Context}", context);
            return default;
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "[ERROR] {Context}", context);

            if (showDialog)
            {
                string msg = $"An error occurred in: {context}\n\n{ex.Message}";
                if (uiOwner != null && uiOwner.InvokeRequired)
                    uiOwner.Invoke(() => ShowError(msg));
                else
                    ShowError(msg);
            }

            return default;
        }
    }

    // ── Global exception hooks ────────────────────────────────────────────────

    /// <summary>
    /// Registers global exception handlers.
    /// Call once from Program.cs before Application.Run().
    /// </summary>
    public static void RegisterGlobalHandlers()
    {
        // WinForms UI thread exceptions
        Application.ThreadException += (sender, e) =>
        {
            SerilogLog.Fatal(e.Exception,
                "[UNHANDLED UI] {Message}", e.Exception.Message);

            var result = MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\n" +
                $"The application will attempt to continue.\n\n" +
                $"Click OK to continue  |  Cancel to exit.",
                "Unexpected Error",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Error);

            if (result == DialogResult.Cancel)
            {
                SerilogLog.Information("User chose to exit after unhandled exception.");
                SerilogLog.CloseAndFlush();
                Application.Exit();
            }
        };

        // Non-UI thread exceptions (.NET runtime)
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;

            SerilogLog.Fatal(ex,
                "[UNHANDLED DOMAIN] Terminating={Terminating}  {Message}",
                e.IsTerminating,
                ex?.Message ?? "Unknown");

            SerilogLog.CloseAndFlush();

            if (e.IsTerminating)
            {
                MessageBox.Show(
                    $"A fatal unhandled error has caused the application to terminate.\n\n" +
                    $"{ex?.Message ?? "Unknown error"}\n\n" +
                    $"Logs: {AppLogger.GetLogDirectory()}",
                    "Fatal Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Stop);
            }
        };

        // Task exceptions (unobserved)
        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            SerilogLog.Error(e.Exception,
                "[UNOBSERVED TASK] {Message}", e.Exception.Message);
            e.SetObserved(); // Prevent process crash
        };

        SerilogLog.Information("Global exception handlers registered.");
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void LogBySeverity(
        Severity severity, string message, Exception? ex)
    {
        switch (severity)
        {
            case Severity.Info:
                SerilogLog.Information("[INFO] {Message}", message);
                break;
            case Severity.Warning:
                SerilogLog.Warning("[WARN] {Message}", message);
                break;
            case Severity.Fatal:
                SerilogLog.Fatal(ex, "[FATAL] {Message}", message);
                break;
            default:
                if (ex != null)
                    SerilogLog.Error(ex, "[ERROR] {Message}", message);
                else
                    SerilogLog.Error("[ERROR] {Message}", message);
                break;
        }
    }

    private static string BuildUserMessage(string message, Exception? ex)
    {
        if (ex == null) return message;

        return $"{message}\n\nDetails: {ex.Message}\n\n" +
               $"If this problem persists, check the logs at:\n" +
               $"{AppLogger.GetLogDirectory()}";
    }
}