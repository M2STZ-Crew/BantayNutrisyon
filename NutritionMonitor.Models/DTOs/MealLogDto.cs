namespace NutritionMonitor.Models.DTOs;

public class MealLogDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime LogDate { get; set; }
    public string MealType { get; set; } = string.Empty;

    // Macronutrients
    public double CaloriesKcal { get; set; }
    public double ProteinG { get; set; }
    public double CarbohydratesG { get; set; }
    public double FatsG { get; set; }
    public double FiberG { get; set; }

    // Micronutrients
    public double VitaminAMcg { get; set; }
    public double VitaminCMg { get; set; }
    public double VitaminDMcg { get; set; }
    public double CalciumMg { get; set; }
    public double IronMg { get; set; }
    public double ZincMg { get; set; }

    public string? Notes { get; set; }
}