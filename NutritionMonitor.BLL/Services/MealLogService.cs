// PHASE 5 FIX — MealLogService.cs
// Changes made:
//
//   [FIX #1] Removed: using NutritionMonitor.DAL.Repositories;
//
//            WHY IT WAS WRONG:
//            This is the Business Logic Layer (BLL). In a clean 3-layer architecture
//            the layers are stacked like this:
//
//                UI  →  BLL  →  DAL  →  Database
//
//            Each layer should only know about the layer directly below it,
//            and ONLY through interfaces — not through concrete classes.
//
//            The BLL (this file) should only know about:
//              - NutritionMonitor.Models.Interfaces  (IMealLogRepository, etc.)
//              - NutritionMonitor.Models.DTOs        (MealLogDto, etc.)
//              - NutritionMonitor.Models.Entities    (MealLog, etc.)
//
//            It should NOT import NutritionMonitor.DAL.Repositories because:
//              1. MealLogRepository is a concrete class — the BLL never uses it directly.
//                 It only ever calls the interface IMealLogRepository.
//              2. If you ever swap SQLite for PostgreSQL and rename or replace
//                 MealLogRepository, this import would break even though the
//                 BLL logic itself didn't change at all.
//              3. It creates a hidden tight coupling between layers that defeats
//                 the entire purpose of having interfaces.
//
//            The import was harmless at compile time only because the concrete
//            class happened to exist. The fix is simply removing the line —
//            zero logic changes needed because the code already used interfaces.

using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.BLL.Services;

public class MealLogService : IMealLogService
{
    private readonly IMealLogRepository _mealLogRepository;
    private readonly IStudentRepository _studentRepository;

    public MealLogService(
        IMealLogRepository mealLogRepository,
        IStudentRepository studentRepository)
    {
        _mealLogRepository = mealLogRepository;
        _studentRepository = studentRepository;
    }

    public async Task<IEnumerable<MealLogDto>> GetLogsByStudentAsync(int studentId)
    {
        var logs = await _mealLogRepository.GetByStudentIdAsync(studentId);
        return logs.Select(MapToDto);
    }

    public async Task<IEnumerable<MealLogDto>> GetLogsByStudentAndDateRangeAsync(
        int studentId, DateTime from, DateTime to)
    {
        var logs = await _mealLogRepository
            .GetByStudentIdAndDateRangeAsync(studentId, from, to);
        return logs.Select(MapToDto);
    }

    public async Task<IEnumerable<MealLogDto>> GetLogsByDateRangeAsync(
        DateTime from, DateTime to)
    {
        var logs = await _mealLogRepository.GetByDateRangeAsync(from, to);
        return logs.Select(MapToDto);
    }

    public async Task<MealLogDto?> GetLogByIdAsync(int id)
    {
        var log = await _mealLogRepository.GetByIdAsync(id);
        return log == null ? null : MapToDto(log);
    }

    public async Task<(bool Success, string Message)> AddLogAsync(MealLogDto dto)
    {
        var validation = ValidateLog(dto);
        if (!validation.IsValid) return (false, validation.Message);

        var student = await _studentRepository.GetByIdAsync(dto.StudentId);
        if (student == null) return (false, "Student not found.");

        var entity = MapToEntity(dto);
        await _mealLogRepository.AddAsync(entity);
        return (true, "Meal log added successfully.");
    }

    public async Task<(bool Success, string Message)> UpdateLogAsync(MealLogDto dto)
    {
        var validation = ValidateLog(dto);
        if (!validation.IsValid) return (false, validation.Message);

        var existing = await _mealLogRepository.GetByIdAsync(dto.Id);
        if (existing == null) return (false, "Meal log not found.");

        existing.LogDate = dto.LogDate;
        existing.MealType = dto.MealType;
        existing.CaloriesKcal = dto.CaloriesKcal;
        existing.ProteinG = dto.ProteinG;
        existing.CarbohydratesG = dto.CarbohydratesG;
        existing.FatsG = dto.FatsG;
        existing.FiberG = dto.FiberG;
        existing.VitaminAMcg = dto.VitaminAMcg;
        existing.VitaminCMg = dto.VitaminCMg;
        existing.VitaminDMcg = dto.VitaminDMcg;
        existing.CalciumMg = dto.CalciumMg;
        existing.IronMg = dto.IronMg;
        existing.ZincMg = dto.ZincMg;
        existing.Notes = dto.Notes;

        await _mealLogRepository.UpdateAsync(existing);
        return (true, "Meal log updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteLogAsync(int id)
    {
        var success = await _mealLogRepository.DeleteAsync(id);
        return success
            ? (true, "Meal log deleted successfully.")
            : (false, "Meal log not found.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Validation
    // ─────────────────────────────────────────────────────────────────────────

    private static (bool IsValid, string Message) ValidateLog(MealLogDto dto)
    {
        if (dto.StudentId <= 0)
            return (false, "Valid student is required.");
        if (dto.LogDate == default)
            return (false, "Log date is required.");
        if (dto.LogDate > DateTime.Today)
            return (false, "Log date cannot be in the future.");
        if (string.IsNullOrWhiteSpace(dto.MealType))
            return (false, "Meal type is required.");
        if (dto.CaloriesKcal < 0)
            return (false, "Calories cannot be negative.");
        if (dto.ProteinG < 0)
            return (false, "Protein cannot be negative.");
        if (dto.CarbohydratesG < 0)
            return (false, "Carbohydrates cannot be negative.");
        if (dto.FatsG < 0)
            return (false, "Fats cannot be negative.");
        return (true, string.Empty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Mapping
    // ─────────────────────────────────────────────────────────────────────────

    private static MealLogDto MapToDto(MealLog m) => new()
    {
        Id = m.Id,
        StudentId = m.StudentId,
        StudentName = m.Student != null
                            ? $"{m.Student.FirstName} {m.Student.LastName}"
                            : string.Empty,
        LogDate = m.LogDate,
        MealType = m.MealType,
        CaloriesKcal = m.CaloriesKcal,
        ProteinG = m.ProteinG,
        CarbohydratesG = m.CarbohydratesG,
        FatsG = m.FatsG,
        FiberG = m.FiberG,
        VitaminAMcg = m.VitaminAMcg,
        VitaminCMg = m.VitaminCMg,
        VitaminDMcg = m.VitaminDMcg,
        CalciumMg = m.CalciumMg,
        IronMg = m.IronMg,
        ZincMg = m.ZincMg,
        Notes = m.Notes
    };

    private static MealLog MapToEntity(MealLogDto dto) => new()
    {
        StudentId = dto.StudentId,
        LogDate = dto.LogDate,
        MealType = dto.MealType,
        CaloriesKcal = dto.CaloriesKcal,
        ProteinG = dto.ProteinG,
        CarbohydratesG = dto.CarbohydratesG,
        FatsG = dto.FatsG,
        FiberG = dto.FiberG,
        VitaminAMcg = dto.VitaminAMcg,
        VitaminCMg = dto.VitaminCMg,
        VitaminDMcg = dto.VitaminDMcg,
        CalciumMg = dto.CalciumMg,
        IronMg = dto.IronMg,
        ZincMg = dto.ZincMg,
        Notes = dto.Notes,
        CreatedAt = DateTime.UtcNow
    };
}