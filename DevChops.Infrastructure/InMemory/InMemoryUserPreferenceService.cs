using System.Collections.Concurrent;
using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;

namespace DevChops.Infrastructure.InMemory;

public class InMemoryUserPreferenceService : IUserPreferenceService
{
    private readonly ConcurrentDictionary<string, UserPreference> _store = new();

    public Task<UserPreference> GetOrCreateAsync(string userId, CancellationToken ct = default)
    {
        var pref = _store.GetOrAdd(userId, id => new UserPreference { UserId = id });
        return Task.FromResult(pref);
    }

    public Task SaveAsync(UserPreference preference, CancellationToken ct = default)
    {
        _store[preference.UserId] = preference;
        return Task.CompletedTask;
    }
}
