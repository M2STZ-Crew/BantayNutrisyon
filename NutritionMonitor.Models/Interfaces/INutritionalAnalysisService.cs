using NutritionMonitor.Models.DTOs;

namespace NutritionMonitor.Models.Interfaces;

public interface INutritionAnalysisService
{
    Task<NutritionAnalysisDto?> AnalyzeStudentAsync(int studentId, DateTime from, DateTime to);
    Task<IEnumerable<NutritionAnalysisDto>> AnalyzeAllStudentsAsync(DateTime from, DateTime to);
}