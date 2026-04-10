using System.Collections.Concurrent;
using DevChops.Application.Interfaces;
using DevChops.Domain.Entities;

namespace DevChops.Infrastructure.InMemory;

public class InMemorySavedFilterService : ISavedFilterService
{
    private readonly ConcurrentDictionary<string, List<SavedFilter>> _store = new();

    public Task<IReadOnlyList<SavedFilter>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        var filters = _store.GetOrAdd(userId, _ => []);
        return Task.FromResult<IReadOnlyList<SavedFilter>>(filters.AsReadOnly());
    }

    public Task SaveAsync(SavedFilter filter, CancellationToken ct = default)
    {
        var list = _store.GetOrAdd(filter.UserId, _ => []);
        lock (list)
        {
            var idx = list.FindIndex(f => f.Id == filter.Id);
            if (idx >= 0) list[idx] = filter;
            else list.Add(filter);
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string userId, Guid filterId, CancellationToken ct = default)
    {
        if (_store.TryGetValue(userId, out var list))
            lock (list) { list.RemoveAll(f => f.Id == filterId); }
        return Task.CompletedTask;
    }
}
