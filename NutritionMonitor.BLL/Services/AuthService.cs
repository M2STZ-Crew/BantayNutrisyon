using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> LoginAsync(LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            return null;

        var user = await _userRepository.GetByEmailAsync(loginDto.Email.Trim());
        if (user == null) return null;
        if (!user.IsActive) return null;

        bool passwordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        if (!passwordValid) return null;

        return MapToDto(user);
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        bool currentValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
        if (!currentValid) return false;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, 11);
        await _userRepository.UpdateAsync(user);
        return true;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive
    };
}