namespace NutritionMonitor.Models.DTOs;

public class BackupDto
{
    public DateTime BackupDate { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0";
    public List<StudentDto> Students { get; set; } = new();
    public List<MealLogDto> MealLogs { get; set; } = new();
}