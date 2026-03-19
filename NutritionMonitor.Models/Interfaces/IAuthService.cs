using NutritionMonitor.Models.DTOs;

namespace NutritionMonitor.Models.Interfaces;

public interface IAuthService
{
    Task<UserDto?> LoginAsync(LoginDto loginDto);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}