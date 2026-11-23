using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>Logs the order of reads and writes through a callback next to the messages a test records, then answers from the in-memory store.</summary>
public sealed class LoggingDB : IDBService
{
    private readonly InMemoryDBService _inner;
    private readonly Action<string> _mark;

    public LoggingDB(InMemoryDBService inner, Action<string> mark)
    {
        _inner = inner;
        _mark = mark;
    }

    public T Get<T>(string id) where T : EntityBase
    {
        _mark("db.get");
        return _inner.Get<T>(id);
    }

    public void Set<T>(T entity) where T : EntityBase
    {
        _mark("db.set");
        _inner.Set(entity);
    }

    public void Delete<T>(string id) where T : EntityBase => _inner.Delete<T>(id);

    public bool Exists<T>(string id) where T : EntityBase => _inner.Exists<T>(id);

    public IEnumerable<T> Search<T>(DBQuery<T> query) where T : EntityBase => _inner.Search(query);

    public long SearchCount<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchCount(query);

    public long GetWriteVersion<T>() where T : EntityBase => _inner.GetWriteVersion<T>();

    public void Load() => _inner.Load();
    public void Connect() => _inner.Connect();
    public void SyncIndexes() => _inner.SyncIndexes();

    public StackExchange.Redis.RedisValue StreamAdd(StackExchange.Redis.RedisKey key,
        StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
        _inner.StreamAdd(key, streamPairs, maxLength, useApproximateMaxLength);

    public string GetRawJson<T>(string id) => _inner.GetRawJson<T>(id);

    public IEnumerable<string> SearchIds<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchIds(query);

    public IEnumerable<string> SearchRawJson<T>(DBQuery<T> query) where T : EntityBase => _inner.SearchRawJson(query);

    public void SetUnindexedJsonProperty<T>(string id, string propertyName, string json) where T : EntityBase =>
        _inner.SetUnindexedJsonProperty<T>(id, propertyName, json);

    public string EscapeTokens(string str) => _inner.EscapeTokens(str);

    public string StringGet(StackExchange.Redis.RedisKey key) => _inner.StringGet(key);

    public Task<StackExchange.Redis.RedisValue> StreamAddAsync(StackExchange.Redis.RedisKey key,
        StackExchange.Redis.NameValueEntry[] streamPairs, int? maxLength = null, bool useApproximateMaxLength = false) =>
        _inner.StreamAddAsync(key, streamPairs, maxLength, useApproximateMaxLength);
}
