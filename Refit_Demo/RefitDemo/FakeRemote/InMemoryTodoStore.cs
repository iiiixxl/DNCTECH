using System.Collections.Concurrent;
using Refit_Demo.RefitDemo.Models;

namespace Refit_Demo.RefitDemo.FakeRemote;

public class InMemoryTodoStore
{
    private readonly ConcurrentDictionary<int, TodoItem> _items = new();
    private int _nextId = 3;

    public InMemoryTodoStore()
    {
        _items[1] = new TodoItem { Id = 1, Title = "Learn Refit", Completed = false, Owner = "admin" };
        _items[2] = new TodoItem { Id = 2, Title = "Call FakeRemote", Completed = true, Owner = "admin" };
    }

    public IReadOnlyList<TodoItem> Query(bool? completed, int? limit)
    {
        IEnumerable<TodoItem> q = _items.Values.OrderBy(x => x.Id);
        if (completed is not null)
        {
            q = q.Where(x => x.Completed == completed);
        }

        if (limit is > 0)
        {
            q = q.Take(limit.Value);
        }

        return q.ToList();
    }

    public TodoItem? Get(int id) => _items.TryGetValue(id, out var item) ? item : null;

    public TodoItem Create(CreateTodoRequest request, string? owner)
    {
        var id = Interlocked.Increment(ref _nextId);
        var item = new TodoItem
        {
            Id = id,
            Title = request.Title,
            Completed = request.Completed,
            Owner = owner
        };
        _items[id] = item;
        return item;
    }

    public IReadOnlyList<TodoItem> GetByOwner(string owner) =>
        _items.Values.Where(x => string.Equals(x.Owner, owner, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Id)
            .ToList();
}
