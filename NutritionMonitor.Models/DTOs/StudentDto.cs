using NutritionMonitor.Models.Enums;

namespace NutritionMonitor.Models.DTOs;

public class StudentDto
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public DateTime DateOfBirth { get; set; }
    public int Age => CalculateAge();
    public Gender Gender { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;

    private int CalculateAge()
    {
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}