using ViAiStudio.Domain.Entities;

namespace ViAiStudio.Application.Common;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task AddProfileAsync(UserProfile profile, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRoleNamesAsync(int userId, CancellationToken cancellationToken);
    Task<Role> GetOrCreateRoleAsync(string name, CancellationToken cancellationToken);
    Task AddUserRoleAsync(int userId, int roleId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}