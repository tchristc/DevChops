using DevChops.Domain.Entities;

namespace DevChops.Application.Interfaces;

public interface IUserPreferenceService
{
    Task<UserPreference> GetOrCreateAsync(string userId, CancellationToken ct = default);
    Task SaveAsync(UserPreference preference, CancellationToken ct = default);
}
