namespace NutritionMonitor.Models.Entities;

public class MealLog
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateTime LogDate { get; set; }
    public string MealType { get; set; } = string.Empty; // Breakfast, Lunch, Dinner, Snack

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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Student? Student { get; set; }
}