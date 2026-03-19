using NutritionMonitor.Models.Enums;

namespace NutritionMonitor.Models.DTOs;

public class NutritionAnalysisDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public DateTime AnalysisDate { get; set; } = DateTime.Today;
    public DateRange Period { get; set; } = new();

    public double AvgCalories { get; set; }
    public double AvgProtein { get; set; }
    public double AvgCarbohydrates { get; set; }
    public double AvgFats { get; set; }
    public double AvgFiber { get; set; }
    public double AvgVitaminA { get; set; }
    public double AvgVitaminC { get; set; }
    public double AvgVitaminD { get; set; }
    public double AvgCalcium { get; set; }
    public double AvgIron { get; set; }
    public double AvgZinc { get; set; }

    public double WeightedDeficitPercentage { get; set; }
    public NutritionStatus Status { get; set; }
    public List<NutrientDeficitDetail> Deficits { get; set; } = new();
}

public class NutrientDeficitDetail
{
    public string NutrientName { get; set; } = string.Empty;
    public double RecommendedValue { get; set; }
    public double ActualValue { get; set; }
    public double DeficitPercentage { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class DateRange
{
    public DateTime From { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime To { get; set; } = DateTime.Today;
}