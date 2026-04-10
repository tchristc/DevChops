using DevChops.Domain.Entities;

namespace DevChops.Application.Interfaces;

public interface ISavedFilterService
{
    Task<IReadOnlyList<SavedFilter>> GetForUserAsync(string userId, CancellationToken ct = default);
    Task SaveAsync(SavedFilter filter, CancellationToken ct = default);
    Task DeleteAsync(string userId, Guid filterId, CancellationToken ct = default);
}
