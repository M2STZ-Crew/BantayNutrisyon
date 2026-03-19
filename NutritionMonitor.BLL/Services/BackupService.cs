using System.Text.Json;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.BLL.Services;

public class BackupService : IBackupService
{
    private readonly IStudentService _studentService;
    private readonly IMealLogService _mealLogService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public BackupService(IStudentService studentService, IMealLogService mealLogService)
    {
        _studentService = studentService;
        _mealLogService = mealLogService;
    }

    public async Task<(bool Success, string Message)> ExportAsync(string filePath)
    {
        try
        {
            var students = (await _studentService.GetAllStudentsAsync()).ToList();
            var logs = (await _mealLogService.GetLogsByDateRangeAsync(DateTime.MinValue, DateTime.MaxValue)).ToList();

            var backup = new BackupDto
            {
                BackupDate = DateTime.UtcNow,
                Students = students,
                MealLogs = logs
            };

            var json = JsonSerializer.Serialize(backup, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
            return (true, $"Backup exported successfully to {filePath}");
        }
        catch (Exception ex)
        {
            return (false, $"Export failed: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ImportAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return (false, "File not found.");

            var json = await File.ReadAllTextAsync(filePath);
            var backup = JsonSerializer.Deserialize<BackupDto>(json, JsonOptions);

            if (backup == null)
                return (false, "Invalid backup file format.");

            int studentsAdded = 0;
            int logsAdded = 0;

            foreach (var student in backup.Students)
            {
                var result = await _studentService.AddStudentAsync(student);
                if (result.Success) studentsAdded++;
            }

            foreach (var log in backup.MealLogs)
            {
                var result = await _mealLogService.AddLogAsync(log);
                if (result.Success) logsAdded++;
            }

            return (true, $"Import completed: {studentsAdded} students, {logsAdded} meal logs restored.");
        }
        catch (Exception ex)
        {
            return (false, $"Import failed: {ex.Message}");
        }
    }
}