using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.DAL;
using NutritionMonitor.UI.Utilities;
using Serilog;
using Serilog.Events;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // ── 1. Configure Serilog ──────────────────────────────────────────────
        string logDir = AppLogger.GetLogDirectory();
        Directory.CreateDirectory(logDir);

        SerilogLog.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "NutritionMonitor")
            .WriteTo.File(
                path: Path.Combine(logDir, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                buffered: false,
                shared: true)   // shared: true allows our reader to open it simultaneously
            .CreateLogger();

        // ── 2. Register global exception handlers ─────────────────────────────
        // Must be done BEFORE Application.Run()
        Application.SetUnhandledExceptionMode(
            UnhandledExceptionMode.CatchException);

        ErrorHandler.RegisterGlobalHandlers();

        AppLogger.Info("════════════════════════════════════════════");
        AppLogger.Info("NutritionMonitor starting — {Time}", DateTime.Now);
        AppLogger.Info("OS: {OS}", Environment.OSVersion);
        AppLogger.Info(".NET: {Runtime}", Environment.Version);
        AppLogger.Info("User: {User}", Environment.UserName);

        try
        {
            // ── 3. Initialize DI ──────────────────────────────────────────────
            ServiceLocator.Initialize();
            AppLogger.Info("Dependency injection initialized.");

            // ── 4. Apply EF Core migrations ───────────────────────────────────
            using (var scope = ServiceLocator.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
                AppLogger.Info("Database migration applied.");
            }

            // ── 5. Launch login form ──────────────────────────────────────────
            Application.Run(new Forms.LoginForm());
        }
        catch (Exception ex)
        {
            ErrorHandler.ShowFatal(
                "The application failed to start.",
                ex,
                exitApplication: true);
        }
        finally
        {
            AppLogger.Info("NutritionMonitor shutting down.");
            SerilogLog.CloseAndFlush();
        }
    }
}