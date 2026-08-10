using Microsoft.EntityFrameworkCore;
using ViAiStudio.Application.Common;
using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Infrastructure.Persistence;

public sealed class UserRepository(ViAiStudioDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        return Task.CompletedTask;
    }

    public Task AddProfileAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        dbContext.UserProfiles.Add(profile);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync(int userId, CancellationToken cancellationToken) =>
        await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
            .ToListAsync(cancellationToken);

    public async Task<Role> GetOrCreateRoleAsync(string name, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = new Role { Name = name };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    public Task AddUserRoleAsync(int userId, int roleId, CancellationToken cancellationToken)
    {
        dbContext.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}