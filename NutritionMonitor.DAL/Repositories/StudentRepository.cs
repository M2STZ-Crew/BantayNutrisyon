using Microsoft.EntityFrameworkCore;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.DAL.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly AppDbContext _context;

    public StudentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Students
                .Include(s => s.MealLogs)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to retrieve student with ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<Student>> GetAllActiveAsync()
    {
        try
        {
            return await _context.Students
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve students.", ex);
        }
    }

    public async Task<IEnumerable<Student>> SearchAsync(string keyword)
    {
        try
        {
            var lower = keyword.ToLower();
            return await _context.Students
                .Where(s => !s.IsDeleted &&
                    (s.FirstName.ToLower().Contains(lower) ||
                     s.LastName.ToLower().Contains(lower) ||
                     s.StudentNumber.ToLower().Contains(lower) ||
                     s.GradeLevel.ToLower().Contains(lower) ||
                     s.Section.ToLower().Contains(lower)))
                .OrderBy(s => s.LastName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to search students.", ex);
        }
    }

    public async Task<Student?> GetByStudentNumberAsync(string studentNumber)
    {
        try
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.StudentNumber == studentNumber && !s.IsDeleted);
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve student by number.", ex);
        }
    }

    public async Task<Student> AddAsync(Student student)
    {
        try
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to add student.", ex);
        }
    }

    public async Task<Student> UpdateAsync(Student student)
    {
        try
        {
            student.UpdatedAt = DateTime.UtcNow;
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
            return student;
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to update student.", ex);
        }
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;
            student.IsDeleted = true;
            student.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to delete student with ID {id}.", ex);
        }
    }
}