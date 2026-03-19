using NutritionMonitor.Models.Entities;

namespace NutritionMonitor.Models.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(int id);
    Task<IEnumerable<Student>> GetAllActiveAsync();
    Task<IEnumerable<Student>> SearchAsync(string keyword);
    Task<Student?> GetByStudentNumberAsync(string studentNumber);
    Task<Student> AddAsync(Student student);
    Task<Student> UpdateAsync(Student student);
    Task<bool> SoftDeleteAsync(int id);
}