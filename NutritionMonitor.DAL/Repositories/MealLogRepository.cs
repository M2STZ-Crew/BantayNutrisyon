using Microsoft.EntityFrameworkCore;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.DAL.Repositories;

public class MealLogRepository : IMealLogRepository
{
    private readonly AppDbContext _context;

    public MealLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MealLog?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.MealLogs
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.Id == id);
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to retrieve meal log with ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<MealLog>> GetByStudentIdAsync(int studentId)
    {
        try
        {
            return await _context.MealLogs
                .Include(m => m.Student)
                .Where(m => m.StudentId == studentId)
                .OrderByDescending(m => m.LogDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to retrieve meal logs for student {studentId}.", ex);
        }
    }

    public async Task<IEnumerable<MealLog>> GetByStudentIdAndDateRangeAsync(int studentId, DateTime from, DateTime to)
    {
        try
        {
            return await _context.MealLogs
                .Include(m => m.Student)
                .Where(m => m.StudentId == studentId && m.LogDate >= from && m.LogDate <= to)
                .OrderByDescending(m => m.LogDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve meal logs by date range.", ex);
        }
    }

    public async Task<IEnumerable<MealLog>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        try
        {
            return await _context.MealLogs
                .Include(m => m.Student)
                .Where(m => m.LogDate >= from && m.LogDate <= to)
                .OrderByDescending(m => m.LogDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve meal logs by date range.", ex);
        }
    }

    public async Task<IEnumerable<MealLog>> GetAllAsync()
    {
        try
        {
            return await _context.MealLogs
                .Include(m => m.Student)
                .OrderByDescending(m => m.LogDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve all meal logs.", ex);
        }
    }

    public async Task<MealLog> AddAsync(MealLog mealLog)
    {
        try
        {
            _context.MealLogs.Add(mealLog);
            await _context.SaveChangesAsync();
            return mealLog;
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to add meal log.", ex);
        }
    }

    public async Task<MealLog> UpdateAsync(MealLog mealLog)
    {
        try
        {
            _context.MealLogs.Update(mealLog);
            await _context.SaveChangesAsync();
            return mealLog;
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to update meal log.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var log = await _context.MealLogs.FindAsync(id);
            if (log == null) return false;
            _context.MealLogs.Remove(log);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to delete meal log with ID {id}.", ex);
        }
    }
}