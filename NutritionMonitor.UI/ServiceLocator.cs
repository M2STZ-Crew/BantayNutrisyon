using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.BLL.Services;
using NutritionMonitor.DAL;
using NutritionMonitor.DAL.Repositories;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.UI;

/// <summary>
/// Central DI container configuration for the WinForms application.
/// </summary>
public static class ServiceLocator
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider => _provider
        ?? throw new InvalidOperationException("ServiceLocator is not initialized.");

    public static void Initialize()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _provider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NutritionMonitor",
            "nutrition.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Scoped);

        // Repositories (DAL)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IMealLogRepository, MealLogRepository>();

        // Services (BLL)
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IMealLogService, MealLogService>();
        services.AddScoped<INutritionAnalysisService, NutritionAnalysisService>();
        services.AddScoped<IBackupService, BackupService>();
    }

    /// <summary>
    /// Creates a new DI scope. Callers must dispose the scope when done.
    /// </summary>
    public static IServiceScope CreateScope() => Provider.CreateScope();

    /// <summary>
    /// Resolves a service directly. Prefer CreateScope() for Scoped services.
    /// </summary>
    public static T GetService<T>() where T : notnull => Provider.GetRequiredService<T>();
}