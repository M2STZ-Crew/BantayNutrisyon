// PHASE 5 FIX — NutritionAnalysisService.cs
// Changes made:
//
//   [FIX #1] Removed: using NutritionMonitor.DAL.Repositories;
//
//            SAME REASON AS MealLogService.cs:
//
//            This is the Business Logic Layer. It should never import
//            concrete repository classes from the DAL.
//
//            Think of it like this using a real-world analogy:
//
//            Imagine a DOCTOR (BLL) who needs to read a patient's records.
//            The doctor doesn't care WHETHER the records are stored in a
//            filing cabinet, a computer, or a cloud server (DAL implementation).
//            The doctor only knows there's a "records desk" (interface) they
//            can ask. Who runs that desk and how is none of the doctor's business.
//
//            By importing NutritionMonitor.DAL.Repositories, the BLL was
//            peeking behind the curtain at the filing cabinet directly.
//            That breaks the separation of concerns.
//
//            The fix: remove the import. The constructor still receives
//            IStudentRepository and IMealLogRepository — those are the
//            interfaces defined in Models, which BLL is allowed to use.
//            The concrete classes (StudentRepository, MealLogRepository)
//            are wired up in ServiceLocator.cs, which is the only place
//            that should know about them.

using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.BLL.Services;

public class NutritionAnalysisService : INutritionAnalysisService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IMealLogRepository _mealLogRepository;

    public NutritionAnalysisService(
        IStudentRepository studentRepository,
        IMealLogRepository mealLogRepository)
    {
        _studentRepository = studentRepository;
        _mealLogRepository = mealLogRepository;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<NutritionAnalysisDto?> AnalyzeStudentAsync(
        int studentId, DateTime from, DateTime to)
    {
        var student = await _studentRepository.GetByIdAsync(studentId);
        if (student == null) return null;

        var logs = (await _mealLogRepository
            .GetByStudentIdAndDateRangeAsync(studentId, from, to))
            .ToList();

        if (logs.Count == 0) return null;

        int age = CalculateAge(student.DateOfBirth);
        var reni = GetReniValues(age, student.Gender);

        var dto = new NutritionAnalysisDto
        {
            StudentId = studentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            Age = age,
            Gender = student.Gender,
            Period = new DateRange { From = from, To = to },
            AvgCalories = logs.Average(l => l.CaloriesKcal),
            AvgProtein = logs.Average(l => l.ProteinG),
            AvgCarbohydrates = logs.Average(l => l.CarbohydratesG),
            AvgFats = logs.Average(l => l.FatsG),
            AvgFiber = logs.Average(l => l.FiberG),
            AvgVitaminA = logs.Average(l => l.VitaminAMcg),
            AvgVitaminC = logs.Average(l => l.VitaminCMg),
            AvgVitaminD = logs.Average(l => l.VitaminDMcg),
            AvgCalcium = logs.Average(l => l.CalciumMg),
            AvgIron = logs.Average(l => l.IronMg),
            AvgZinc = logs.Average(l => l.ZincMg)
        };

        dto.Deficits = CalculateDeficits(dto, reni);

        dto.WeightedDeficitPercentage = dto.Deficits.Count > 0
            ? dto.Deficits.Average(d => d.DeficitPercentage)
            : 0;

        dto.Status = dto.WeightedDeficitPercentage switch
        {
            >= 30 => NutritionStatus.Malnourished,
            >= 15 => NutritionStatus.AtRisk,
            _ => NutritionStatus.Normal
        };

        return dto;
    }

    public async Task<IEnumerable<NutritionAnalysisDto>> AnalyzeAllStudentsAsync(
        DateTime from, DateTime to)
    {
        var students = await _studentRepository.GetAllActiveAsync();
        var results = new List<NutritionAnalysisDto>();

        foreach (var student in students)
        {
            var analysis = await AnalyzeStudentAsync(student.Id, from, to);
            if (analysis != null) results.Add(analysis);
        }

        return results;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Deficit Calculation
    // ─────────────────────────────────────────────────────────────────────────

    private static List<NutrientDeficitDetail> CalculateDeficits(
        NutritionAnalysisDto dto, ReniValues reni)
    {
        var deficits = new List<NutrientDeficitDetail>();

        void AddDeficit(string name, double actual, double recommended, string unit)
        {
            if (recommended <= 0) return;
            double deficit = Math.Max(0,
                (recommended - actual) / recommended * 100);

            deficits.Add(new NutrientDeficitDetail
            {
                NutrientName = name,
                RecommendedValue = recommended,
                ActualValue = actual,
                DeficitPercentage = deficit,
                Unit = unit
            });
        }

        AddDeficit("Calories", dto.AvgCalories, reni.Calories, "kcal");
        AddDeficit("Protein", dto.AvgProtein, reni.ProteinG, "g");
        AddDeficit("Vitamin A", dto.AvgVitaminA, reni.VitaminAMcg, "mcg");
        AddDeficit("Vitamin C", dto.AvgVitaminC, reni.VitaminCMg, "mg");
        AddDeficit("Vitamin D", dto.AvgVitaminD, reni.VitaminDMcg, "mcg");
        AddDeficit("Calcium", dto.AvgCalcium, reni.CalciumMg, "mg");
        AddDeficit("Iron", dto.AvgIron, reni.IronMg, "mg");
        AddDeficit("Zinc", dto.AvgZinc, reni.ZincMg, "mg");

        return deficits;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  RENI Reference Values — DOH Philippine RENI 2015
    //  Simplified by age band and gender
    // ─────────────────────────────────────────────────────────────────────────

    private static ReniValues GetReniValues(int age, Gender gender)
    {
        return age switch
        {
            <= 3 => new ReniValues(1000, 25, 400, 30, 10, 500, 8, 3),
            <= 6 => new ReniValues(1400, 35, 450, 35, 10, 600, 9, 5),
            <= 9 => new ReniValues(1600, 45, 500, 40, 10, 700, 10, 7),
            <= 12 => gender == Gender.Male
                ? new ReniValues(2000, 60, 600, 50, 10, 1000, 12, 9)
                : new ReniValues(1800, 55, 600, 50, 10, 1000, 18, 9),
            <= 15 => gender == Gender.Male
                ? new ReniValues(2300, 70, 700, 60, 10, 1200, 14, 11)
                : new ReniValues(2100, 65, 700, 60, 10, 1200, 20, 10),
            <= 18 => gender == Gender.Male
                ? new ReniValues(2500, 75, 750, 70, 10, 1200, 14, 12)
                : new ReniValues(2200, 65, 750, 70, 10, 1200, 20, 10),
            _ => gender == Gender.Male
                ? new ReniValues(2300, 65, 700, 75, 15, 1000, 12, 11)
                : new ReniValues(1900, 57, 700, 75, 15, 1000, 18, 8)
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    // Private record — lives only inside this service
    // ReniValues holds the recommended daily intake per nutrient
    private record ReniValues(
        double Calories,
        double ProteinG,
        double VitaminAMcg,
        double VitaminCMg,
        double VitaminDMcg,
        double CalciumMg,
        double IronMg,
        double ZincMg
    );
}