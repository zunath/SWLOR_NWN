using System.Text.RegularExpressions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A fake <see cref="IDBService"/> for unit tests of converted services: entities live in a dictionary by Id, and every
/// <see cref="Set{T}"/> is recorded in order. <see cref="Get{T}"/> returns the stored instance (like the real database
/// within a frame) or null when nothing is stored, as <c>DB.Get</c> does.
/// </summary>
/// <remarks>
/// By default a search cannot evaluate a query: <see cref="Search{T}"/> and <see cref="SearchCount{T}"/> answer with
/// every stored entity of the searched type, so a test stores only the entities the query would match and asserts on
/// the recorded <see cref="Queries"/> to prove what was asked. A test whose subject asks several different questions of
/// one entity type (a boot-time sweep over cities, then over their citizens) sets <see cref="EvaluateQueries"/>: the
/// field filters of the query are then applied to the stored entities.
/// </remarks>
public sealed class InMemoryDBService : IDBService
{
    private static readonly Regex FilterPattern = new(@"@(?<field>\w+):(?<value>\S+)", RegexOptions.Compiled);

    private readonly Dictionary<string, EntityBase> _entities = new(StringComparer.Ordinal);

    /// <summary>
    /// When true, a search applies the field filters of its query (the <c>@Field:value</c> terms of the Redis query text:
    /// strings compared exactly, numbers, booleans, enums, and lists separated by <c>|</c>) to the stored entities of
    /// the searched type. A filter on a field the entity does not have, or whose type is not one of those, throws.
    /// Paging and sorting are ignored.
    /// </summary>
    public bool EvaluateQueries { get; set; }

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

    public bool Exists<T>(string id)
        where T : EntityBase
    {
        return _entities.ContainsKey(id);
    }

    public void Delete<T>(string id)
        where T : EntityBase
    {
        _entities.Remove(id);
        Deleted.Add(id);
    }

    public IEnumerable<T> Search<T>(DBQuery<T> query)
        where T : EntityBase
    {
        Queries.Add(query);
        return Matching(query);
    }

    public long SearchCount<T>(DBQuery<T> query)
        where T : EntityBase
    {
        Queries.Add(query);
        return Matching(query).Count;
    }

    private List<T> Matching<T>(DBQuery<T> query)
        where T : EntityBase
    {
        var candidates = _entities.Values.OfType<T>();
        if (!EvaluateQueries)
            return candidates.ToList();

        var filters = FilterPattern.Matches(query.BuildQuery().QueryString)
            .Select(match => (Field: match.Groups["field"].Value, Values: match.Groups["value"].Value.Split('|')))
            .Where(filter => filter.Field != nameof(EntityBase.EntityType))
            .ToList();

        return candidates.Where(entity => filters.All(filter => Matches(entity, filter.Field, filter.Values))).ToList();
    }

    private static bool Matches(EntityBase entity, string field, string[] wanted)
    {
        var property = entity.GetType().GetProperty(field)
            ?? throw new NotSupportedException($"{entity.GetType().Name} has no field {field} to filter on.");

        var text = property.GetValue(entity) switch
        {
            null => string.Empty,
            bool flag => flag ? "1" : "0",
            Enum value => Convert.ToInt32(value).ToString(),
            int number => number.ToString(),
            string value => DB.EscapeTokens(value),
            var other => throw new NotSupportedException($"{entity.GetType().Name}.{field} is a {other.GetType().Name}, which a query filter cannot compare.")
        };

        return wanted.Contains(text, StringComparer.Ordinal);
    }
}
