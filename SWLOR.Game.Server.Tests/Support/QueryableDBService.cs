using System.Globalization;
using StackExchange.Redis;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A fake <see cref="IDBService"/> like <see cref="InMemoryDBService"/> (it stores the entities and records every call
/// in <see cref="Store"/>) that also answers a search with the entities the query matches, for tests of code that
/// runs several searches over the same entity type (the contract board boot sweep).
/// </summary>
/// <remarks>
/// It evaluates only what the quest contract queries use, on the query text <c>DBQuery</c> builds: an exact value
/// (<c>@Status:3</c>, <c>@PlayerId:uuid\-1</c>), a list of numbers (<c>@Status:3|4</c>) and an inclusive numeric
/// range (<c>@SettlementPending:[1 1]</c>), each compared with the entity property of that name. Paging is ignored.
/// </remarks>
public sealed class QueryableDBService : IDBService
{
    public void Load() => throw UnsupportedDBMember.For(nameof(Load));
    public void Connect() => throw UnsupportedDBMember.For(nameof(Connect));
    public void SyncIndexes() => throw UnsupportedDBMember.For(nameof(SyncIndexes));

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

    /// <summary>The storage and the record of reads, saves, deletes and queries.</summary>
    public InMemoryDBService Store { get; } = new();

    /// <summary>Stores an entity without recording a save, to set the scene.</summary>
    public QueryableDBService With(EntityBase entity)
    {
        Store.With(entity);
        return this;
    }

    public T Get<T>(string id)
        where T : EntityBase => Store.Get<T>(id);

    public void Set<T>(T entity)
        where T : EntityBase => Store.Set(entity);

    public void Delete<T>(string id)
        where T : EntityBase => Store.Delete<T>(id);

    public bool Exists<T>(string id)
        where T : EntityBase => Store.Exists<T>(id);

    public long GetWriteVersion<T>()
        where T : EntityBase =>
        Store.GetWriteVersion<T>();

    public Task<RedisValue> StreamAddAsync(RedisKey key, NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
        Store.StreamAddAsync(key, streamPairs, maxLength, useApproximateMaxLength);

    public IEnumerable<T> Search<T>(DBQuery<T> query)
        where T : EntityBase
    {
        var text = query.BuildQuery().QueryString;
        return Store.Search(query).Where(entity => Matches(entity, text)).ToList();
    }

    public long SearchCount<T>(DBQuery<T> query)
        where T : EntityBase
    {
        var text = query.BuildQuery().QueryString;
        return Store.Search(query).Count(entity => Matches(entity, text));
    }

    private static bool Matches(EntityBase entity, string queryText)
    {
        foreach (var clause in queryText.Split(" @", StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var colon = clause.IndexOf(':');
            var name = clause[..colon];
            var criteria = clause[(colon + 1)..];
            var property = entity.GetType().GetProperty(name)
                ?? throw new InvalidOperationException($"{entity.GetType().Name} has no property {name}.");

            if (!Matches(Normalize(property.GetValue(entity)), criteria))
                return false;
        }

        return true;
    }

    private static string Normalize(object? value) => value switch
    {
        null => string.Empty,
        Enum flag => Convert.ToInt32(flag, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
        bool flag => flag ? "1" : "0",
        var other => Convert.ToString(other, CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static bool Matches(string actual, string criteria)
    {
        if (criteria.StartsWith('[') && criteria.EndsWith(']'))
        {
            var bounds = criteria[1..^1].Split(' ').Select(double.Parse).ToArray();
            var number = double.Parse(actual, CultureInfo.InvariantCulture);
            return number >= bounds[0] && number <= bounds[1];
        }

        return criteria.Split('|').Any(option => option.Replace("\\", string.Empty) == actual);
    }
}
