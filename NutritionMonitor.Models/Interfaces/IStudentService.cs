using NutritionMonitor.Models.DTOs;

namespace NutritionMonitor.Models.Interfaces;

public interface IStudentService
{
    Task<IEnumerable<StudentDto>> GetAllStudentsAsync();
    Task<IEnumerable<StudentDto>> SearchStudentsAsync(string keyword);
    Task<StudentDto?> GetStudentByIdAsync(int id);
    Task<(bool Success, string Message)> AddStudentAsync(StudentDto dto);
    Task<(bool Success, string Message)> UpdateStudentAsync(StudentDto dto);
    Task<(bool Success, string Message)> DeleteStudentAsync(int id);
}