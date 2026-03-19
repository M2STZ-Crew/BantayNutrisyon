using Microsoft.EntityFrameworkCore;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.DAL;

namespace NutritionMonitor.DAL;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<MealLog> MealLogs => Set<MealLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired();

            // Seed admin user — password: Admin@123
            // Pre-computed BCrypt hash (never call BCrypt.HashPassword inside HasData)
            entity.HasData(new User
            {
                Id = 1,
                FullName = "System Administrator",
                Email = "admin@nutrition.local",
                PasswordHash = "$2a$11$mytc0Ge2txX31qR05Opu6.I0.gALwl14AmJKbVJR0uauQukjLowbC",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        // Student configuration
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.StudentNumber).IsUnique();
            entity.Property(s => s.StudentNumber).IsRequired().HasMaxLength(20);
            entity.Property(s => s.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(s => s.LastName).IsRequired().HasMaxLength(50);
            entity.Property(s => s.GradeLevel).IsRequired().HasMaxLength(20);
            entity.Property(s => s.Section).IsRequired().HasMaxLength(20);
        });

        // MealLog configuration
        modelBuilder.Entity<MealLog>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.MealType).IsRequired().HasMaxLength(20);
            entity.HasOne(m => m.Student)
                  .WithMany(s => s.MealLogs)
                  .HasForeignKey(m => m.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}