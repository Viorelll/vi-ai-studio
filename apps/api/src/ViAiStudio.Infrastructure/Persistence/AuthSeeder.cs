using Microsoft.EntityFrameworkCore;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public static class AuthSeeder
{
    private const string AdminEmail = "llleroiv@gmail.com";
    private const string AdminRoleName = "Admin";

    public static async Task SeedAsync(ViAiStudioDbContext db, CancellationToken cancellationToken = default)
    {
        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == AdminRoleName, cancellationToken);
        if (adminRole is null)
        {
            adminRole = new Role { Name = AdminRoleName };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync(cancellationToken);
        }

        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail, cancellationToken);
        if (adminUser is null)
        {
            adminUser = new User { Email = AdminEmail, CreatedAt = DateTimeOffset.UtcNow };
            db.Users.Add(adminUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        var hasAdminRole = await db.UserRoles.AnyAsync(
            ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id, cancellationToken);
        if (!hasAdminRole)
        {
            db.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}