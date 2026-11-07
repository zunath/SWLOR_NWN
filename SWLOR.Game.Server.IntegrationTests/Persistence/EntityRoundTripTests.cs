using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.IntegrationTests.Support;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.IntegrationTests.Persistence;

/// <summary>
/// For every entity type the database stores: a populated instance survives Set, Get, a search on each of its indexed
/// properties, Delete, and a reload of the indexes through the real <c>DB.Load</c>.
/// </summary>
[TestFixture]
public class EntityRoundTripTests : IntegrationTestBase
{
    public static IEnumerable<TestCaseData> EntityTypes() =>
        EntityFactory.EntityTypes.Select(type => new TestCaseData(type).SetName($"{{m}}({type.Name})"));

    private static readonly MethodInfo RoundTripMethod =
        typeof(EntityRoundTripTests).GetMethod(nameof(RoundTrip), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly MethodInfo IndexedSearchMethod =
        typeof(EntityRoundTripTests).GetMethod(nameof(SearchEachIndexedProperty), BindingFlags.NonPublic | BindingFlags.Instance)!;

    [Test]
    public void EveryEntityTypeIsCoveredByReflection()
    {
        EntityFactory.EntityTypes.Should().HaveCountGreaterThan(25);
        EntityFactory.EntityTypes.Should().Contain(typeof(Player)).And.Contain(typeof(InventoryItem)).And.Contain(typeof(ServerConfiguration));
    }

    [TestCaseSource(nameof(EntityTypes))]
    public void SetThenGet_ReturnsTheStoredEntityAfterTheFrameEnds(Type type)
    {
        Call(RoundTripMethod.MakeGenericMethod(type), "round-trip-" + type.Name);
    }

    [TestCaseSource(nameof(EntityTypes))]
    public void EachIndexedPropertyFindsTheEntity(Type type)
    {
        Call(IndexedSearchMethod.MakeGenericMethod(type), "indexed-" + type.Name, null, true);
    }

    [TestCaseSource(nameof(EntityTypes))]
    public void DeleteRemovesTheRecordAndItsSearchEntry(Type type)
    {
        var entity = EntityFactory.Build(type, "delete-" + type.Name);
        typeof(IDBService).GetMethod(nameof(IDBService.Set))!.MakeGenericMethod(type).Invoke(Db, new object[] { entity });
        Redis.KeyExists($"{type.Name}:{entity.Id}").Should().BeTrue();
        Redis.KeyExists($"Index:{type.Name}:{entity.Id}").Should().BeTrue("Set writes the search hash beside the JSON");

        typeof(IDBService).GetMethod(nameof(IDBService.Delete))!.MakeGenericMethod(type).Invoke(Db, new object[] { entity.Id });
        Db.EndFrame();

        Redis.KeyExists($"{type.Name}:{entity.Id}").Should().BeFalse();
        Redis.KeyExists($"Index:{type.Name}:{entity.Id}").Should().BeFalse();
        typeof(IDBService).GetMethod(nameof(IDBService.Exists))!.MakeGenericMethod(type).Invoke(Db, new object[] { entity.Id })
            .Should().Be(false);
    }

    [Test]
    public void ReloadingTheIndexes_KeepsEveryEntityTypeSearchable()
    {
        foreach (var type in EntityFactory.EntityTypes)
        {
            var entity = EntityFactory.Build(type, "reload-" + type.Name);
            typeof(IDBService).GetMethod(nameof(IDBService.Set))!.MakeGenericMethod(type).Invoke(Db, new object[] { entity });
        }

        // A second DB.Load against the populated database, as a server restart does.
        var reloaded = new IntegrationDBService();
        reloaded.Load();

        foreach (var type in EntityFactory.EntityTypes)
            Call(IndexedSearchMethod.MakeGenericMethod(type), "reload-" + type.Name, reloaded, false);
    }

    private void Call(MethodInfo method, params object?[] arguments)
    {
        try
        {
            method.Invoke(this, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
        }
    }

    private void RoundTrip<T>(string id)
        where T : EntityBase
    {
        var entity = EntityFactory.Build<T>(id);
        var expected = EntityFactory.Json(entity);

        Db.Set(entity);
        Db.GetRawJson<T>(id).Should().Be(expected, "the stored JSON is the serialized entity");

        // Within the frame Get serves the instance that was set; the next frame reads Redis.
        Db.Get<T>(id).Should().BeSameAs(entity);
        Db.EndFrame();

        var loaded = Db.Get<T>(id);
        loaded.Should().NotBeNull();
        loaded.Should().NotBeSameAs(entity);
        EntityFactory.Json(loaded).Should().Be(expected, "Get returns what was stored");
        Db.Exists<T>(id).Should().BeTrue();

        // A second Set replaces the first; one record, one search entry.
        entity.DateCreated = entity.DateCreated.AddDays(1);
        Db.Set(entity);
        Db.EndFrame();
        EntityFactory.Json(Db.Get<T>(id)).Should().Be(EntityFactory.Json(entity));
        Db.SearchCount(new DBQuery<T>().AddFieldSearch(nameof(EntityBase.Id), id, false)).Should().Be(1);
    }

    private void SearchEachIndexedProperty<T>(string id, IDBService? db = null, bool store = true)
        where T : EntityBase
    {
        db ??= Db;
        var entity = EntityFactory.Build<T>(id);
        if (store)
            db.Set(entity);

        var indexed = typeof(T).GetProperties().Where(p => p.GetCustomAttribute<IndexedAttribute>() != null).ToList();
        indexed.Should().Contain(p => p.Name == nameof(EntityBase.Id));

        foreach (var property in indexed)
        {
            var value = property.GetValue(entity);
            if (value == null || (value is string text && text.Length == 0))
                continue;

            var query = new DBQuery<T>();
            switch (value)
            {
                case string s: query.AddFieldSearch(property.Name, s, false); break;
                case Guid g: query.AddFieldSearch(property.Name, g.ToString(), false); break;
                case bool b: query.AddFieldSearch(property.Name, b); break;
                case int or long or ulong:
                    // NUMERIC fields are filtered by range and ordered (see QueryTests); the text form of an equality
                    // query does not match them, and no caller uses one. The search hash must hold the number.
                    ((long)Redis.HashGet($"Index:{typeof(T).Name}:{id}", property.Name)).Should().Be(Convert.ToInt64(value), property.Name);
                    continue;
                default:
                    if (value.GetType().IsEnum)
                    {
                        query.AddFieldSearch(property.Name, Convert.ToInt32(value));
                        break;
                    }

                    Assert.Fail($"{typeof(T).Name}.{property.Name} is indexed with an unsupported type {value.GetType().Name}.");
                    break;
            }

            // Always narrowed by Id so entities of other tests cannot match.
            if (property.Name != nameof(EntityBase.Id))
                query.AddFieldSearch(nameof(EntityBase.Id), id, false);
            var ids = db.SearchIds(query).ToList();
            ids.Should().Equal(new[] { id }, $"{typeof(T).Name}.{property.Name} = {value} finds the entity");
            db.SearchCount(query).Should().Be(1, property.Name);
            EntityFactory.Json(db.Search(query).Single()).Should().Be(EntityFactory.Json(entity), property.Name);
        }
    }
}
