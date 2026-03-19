using NutritionMonitor.Models.DTOs;

namespace NutritionMonitor.Models.Interfaces;

public interface IBackupService
{
    Task<(bool Success, string Message)> ExportAsync(string filePath);
    Task<(bool Success, string Message)> ImportAsync(string filePath);
}