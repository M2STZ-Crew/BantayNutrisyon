using NutritionMonitor.Models.Entities;

namespace NutritionMonitor.Models.Interfaces;

public interface IMealLogRepository
{
    Task<MealLog?> GetByIdAsync(int id);
    Task<IEnumerable<MealLog>> GetByStudentIdAsync(int studentId);
    Task<IEnumerable<MealLog>> GetByStudentIdAndDateRangeAsync(int studentId, DateTime from, DateTime to);
    Task<IEnumerable<MealLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    Task<IEnumerable<MealLog>> GetAllAsync();
    Task<MealLog> AddAsync(MealLog mealLog);
    Task<MealLog> UpdateAsync(MealLog mealLog);
    Task<bool> DeleteAsync(int id);
}