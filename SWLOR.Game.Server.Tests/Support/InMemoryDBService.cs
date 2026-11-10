using StackExchange.Redis;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

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
    public void Load() => throw UnsupportedDBMember.For(nameof(Load));

    public RedisValue StreamAdd(RedisKey key, NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
        throw UnsupportedDBMember.For(nameof(StreamAdd));

    public string GetRawJson<T>(string id) => throw UnsupportedDBMember.For(nameof(GetRawJson));

    public IEnumerable<string> SearchIds<T>(DBQuery<T> query)
        where T : EntityBase =>
        throw UnsupportedDBMember.For(nameof(SearchIds));

    public IEnumerable<string> SearchRawJson<T>(DBQuery<T> query)
        where T : EntityBase =>
        throw UnsupportedDBMember.For(nameof(SearchRawJson));

    public void SetUnindexedJsonProperty<T>(string id, string propertyName, string json)
        where T : EntityBase =>
        throw UnsupportedDBMember.For(nameof(SetUnindexedJsonProperty));

    public string EscapeTokens(string str) => throw UnsupportedDBMember.For(nameof(EscapeTokens));

    private readonly Dictionary<string, EntityBase> _entities = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, long> _writeVersions = new();

    /// <summary>The entities passed to <see cref="Set{T}"/>, in call order.</summary>
    public List<EntityBase> Saved { get; } = new();

    /// <summary>The ids passed to <see cref="Get{T}"/>, in call order.</summary>
    public List<string> Reads { get; } = new();

    /// <summary>The ids passed to <see cref="Delete{T}"/>, in call order.</summary>
    public List<string> Deleted { get; } = new();

    /// <summary>The queries passed to <see cref="Search{T}"/> and <see cref="SearchCount{T}"/>, in call order.</summary>
    public List<object> Queries { get; } = new();

    /// <summary>The stream entries passed to <see cref="StreamAddAsync"/>, in call order.</summary>
    public List<StreamAddCall> StreamAdds { get; } = new();

    /// <summary>When set, <see cref="StreamAddAsync"/> returns a task that faults with this exception.</summary>
    public Exception? StreamAddFailure { get; set; }

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
        _writeVersions[typeof(T)] = GetWriteVersion<T>() + 1;
    }

    public void Delete<T>(string id)
        where T : EntityBase
    {
        _entities.Remove(id);
        Deleted.Add(id);
        _writeVersions[typeof(T)] = GetWriteVersion<T>() + 1;
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

    public long GetWriteVersion<T>()
        where T : EntityBase =>
        _writeVersions.TryGetValue(typeof(T), out var version) ? version : 0;

    public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false)
    {
        StreamAdds.Add(new StreamAddCall(key.ToString(), streamPairs, maxLength, useApproximateMaxLength));
        return StreamAddFailure == null
            ? Task.FromResult((RedisValue)"1-0")
            : Task.FromException<RedisValue>(StreamAddFailure);
    }
}

/// <summary>One call of <see cref="IDBService.StreamAddAsync"/>.</summary>
public sealed record StreamAddCall(string Key, NameValueEntry[] Entries, int? MaxLength, bool UseApproximateMaxLength);
