using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The runtime state a hot reload carries (<see cref="ReloadStateCarrier"/>, P4-15): every <see cref="ReloadStateKind.Carried"/> field
/// can be written and read back to the same content, shared objects stay shared, collections are refilled in place, a failure in one
/// class leaves the others carried, and what cannot cross a reload (a delegate, a type of another assembly) is refused.
/// </summary>
[TestFixture]
public class ReloadStateCarrierTests
{
    private static readonly Assembly Game = ScriptHandlerTestSupport.GameAssembly;

    private static ReloadStateCarrier Carrier() => new(Game, new JsonConverter[]
    {
        new AbilityDetailReloadConverter(() => new Dictionary<FeatType, AbilityDetail>()),
    });

    private static IEnumerable<TestCaseData> CarriedClasses()
    {
        return ReloadStateCarrier.CarriedClasses(Game).Select(entry => new TestCaseData(entry.Type).SetArgDisplayNames(entry.Type.Name));
    }

    [Test]
    public void TheCarriedClassesAreTheReviewedOnes()
    {
        ReloadStateCarrier.CarriedClasses(Game).Select(entry => entry.Type.Name).Should().Contain(new[]
        {
            nameof(StatusEffectService), nameof(PartyService), nameof(AbilityService), "CombatActivityService", "CombatPointService",
            "EnmityService", nameof(PlayerNameService), "PvpCombatService", "TemporaryStatModifierService", nameof(ShuttleService),
        });
    }

    [Test]
    public void EveryCarriedFieldHasACarriableType()
    {
        var problems = ReloadStateCarrier.CarriedClasses(Game)
            .SelectMany(entry => entry.Fields)
            .SelectMany(field => ReloadStateCarrier.Problems(field.FieldType, Game, type => type == typeof(AbilityDetail))
                .Select(problem => $"{field.DeclaringType!.FullName}.{field.Name}: {problem}"))
            .ToArray();

        problems.Should().BeEmpty("a carried field must hold data only: no delegates, no object-typed members, no foreign objects");
    }

    [TestCaseSource(nameof(CarriedClasses))]
    public void EachCarriedClassComesBackWithTheSameContent(Type type)
    {
        var fields = ReloadStateCarrier.CarriedClasses(Game).Single(entry => entry.Type == type).Fields;
        var saved = fields.Where(field => field.IsStatic).ToDictionary(field => field, field => field.GetValue(null));
        try
        {
            var before = ReloadCarrySamples.Owner(type, fields);
            foreach (var field in fields)
                SetSample(field, before, ReloadCarrySamples.Create(field.FieldType));
            var hasher = new ReloadDigestHasher(Game);
            var expected = fields.ToDictionary(field => field.Name, field => hasher.Hash(field.GetValue(field.IsStatic ? null : before)));

            var text = Carrier().Write(fields, before);

            var after = ReloadCarrySamples.Owner(type, fields);
            foreach (var field in fields)
                SetSample(field, after, ReloadCarrySamples.EmptyCollection(field.FieldType));
            Carrier().Read(fields, after, text);

            foreach (var field in fields)
                hasher.Hash(field.GetValue(field.IsStatic ? null : after)).Should().Be(expected[field.Name], $"{type.Name}.{field.Name} is carried as it was");
        }
        finally
        {
            foreach (var (field, value) in saved)
                SetSample(field, null, value);
        }
    }

    [Test]
    public void StatusEffectsKeepTheirTypeFieldsAndSharedInstances()
    {
        var fields = FieldsOf(typeof(StatusEffectService));
        var service = ReloadCarrySamples.Owner(typeof(StatusEffectService), fields);
        var effects = new CreatureStatusEffect();
        var venom = new VenomStatusEffect();
        venom.RestartDuration(5);
        effects.Add(venom);
        var guard = new GuardedStatusEffect();
        guard.RestartDuration(-1);
        effects.Add(guard);
        SetField(service, "_creatureEffects", new Dictionary<uint, CreatureStatusEffect> { [42u] = effects });
        SetField(service, "_loggedOutPlayerEffects", Activator.CreateInstance(FieldOf(typeof(StatusEffectService), "_loggedOutPlayerEffects").FieldType));

        var text = Carrier().Write(fields, service);
        var copy = ReloadCarrySamples.Owner(typeof(StatusEffectService), fields);
        SetField(copy, "_creatureEffects", new Dictionary<uint, CreatureStatusEffect>());
        Carrier().Read(fields, copy, text);

        var carried = ((Dictionary<uint, CreatureStatusEffect>)FieldOf(typeof(StatusEffectService), "_creatureEffects").GetValue(copy))[42u];
        var all = carried.GetAllEffects();
        all.Select(effect => effect.GetType()).Should().BeEquivalentTo(new[] { typeof(VenomStatusEffect), typeof(GuardedStatusEffect) });
        all.Select(effect => effect.Id).Should().BeEquivalentTo(new[] { venom.Id, guard.Id }, "the native effect tags name the status effect ids");
        carried.GetAllTickEffects().Should().OnlyContain(effect => all.Contains(effect), "an effect in two sets is one object again");
        all.OfType<VenomStatusEffect>().Single().DurationTicks.Should().Be(5);
        all.OfType<GuardedStatusEffect>().Single().DurationTicks.Should().Be(-1);
    }

    [Test]
    public void ACollectionIsRefilledInPlaceAndTimesKeepTheirKind()
    {
        var fields = FieldsOf(typeof(MusicService));
        var music = ReloadCarrySamples.Owner(typeof(MusicService), fields);
        SetField(music, "_lastBattleThemeCombat", new Dictionary<uint, DateTime> { [9u] = ReloadCarrySamples.SampleTime });
        var text = Carrier().Write(fields, music);

        var copy = ReloadCarrySamples.Owner(typeof(MusicService), fields);
        var existing = new Dictionary<uint, DateTime> { [1u] = DateTime.MinValue };
        SetField(copy, "_lastBattleThemeCombat", existing);
        Carrier().Read(fields, copy, text);

        FieldOf(typeof(MusicService), "_lastBattleThemeCombat").GetValue(copy).Should().BeSameAs(existing);
        existing.Should().Equal(new Dictionary<uint, DateTime> { [9u] = ReloadCarrySamples.SampleTime });
        existing[9u].Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void ADictionaryWithATupleKeyIsWrittenAsPairs()
    {
        var converter = new ReloadCarryPairDictionaryConverter();
        var value = new Dictionary<(uint, uint), DateTime> { [(1u, 2u)] = ReloadCarrySamples.SampleTime };
        var settings = new JsonSerializerSettings { Converters = { converter }, DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind };

        var text = JsonConvert.SerializeObject(value, settings);
        text.Should().StartWith("[[");
        JsonConvert.DeserializeObject<Dictionary<(uint, uint), DateTime>>(text, settings).Should().Equal(value);
        converter.CanConvert(typeof(Dictionary<uint, int>)).Should().BeFalse();
    }

    [Test]
    public void ADelegateAndAForeignTypeAreRefused()
    {
        ReloadStateCarrier.Problems(typeof(Dictionary<uint, Action>), Game).Should().ContainSingle().Which.Should().Contain("delegate");
        ReloadStateCarrier.Problems(typeof(List<object>), Game).Should().ContainSingle().Which.Should().Contain("object");
        ReloadStateCarrier.Problems(typeof(Dictionary<uint, System.Text.StringBuilder>), Game).Should().ContainSingle()
            .Which.Should().Contain("StringBuilder");
        ReloadStateCarrier.Problems(typeof(Dictionary<(uint, uint), List<DateTime>>), Game).Should().BeEmpty();

        var binder = new ReloadCarryBinder(Game);
        binder.Invoking(b => b.BindToName(typeof(System.Text.StringBuilder), out _, out _)).Should().Throw<JsonSerializationException>();
        binder.Invoking(b => b.BindToType(null, "System.Text.StringBuilder")).Should().Throw<JsonSerializationException>();
        binder.BindToType(null, typeof(VenomStatusEffect).FullName).Should().Be(typeof(VenomStatusEffect));
    }

    [Test]
    public void CarryAllPutsEachClassAndAdoptAllTakesItBackAndStartsTimers()
    {
        // An adopter (HoloComService) looks for the players online; there are none.
        using var engine = NoPlayersOnline();
        var store = new Dictionary<string, string>();
        var owners = new Dictionary<Type, object>();
        object Resolve(Type type) => owners.TryGetValue(type, out var owner) ? owner : null;
        foreach (var (type, fields) in ReloadStateCarrier.CarriedClasses(Game))
        {
            var owner = ReloadCarrySamples.Owner(type, fields);
            if (owner != null)
            {
                foreach (var field in fields)
                    SetSample(field, owner, ReloadCarrySamples.EmptyCollection(field.FieldType) ?? ReloadCarrySamples.Create(field.FieldType));
                owners[type] = owner;
            }
        }

        var names = owners[typeof(PlayerNameService)];
        FieldOf(typeof(PlayerNameService), "AfkPlayers").SetValue(names,
            new Dictionary<uint, (uint Area, System.Numerics.Vector3 Position)> { [5u] = (6u, new System.Numerics.Vector3(1, 2, 3)) });

        Carrier().CarryAll(Resolve, (key, text) => store[key] = text);
        store.Keys.Should().BeEquivalentTo(ReloadStateCarrier.CarriedClasses(Game).Select(entry => ReloadStateCarrier.KeyPrefix + entry.Type.FullName));

        var fresh = ReloadCarrySamples.Owner(typeof(PlayerNameService), FieldsOf(typeof(PlayerNameService)));
        FieldOf(typeof(PlayerNameService), "AfkPlayers").SetValue(fresh, new Dictionary<uint, (uint Area, System.Numerics.Vector3 Position)>());
        owners[typeof(PlayerNameService)] = fresh;
        var taken = new List<string>();
        Carrier().Invoking(carrier => carrier.AdoptAll(Resolve, key =>
        {
            taken.Add(key);
            return store.TryGetValue(key, out var text) ? text : null;
        })).Should().NotThrow();
        taken.Should().BeEquivalentTo(store.Keys);

        ((PlayerNameService)fresh).IsAfk(5u).Should().BeTrue("the AFK players are carried");
        var timer = (IDisposable)FieldOf(typeof(PlayerNameService), "_afkMovementCheck").GetValue(fresh);
        timer.Should().NotBeNull("the adopter hook starts the movement check the old build's timer ran");
        timer!.Dispose();
    }

    [Test]
    public void AClassThatFailsIsNamedAndTheOthersStillCarry()
    {
        var store = new Dictionary<string, string>();
        object Resolve(Type type) => type == typeof(PartyService) ? null : ReloadCarrySamples.Owner(type, FieldsOf(type));

        Carrier().Invoking(carrier => carrier.CarryAll(Resolve, (key, text) => store[key] = text))
            .Should().Throw<InvalidOperationException>().WithMessage("*PartyService*");
        store.Should().ContainKey(ReloadStateCarrier.KeyPrefix + typeof(MusicService).FullName);
        store.Should().NotContainKey(ReloadStateCarrier.KeyPrefix + typeof(PartyService).FullName);
    }

    [Test]
    public void AClassTheOldBuildLeftNothingForKeepsWhatTheNewBuildMade()
    {
        // An adopter (HoloComService) looks for the players online; there are none.
        using var engine = NoPlayersOnline();
        var store = new Dictionary<string, string>();
        object Resolve(Type type)
        {
            // A service as the container builds it: every carried collection exists and is empty.
            var owner = ReloadCarrySamples.Owner(type, FieldsOf(type));
            foreach (var field in FieldsOf(type).Where(field => !field.IsStatic))
                field.SetValue(owner, ReloadCarrySamples.EmptyCollection(field.FieldType) ?? ReloadCarrySamples.Create(field.FieldType));
            return owner;
        }

        Carrier().CarryAll(Resolve, (key, text) => store[key] = text);
        store.Remove(ReloadStateCarrier.KeyPrefix + typeof(MusicService).FullName);

        var music = ReloadCarrySamples.Owner(typeof(MusicService), FieldsOf(typeof(MusicService)));
        var kept = new Dictionary<uint, DateTime> { [3u] = ReloadCarrySamples.SampleTime };
        SetField(music, "_lastBattleThemeCombat", kept);
        Carrier().Invoking(carrier => carrier.AdoptAll(type => type == typeof(MusicService) ? music : Resolve(type),
            key => store.TryGetValue(key, out var text) ? text : null)).Should().NotThrow();
        kept.Should().ContainKey(3u);
    }

    [Test]
    public void TheRangedRollIdsGoOnFromTheOldBuildSoTheCarriedHasteHistoryAcceptsNewHits()
    {
        var type = typeof(SWLOR.Game.Server.Native.RangedWeaponAttackContext);
        var counter = type.GetField("_nextRoll", BindingFlags.NonPublic | BindingFlags.Static)!;
        var fields = ReloadStateCarrier.CarriedClasses(Game).Single(entry => entry.Type == type).Fields;
        fields.Should().ContainSingle().Which.Should().BeSameAs(counter);
        var saved = counter.GetValue(null);
        try
        {
            // The old build recorded hit 5 in a haste history it carries.
            var configuration = new SWLOR.Game.Server.Domain.Combat.OrdinaryAttackHasteConfiguration(10, 3, 0, 30);
            var haste = new SWLOR.Game.Server.Domain.Combat.OrdinaryAttackHasteState();
            counter.SetValue(null, 4L);
            var oldHit = new SWLOR.Game.Server.Native.RangedWeaponAttackContext.Roll();
            haste.RecordHit(ReloadCarrySamples.SampleTime, configuration, 77u, oldHit.Id).Should().BeTrue();
            var text = Carrier().Write(fields, null);

            // The new build's counter starts again at 0 until the carry puts it back.
            counter.SetValue(null, 0L);
            Carrier().Read(fields, null, text);
            var newHit = new SWLOR.Game.Server.Native.RangedWeaponAttackContext.Roll();

            newHit.Id.Should().BeGreaterThan(oldHit.Id);
            haste.RecordHit(ReloadCarrySamples.SampleTime.AddSeconds(1), configuration, 77u, newHit.Id).Should().BeTrue(
                "a new hit's id never repeats one the carried history holds");
        }
        finally
        {
            counter.SetValue(null, saved);
        }
    }

    [Test]
    public void TheHoloComTokensAndStealthWindowsGoOnFromTheOldBuild()
    {
        ReloadStateCarrier.CarriedClasses(Game).Should().Contain(entry =>
            entry.Type.Name == "HoloComService" && entry.Fields.Any(field => field.Name == "_nextCallToken"),
            "a ringing attempt's token is kept in engine locals, so new tokens must not repeat it");
        ReloadStateCarrier.CarriedClasses(Game).Should().Contain(entry =>
            entry.Type.Name == "StealthService" && entry.Fields.Any(field => field.Name == "_lastTimedExitGeneration"),
            "a stealth window's generation is kept in engine locals, so new windows must not repeat it");
    }

    [Test]
    public void TheQuiescersEndTheWorkInFlightOfEveryDiscoveredClassAndNameTheOnesThatFail()
    {
        ReloadQuiescers.Discover(Game).Select(type => type.Name).Should().Contain(new[]
        {
            nameof(AbilityService), "ItemService", "FishingService", "StealthService",
        });

        var called = new List<Type>();
        var players = new uint[] { 9 };
        object Resolve(Type type) => type == typeof(AbilityService) ? null : new RecordingQuiescer(type, called, players);

        var run = () => ReloadQuiescers.Run(Game, Resolve, players);

        run.Should().Throw<InvalidOperationException>().WithMessage("*AbilityService*", "a class the container cannot give is named");
        called.Should().HaveCount(ReloadQuiescers.Discover(Game).Count - 1, "every other class still ends its work");
    }

    private sealed class RecordingQuiescer : IReloadQuiescer
    {
        private readonly Type _type;
        private readonly List<Type> _called;
        private readonly IReadOnlyList<uint> _players;

        public RecordingQuiescer(Type type, List<Type> called, IReadOnlyList<uint> players)
        {
            _type = type;
            _called = called;
            _players = players;
        }

        public void QuiesceForReload(IReadOnlyList<uint> players)
        {
            players.Should().BeSameAs(_players);
            _called.Add(_type);
        }
    }

    [Test]
    public void TheAbilityConverterWritesTheFeatAndReadsTheNewBuildsDefinition()
    {
        var oldDetail = new AbilityDetail();
        var newDetail = new AbilityDetail();
        var write = new AbilityDetailReloadConverter(() => new Dictionary<FeatType, AbilityDetail> { [FeatType.PowerAttack] = oldDetail });
        var read = new AbilityDetailReloadConverter(() => new Dictionary<FeatType, AbilityDetail> { [FeatType.PowerAttack] = newDetail });

        var text = JsonConvert.SerializeObject(new[] { oldDetail, new AbilityDetail() }, new JsonSerializerSettings { Converters = { write } });
        text.Should().Be("[\"PowerAttack\",null]", "a definition the cache does not hold is written as null");
        JsonConvert.DeserializeObject<AbilityDetail[]>(text, new JsonSerializerSettings { Converters = { read } })
            .Should().Equal(newDetail, null);
    }

    private static PlayerEngine NoPlayersOnline()
    {
        var engine = new PlayerEngine();
        engine.Engine.SetReturn("GetFirstPC", 0x7F000000u);
        engine.Engine.SetReturn("GetNextPC", 0x7F000000u);
        engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != 0x7F000000u);
        return engine;
    }

    private static IReadOnlyList<FieldInfo> FieldsOf(Type type)
    {
        return ReloadStateCarrier.CarriedClasses(type.Assembly).SingleOrDefault(entry => entry.Type == type).Fields
               ?? type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                   .Where(field => ReloadDigestHasher.StateOf(field) == ReloadStateKind.Carried).ToArray();
    }

    private static FieldInfo FieldOf(Type type, string name)
    {
        return type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
    }

    private static void SetField(object owner, string name, object value)
    {
        FieldOf(owner.GetType(), name).SetValue(owner, value);
    }

    /// <summary>Sets a field's value; a static readonly collection is refilled instead, as the carrier does.</summary>
    private static void SetSample(FieldInfo field, object owner, object value)
    {
        if (field.IsStatic && field.IsInitOnly)
        {
            var current = (System.Collections.IDictionary)field.GetValue(null)!;
            current.Clear();
            if (value is System.Collections.IDictionary entries)
            {
                foreach (System.Collections.DictionaryEntry entry in entries)
                    current[entry.Key] = entry.Value;
            }

            return;
        }

        field.SetValue(owner, value);
    }
}
