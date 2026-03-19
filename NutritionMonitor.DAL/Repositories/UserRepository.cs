using Microsoft.EntityFrameworkCore;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;

namespace NutritionMonitor.DAL.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve user by email.", ex);
        }
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Users.FindAsync(id);
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to retrieve user with ID {id}.", ex);
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        try
        {
            return await _context.Users.Where(u => u.IsActive).ToListAsync();
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to retrieve users.", ex);
        }
    }

    public async Task<User> AddAsync(User user)
    {
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to add user.", ex);
        }
    }

    public async Task<User> UpdateAsync(User user)
    {
        try
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            throw new DataAccessException("Failed to update user.", ex);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            user.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException($"Failed to delete user with ID {id}.", ex);
        }
    }
}