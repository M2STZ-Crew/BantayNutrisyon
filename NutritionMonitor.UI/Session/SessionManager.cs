using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;

namespace NutritionMonitor.UI.Session;

/// <summary>
/// Holds the currently authenticated user for the lifetime of the session.
/// Access from anywhere in the UI layer via SessionManager.Current.
/// </summary>
public static class SessionManager
{
    private static UserDto? _currentUser;

    public static UserDto Current => _currentUser
        ?? throw new InvalidOperationException("No user is currently logged in.");

    public static bool IsLoggedIn => _currentUser != null;

    public static bool IsAdmin => _currentUser?.Role == UserRole.Admin;

    public static bool IsNutritionist => _currentUser?.Role == UserRole.Nutritionist;

    public static void SetUser(UserDto user)
    {
        _currentUser = user ?? throw new ArgumentNullException(nameof(user));
    }

    public static void Clear()
    {
        _currentUser = null;
    }
}