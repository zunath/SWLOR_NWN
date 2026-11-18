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
using SWLOR.Game.Server.Tests.Core.CarriedFrom;
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
            nameof(StatusEffectService), nameof(PartyRosterService), nameof(AbilityAuraService), "CombatActivityService", "CombatPointService",
            "EnmityService", nameof(PlayerNameService), "PvpCombatService", "TemporaryStatModifierService", nameof(ShuttleStateService),
        });
    }

    [Test]
    public void EveryCarriedFieldHasACarriableType()
    {
        var problems = ReloadStateCarrier.CarriedClasses(Game)
            .SelectMany(entry => entry.Fields)
            .SelectMany(field => ReloadStateCarrier.Problems(field.FieldType, Game, type => type == typeof(AbilityDetail),
                    ScriptHandlerTestSupport.ServerContainer.IsRegistered)
                .Select(problem => $"{field.DeclaringType!.FullName}.{field.Name}: {problem}"))
            .ToArray();

        // A field of a carried object that holds a service the container registers is left out, as the carry leaves it out
        // (the content factory builds the object with the new build's services); one the registrations miss is checked as data.
        problems.Should().BeEmpty("a carried field must hold data only: no delegates, no object-typed members, no foreign objects, no services");
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
        var fields = FieldsOf(StatusEffectInternals.CoreType);
        var service = ReloadCarrySamples.Owner(StatusEffectInternals.CoreType, fields);
        var effects = new CreatureStatusEffect();
        var venom = new VenomStatusEffect();
        venom.RestartDuration(5);
        effects.Add(venom);
        var guard = new GuardedStatusEffect();
        guard.RestartDuration(-1);
        effects.Add(guard);
        SetField(service, "_creatureEffects", new Dictionary<uint, CreatureStatusEffect> { [42u] = effects });

        var text = Carrier().Write(fields, service);
        var copy = ReloadCarrySamples.Owner(StatusEffectInternals.CoreType, fields);
        SetField(copy, "_creatureEffects", new Dictionary<uint, CreatureStatusEffect>());
        Carrier().Read(fields, copy, text);

        var carried = ((Dictionary<uint, CreatureStatusEffect>)FieldOf(StatusEffectInternals.CoreType, "_creatureEffects").GetValue(copy))[42u];
        var all = carried.GetAllEffects();
        all.Select(effect => effect.GetType()).Should().BeEquivalentTo(new[] { typeof(VenomStatusEffect), typeof(GuardedStatusEffect) });
        all.Select(effect => effect.Id).Should().BeEquivalentTo(new[] { venom.Id, guard.Id }, "the native effect tags name the status effect ids");
        carried.GetAllTickEffects().Should().OnlyContain(effect => all.Contains(effect), "an effect in two sets is one object again");
        all.OfType<VenomStatusEffect>().Single().DurationTicks.Should().Be(5);
        all.OfType<GuardedStatusEffect>().Single().DurationTicks.Should().Be(-1);
    }

    [Test]
    public void ALoggedOutPlayersEffectsKeepTheirCreatureTheTimeAndTheirEffects()
    {
        var fields = FieldsOf(StatusEffectInternals.SessionType);
        var session = ReloadCarrySamples.Owner(StatusEffectInternals.SessionType, fields);
        var held = LoggedOutPlayers(42u, out var venom);
        SetField(session, "_loggedOutPlayerEffects", held);

        var text = Carrier().Write(fields, session);
        var copy = ReloadCarrySamples.Owner(StatusEffectInternals.SessionType, fields);
        SetField(copy, "_loggedOutPlayerEffects", Activator.CreateInstance(StatusEffectInternals.LoggedOutField.FieldType));
        Carrier().Read(fields, copy, text);

        var carried = (System.Collections.IDictionary)FieldOf(StatusEffectInternals.SessionType, "_loggedOutPlayerEffects").GetValue(copy)!;
        carried.Keys.Cast<string>().Should().Equal("player-1");
        var entry = carried["player-1"]!;
        entry.GetType().GetProperty("Creature")!.GetValue(entry).Should().Be(42u);
        entry.GetType().GetProperty("LoggedOutAt")!.GetValue(entry).Should().Be(ReloadCarrySamples.SampleTime);
        ((CreatureStatusEffect)entry.GetType().GetProperty("Effects")!.GetValue(entry)!).GetAllEffects().Should().ContainSingle()
            .Which.Id.Should().Be(venom.Id);
    }

    [Test]
    public void AReloadFromTheParentBuildReadsTheTrackersAndTheLoggedOutEffectsFromTheOneTextTheStatusEffectServiceWrote()
    {
        // The parent build carried both tables in StatusEffectService, one text under its name. The tracker part (same name) reads
        // its table from it and skips the logged-out one; the session part finds nothing under its own name and reads the
        // logged-out table from the same text, taken once.
        const string former = ReloadStateCarrier.KeyPrefix + "SWLOR.Game.Server.Domain.Character.StatusEffectService";
        var coreFields = FieldsOf(StatusEffectInternals.CoreType);
        var core = ReloadCarrySamples.Owner(StatusEffectInternals.CoreType, coreFields);
        var tracked = new CreatureStatusEffect();
        tracked.Add(new VenomStatusEffect());
        SetField(core, "_creatureEffects", new Dictionary<uint, CreatureStatusEffect> { [7u] = tracked });
        var sessionFields = FieldsOf(StatusEffectInternals.SessionType);
        var session = ReloadCarrySamples.Owner(StatusEffectInternals.SessionType, sessionFields);
        SetField(session, "_loggedOutPlayerEffects", LoggedOutPlayers(42u, out _));
        var parentText = Newtonsoft.Json.Linq.JObject.Parse(Carrier().Write(coreFields, core));
        parentText.Merge(Newtonsoft.Json.Linq.JObject.Parse(Carrier().Write(sessionFields, session)));
        var store = new Dictionary<string, string> { [former] = parentText.ToString(Formatting.None) };

        var freshCore = ReloadCarrySamples.Owner(StatusEffectInternals.CoreType, coreFields);
        SetField(freshCore, "_creatureEffects", new Dictionary<uint, CreatureStatusEffect>());
        var freshSession = ReloadCarrySamples.Owner(StatusEffectInternals.SessionType, sessionFields);
        SetField(freshSession, "_loggedOutPlayerEffects", Activator.CreateInstance(StatusEffectInternals.LoggedOutField.FieldType));
        var taken = new List<string>();
        using var engine = NoPlayersOnline();

        Carrier().Invoking(carrier => carrier.AdoptAll(
            type => type == StatusEffectInternals.CoreType ? freshCore : type == StatusEffectInternals.SessionType ? freshSession : null,
            key =>
            {
                taken.Add(key);
                return store.TryGetValue(key, out var text) ? text : null;
            })).Should().NotThrow();

        ((Dictionary<uint, CreatureStatusEffect>)FieldOf(StatusEffectInternals.CoreType, "_creatureEffects").GetValue(freshCore)!)[7u]
            .GetAllEffects().Should().ContainSingle().Which.Should().BeOfType<VenomStatusEffect>();
        var carried = (System.Collections.IDictionary)FieldOf(StatusEffectInternals.SessionType, "_loggedOutPlayerEffects").GetValue(freshSession)!;
        carried.Keys.Cast<string>().Should().Equal("player-1");
        taken.Count(key => key == former).Should().Be(1, "each key is taken once");
    }

    [Test]
    public void TheTrackersAreAdoptedBeforeTheSessionSettlesThemAndNoClassSitsBetweenThem()
    {
        // ReloadStateCarrier.AdoptAll walks the carried classes in the ordinal order of their full names and calls
        // OnCarriedStateAdopted right after it reads a class's fields. The session part's adoption settles the creatures the
        // tracker part holds (and notifies the stat listener), so the tracker part's name has to sort first. Before P5-5e both
        // tables were one class's, so nothing else was adopted between them; nothing is now.
        var names = ReloadStateCarrier.CarriedClasses(Game).Select(entry => entry.Type.FullName).ToList();
        var tracker = names.IndexOf(typeof(StatusEffectService).FullName);
        var session = names.IndexOf(typeof(StatusEffectSessionService).FullName);
        tracker.Should().BeGreaterThanOrEqualTo(0);
        session.Should().Be(tracker + 1, "the session part follows the tracker part directly in the adoption order");
        string.CompareOrdinal(names[tracker], names[session]).Should().BeNegative();
        typeof(StatusEffectService).GetInterfaces().Should().NotContain(typeof(IReloadStateAdopter), "the session part settles the carried effects");
        typeof(StatusEffectSessionService).GetInterfaces().Should().Contain(typeof(IReloadStateAdopter));

        // And the adoption itself: the session part's adoption sees the trackers the tracker part just took from the same carry.
        var trackerFields = FieldsOf(typeof(StatusEffectService));
        var changed = new List<uint>();
        var written = NewTrackers(new Dictionary<uint, CreatureStatusEffect> { [42u] = new CreatureStatusEffect() }, changed);
        var trackers = NewTrackers(new Dictionary<uint, CreatureStatusEffect>(), changed);
        var sessionService = new StatusEffectSessionService(trackers, null!, null!, null!, null!);
        var store = new Dictionary<string, string>
        {
            [ReloadStateCarrier.KeyPrefix + typeof(StatusEffectService).FullName] = Carrier().Write(trackerFields, written),
            [ReloadStateCarrier.KeyPrefix + typeof(StatusEffectSessionService).FullName] = "{\"_loggedOutPlayerEffects\":{}}",
        };
        using var engine = NoPlayersOnline();

        Carrier().Invoking(carrier => carrier.AdoptAll(
            type => type == typeof(StatusEffectService) ? trackers : type == typeof(StatusEffectSessionService) ? sessionService : null,
            key => store.TryGetValue(key, out var text) ? text : null)).Should().NotThrow();

        changed.Should().Equal(new[] { 42u }, "the session part's adoption recomputes the movement rate of the creature the tracker part carried");
    }

    private static StatusEffectService NewTrackers(Dictionary<uint, CreatureStatusEffect> table, List<uint> changed)
    {
        var trackers = (StatusEffectService)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(StatusEffectService));
        FieldOf(typeof(StatusEffectService), "_creatureEffects").SetValue(trackers, table);
        FieldOf(typeof(StatusEffectService), "_statEffectListeners").SetValue(trackers, new List<IStatEffectListener> { new MovementProbe(changed) });
        return trackers;
    }

    private static object LoggedOutPlayers(uint creature, out VenomStatusEffect venom)
    {
        var effects = new CreatureStatusEffect();
        venom = new VenomStatusEffect();
        venom.RestartDuration(5);
        effects.Add(venom);
        var table = (System.Collections.IDictionary)Activator.CreateInstance(StatusEffectInternals.LoggedOutField.FieldType)!;
        var entryType = StatusEffectInternals.LoggedOutField.FieldType.GenericTypeArguments[1];
        table["player-1"] = Activator.CreateInstance(entryType, creature, effects, ReloadCarrySamples.SampleTime);
        return table;
    }

    private sealed class MovementProbe : IStatEffectListener
    {
        private readonly List<uint> _changed;

        public MovementProbe(List<uint> changed) => _changed = changed;

        public void OnStatusEffectsRestored(uint player)
        {
        }

        public void OnStatAdjustingEffectsChanged(uint creature) => _changed.Add(creature);
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
        object Resolve(Type type) => type == typeof(PartyRosterService) ? null : ReloadCarrySamples.Owner(type, FieldsOf(type));

        Carrier().Invoking(carrier => carrier.CarryAll(Resolve, (key, text) => store[key] = text))
            .Should().Throw<InvalidOperationException>().WithMessage("*PartyRosterService*");
        store.Should().ContainKey(ReloadStateCarrier.KeyPrefix + typeof(MusicService).FullName);
        store.Should().NotContainKey(ReloadStateCarrier.KeyPrefix + typeof(PartyRosterService).FullName);
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
    public void AReloadFromTheParentBuildReadsThePartiesAndAurasFromTheirFormerOwners()
    {
        // The parent build carried the party caches in PartyService and the auras in AbilityService, the same fields by name and type.
        const string formerParty = ReloadStateCarrier.KeyPrefix + "SWLOR.Game.Server.Domain.Social.PartyService";
        const string formerAbility = ReloadStateCarrier.KeyPrefix + "SWLOR.Game.Server.Domain.Abilities.AbilityService";
        var party = Guid.NewGuid();
        var roster = new PartyRosterService();
        roster.Parties[party] = new List<uint> { 11u, 12u };
        roster.CreatureToParty[11u] = party;
        roster.CreatureToParty[12u] = party;
        roster.PartyLeaders[party] = 11u;
        var auraFields = FieldsOf(typeof(AbilityAuraService));
        var auras = ReloadCarrySamples.Owner(typeof(AbilityAuraService), auraFields);
        SetField(auras, "_playerAuras", ReloadCarrySamples.Create(FieldOf(typeof(AbilityAuraService), "_playerAuras").FieldType));
        var hasher = new ReloadDigestHasher(Game);
        var aurasBefore = hasher.Hash(FieldOf(typeof(AbilityAuraService), "_playerAuras").GetValue(auras));
        var store = new Dictionary<string, string>
        {
            [formerParty] = Carrier().Write(FieldsOf(typeof(PartyRosterService)), roster),
            [formerAbility] = Carrier().Write(auraFields, auras),
        };

        var freshRoster = new PartyRosterService();
        var freshAuras = ReloadCarrySamples.Owner(typeof(AbilityAuraService), auraFields);
        SetField(freshAuras, "_playerAuras", ReloadCarrySamples.EmptyCollection(FieldOf(typeof(AbilityAuraService), "_playerAuras").FieldType));
        var taken = new List<string>();
        Carrier().Invoking(carrier => carrier.AdoptAll(
            type => type == typeof(PartyRosterService) ? freshRoster : type == typeof(AbilityAuraService) ? freshAuras : null,
            key =>
            {
                taken.Add(key);
                return store.Remove(key, out var text) ? text : null;
            })).Should().NotThrow("every other class finds nothing and starts empty, as from any build that did not carry it");

        freshRoster.Parties.Should().ContainKey(party).WhoseValue.Should().Equal(11u, 12u);
        freshRoster.CreatureToParty.Should().Equal(new Dictionary<uint, Guid> { [11u] = party, [12u] = party });
        freshRoster.PartyLeaders.Should().Equal(new Dictionary<Guid, uint> { [party] = 11u });
        hasher.Hash(FieldOf(typeof(AbilityAuraService), "_playerAuras").GetValue(freshAuras)).Should().Be(aurasBefore, "the auras are carried as they were");
        taken.Should().OnlyHaveUniqueItems().And.Contain(new[] { formerParty, formerAbility });
        store.Should().BeEmpty("both former texts were read");
    }

    [Test]
    public void AFormerOwnersTextFeedsEveryClassItsFieldsMovedToAndIsTakenOnce()
    {
        // The earlier build carried RetiredWholeService (since split into the left and right parts, and _oldRight renamed _right),
        // and CarriedFromKeeper with a _moved field the right part holds now. Take removes a key, as ReloadCarry.Take does.
        const string retired = ReloadStateCarrier.KeyPrefix + "SWLOR.Game.Server.Tests.Core.CarriedFrom.RetiredWholeService";
        var keeperKey = ReloadStateCarrier.KeyPrefix + typeof(CarriedFromKeeper).FullName;
        var store = new Dictionary<string, string>
        {
            [retired] = "{\"_left\":{\"1\":10},\"_oldRight\":{\"2\":20},\"_dropped\":{\"3\":30}}",
            [keeperKey] = "{\"_kept\":{\"4\":40},\"_moved\":{\"5\":50}}",
        };
        var keeper = new CarriedFromKeeper();
        var left = new CarriedFromLeftPart();
        var right = new CarriedFromRightPart();
        var owners = new Dictionary<Type, object> { [typeof(CarriedFromKeeper)] = keeper, [typeof(CarriedFromLeftPart)] = left, [typeof(CarriedFromRightPart)] = right };
        var taken = new List<string>();

        new ReloadStateCarrier(typeof(CarriedFromKeeper).Assembly).Invoking(carrier => carrier.AdoptAll(
            type => owners.TryGetValue(type, out var owner) ? owner : null,
            key =>
            {
                taken.Add(key);
                return store.Remove(key, out var text) ? text : null;
            })).Should().NotThrow();

        keeper.Kept.Should().Equal(new Dictionary<uint, int> { [4u] = 40 }, "the keeper reads its own text and skips the field that moved");
        left.Left.Should().Equal(new Dictionary<uint, int> { [1u] = 10 });
        right.Right.Should().Equal(new Dictionary<uint, int> { [2u] = 20 }, "a renamed field is read under its former name");
        right.Moved.Should().Equal(new Dictionary<uint, int> { [5u] = 50 }, "the keeper's own text also feeds the field that moved from it");
        right.Adoptions.Should().Be(1, "the adopter hook runs once, after both former texts are read");
        taken.Should().OnlyHaveUniqueItems().And.Contain(new[] { retired, keeperKey });
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
    public void ALocationIsCarriedAsItsAreaPositionAndFacingAndComesBackAsANewOne()
    {
        // A pending ship launch keeps where the pilot stood (P4-15 closure audit); the old build's location handle does not survive.
        ReloadStateCarrier.CarriedClasses(Game).Should().Contain(entry =>
            entry.Type.Name == "SpaceService" && entry.Fields.Any(field => field.Name == "_pendingShipLaunches"));
        var engine = new SWLOR.NWN.API.Testing.FakeNWScript();
        engine.SetReturn("GetAreaFromLocation", 4001u);
        engine.SetReturn("GetPositionFromLocation", new System.Numerics.Vector3(1.5f, 2.5f, 3.5f));
        engine.SetReturn("GetFacingFromLocation", 90f);
        using var scope = SWLOR.NWN.API.NWScript.NWScriptRuntime.Override(engine);
        var converter = new ReloadCarryLocationConverter();

        var text = JsonConvert.SerializeObject(SWLOR.NWN.API.Testing.EngineStructures.Create<SWLOR.NWN.API.Engine.Location>(0x55), converter);
        JsonConvert.DeserializeObject<SWLOR.NWN.API.Engine.Location>(text, converter);

        var made = engine.CallsTo("Location").Should().ContainSingle().Subject;
        made.Arguments[0].Should().Be(4001u);
        made.Arguments[1].Should().Be(new System.Numerics.Vector3(1.5f, 2.5f, 3.5f));
        made.Arguments[2].Should().Be(90f);
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

    /// <summary>Sets a field's value; a static readonly dictionary or list is refilled instead, as the carrier does.</summary>
    private static void SetSample(FieldInfo field, object owner, object value)
    {
        if (field.IsStatic && field.IsInitOnly && field.GetValue(null) is System.Collections.IDictionary current)
        {
            current.Clear();
            if (value is System.Collections.IDictionary entries)
            {
                foreach (System.Collections.DictionaryEntry entry in entries)
                    current[entry.Key] = entry.Value;
            }

            return;
        }

        if (field.IsStatic && field.IsInitOnly && field.GetValue(null) is System.Collections.IList list)
        {
            list.Clear();
            if (value is System.Collections.IEnumerable items)
            {
                foreach (var item in items)
                    list.Add(item);
            }

            return;
        }

        field.SetValue(owner, value);
    }
}
