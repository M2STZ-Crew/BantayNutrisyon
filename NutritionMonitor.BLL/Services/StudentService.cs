using NutritionMonitor.DAL.Repositories;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.BLL.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepository _studentRepository;

    public StudentService(IStudentRepository studentRepository)
    {
        _studentRepository = studentRepository;
    }

    public async Task<IEnumerable<StudentDto>> GetAllStudentsAsync()
    {
        var students = await _studentRepository.GetAllActiveAsync();
        return students.Select(MapToDto);
    }

    public async Task<IEnumerable<StudentDto>> SearchStudentsAsync(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllStudentsAsync();

        var students = await _studentRepository.SearchAsync(keyword.Trim());
        return students.Select(MapToDto);
    }

    public async Task<StudentDto?> GetStudentByIdAsync(int id)
    {
        var student = await _studentRepository.GetByIdAsync(id);
        return student == null ? null : MapToDto(student);
    }

    public async Task<(bool Success, string Message)> AddStudentAsync(StudentDto dto)
    {
        var validation = ValidateStudent(dto);
        if (!validation.IsValid) return (false, validation.Message);

        var existing = await _studentRepository.GetByStudentNumberAsync(dto.StudentNumber.Trim());
        if (existing != null) return (false, "Student number already exists.");

        var entity = MapToEntity(dto);
        await _studentRepository.AddAsync(entity);
        return (true, "Student added successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateStudentAsync(StudentDto dto)
    {
        var validation = ValidateStudent(dto);
        if (!validation.IsValid) return (false, validation.Message);

        var existing = await _studentRepository.GetByIdAsync(dto.Id);
        if (existing == null) return (false, "Student not found.");

        // Check student number uniqueness (excluding self)
        var duplicate = await _studentRepository.GetByStudentNumberAsync(dto.StudentNumber.Trim());
        if (duplicate != null && duplicate.Id != dto.Id)
            return (false, "Student number already exists.");

        existing.StudentNumber = dto.StudentNumber.Trim();
        existing.FirstName = dto.FirstName.Trim();
        existing.LastName = dto.LastName.Trim();
        existing.DateOfBirth = dto.DateOfBirth;
        existing.Gender = dto.Gender;
        existing.GradeLevel = dto.GradeLevel.Trim();
        existing.Section = dto.Section.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        await _studentRepository.UpdateAsync(existing);
        return (true, "Student updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteStudentAsync(int id)
    {
        var success = await _studentRepository.SoftDeleteAsync(id);
        return success
            ? (true, "Student deleted successfully.")
            : (false, "Student not found.");
    }

    private static (bool IsValid, string Message) ValidateStudent(StudentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StudentNumber))
            return (false, "Student number is required.");
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            return (false, "First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            return (false, "Last name is required.");
        if (dto.DateOfBirth == default)
            return (false, "Date of birth is required.");
        if (dto.DateOfBirth > DateTime.Today)
            return (false, "Date of birth cannot be in the future.");
        if (string.IsNullOrWhiteSpace(dto.GradeLevel))
            return (false, "Grade level is required.");
        if (string.IsNullOrWhiteSpace(dto.Section))
            return (false, "Section is required.");

        return (true, string.Empty);
    }

    private static StudentDto MapToDto(Student s) => new()
    {
        Id = s.Id,
        StudentNumber = s.StudentNumber,
        FirstName = s.FirstName,
        LastName = s.LastName,
        DateOfBirth = s.DateOfBirth,
        Gender = s.Gender,
        GradeLevel = s.GradeLevel,
        Section = s.Section
    };

    private static Student MapToEntity(StudentDto dto) => new()
    {
        StudentNumber = dto.StudentNumber.Trim(),
        FirstName = dto.FirstName.Trim(),
        LastName = dto.LastName.Trim(),
        DateOfBirth = dto.DateOfBirth,
        Gender = dto.Gender,
        GradeLevel = dto.GradeLevel.Trim(),
        Section = dto.Section.Trim(),
        CreatedAt = DateTime.UtcNow
    };
}