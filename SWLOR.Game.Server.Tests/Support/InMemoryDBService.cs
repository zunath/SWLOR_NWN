using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A fake <see cref="IDBService"/> for unit tests of converted services: entities live in a dictionary by Id, and every
/// <see cref="Set{T}"/> is recorded in order. <see cref="Get{T}"/> returns the stored instance (like the real database
/// within a frame) or null when nothing is stored, as <c>DB.Get</c> does.
/// </summary>
/// <remarks>
/// A search cannot evaluate a query: <see cref="Search{T}"/> and <see cref="SearchCount{T}"/> answer with every stored
/// entity of the searched type, so a test stores only the entities the query would match and asserts on the recorded
/// <see cref="Queries"/> to prove what was asked.
/// </remarks>
public sealed class InMemoryDBService : IDBService
{
    private readonly Dictionary<string, EntityBase> _entities = new(StringComparer.Ordinal);

    /// <summary>The entities passed to <see cref="Set{T}"/>, in call order.</summary>
    public List<EntityBase> Saved { get; } = new();

    /// <summary>The ids passed to <see cref="Get{T}"/>, in call order.</summary>
    public List<string> Reads { get; } = new();

    /// <summary>The ids passed to <see cref="Delete{T}"/>, in call order.</summary>
    public List<string> Deleted { get; } = new();

    /// <summary>The queries passed to <see cref="Search{T}"/> and <see cref="SearchCount{T}"/>, in call order.</summary>
    public List<object> Queries { get; } = new();

    /// <summary>Stores an entity without recording a save, to set the scene.</summary>
    public InMemoryDBService With(EntityBase entity)
    {
        _entities[entity.Id] = entity;
        return this;
    }

    public T Get<T>(string id)
        where T : EntityBase
    {
        Reads.Add(id);
        return _entities.TryGetValue(id, out var entity) ? (T)entity : null;
    }

    public void Set<T>(T entity)
        where T : EntityBase
    {
        _entities[entity.Id] = entity;
        Saved.Add(entity);
    }

    public void Delete<T>(string id)
        where T : EntityBase
    {
        _entities.Remove(id);
        Deleted.Add(id);
    }

    public bool Exists<T>(string id)
        where T : EntityBase
    {
        return _entities.TryGetValue(id, out var entity) && entity is T;
    }

    public IEnumerable<T> Search<T>(DBQuery<T> query)
        where T : EntityBase
    {
        Queries.Add(query);
        return _entities.Values.OfType<T>().ToList();
    }

    public long SearchCount<T>(DBQuery<T> query)
        where T : EntityBase
    {
        Queries.Add(query);
        return _entities.Values.OfType<T>().Count();
    }
}
