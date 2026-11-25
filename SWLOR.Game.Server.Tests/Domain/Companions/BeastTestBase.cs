using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// The world every beast test runs in: the recording engine and plugins, the in-memory database, the beast catalog of
/// <see cref="SampleBeastDefinition"/> and the seeded random rolls. The calls into services that are not converted yet
/// (perks, stats, the companion control, the windows) are answered by <see cref="TestBeastMastery"/>, which also builds the
/// real <see cref="BeastSpawnService"/> and <see cref="BeastIncubationService"/> its <c>Spawn</c> and <c>Incubation</c> return.
/// </summary>
public abstract class BeastTestBase
{
    protected const uint ObjectInvalid = 0x7F000000;
    protected const uint Player = 101;
    protected const uint BeastObject = 102;
    protected const uint Skin = 103;
    protected const uint Claw = 104;
    protected const uint Npc = 105;
    protected const uint Area = 106;
    protected const uint ItemObject = 107;
    protected const uint Module = 108;
    protected const uint Incubator = 109;
    protected const uint Killer = 110;
    protected const uint Damager = 111;
    protected const uint FirstCreatedObject = 5000;
    protected const string PlayerUuid = "player-uuid-1";
    protected const string BeastId = "beast-1";

    protected FakeNWScript _engine = null!;
    protected FakeEventsPlugin _events = null!;
    protected FakeCreaturePlugin _creature = null!;
    protected FakeItemPlugin _itemPlugin = null!;
    protected LocalVariables _locals = null!;
    protected QueryableDBService _db = null!;
    protected RecordingFieldNotes _fieldNotes = null!;
    protected PropertyIdStub _propertyIds = null!;
    protected TestBeastMastery _service = null!;
    protected readonly List<IDisposable> _scopes = new();
    protected readonly HashSet<uint> _valid = new();
    protected readonly Dictionary<nint, (ItemPropertyType Type, int SubType, int Cost)> _properties = new();
    protected nint _nextHandle;
    protected uint _nextObject;
    protected FacadeContainer _container = null!;

    // A beast is given its AI profile through the AI facade, which resolves from a container.
    [OneTimeSetUp]
    public void BuildTheContainer()
    {
        _container = new FacadeContainer();
    }

    [OneTimeTearDown]
    public void DisposeTheContainer()
    {
        _container.Dispose();
    }

    [SetUp]
    public void SetUp()
    {
        _valid.Clear();
        _valid.UnionWith(new[] { Player, BeastObject, Skin, Claw, Npc, Area, ItemObject, Module, Incubator, Killer, Damager });
        _properties.Clear();
        _nextHandle = 1000;
        _nextObject = FirstCreatedObject;

        _engine = new FakeNWScript();
        _events = new FakeEventsPlugin();
        _creature = new FakeCreaturePlugin();
        _itemPlugin = new FakeItemPlugin();
        _locals = new LocalVariables(_engine);
        _db = new QueryableDBService();
        _fieldNotes = new RecordingFieldNotes();
        var property = DispatchProxy.Create<IPropertyService, PropertyIdStub>();
        _propertyIds = (PropertyIdStub)(object)property;
        _service = new TestBeastMastery(_db, _fieldNotes, property) { Definitions = new[] { typeof(SampleBeastDefinition) } };

        _engine.SetReturnHandler("GetIsObjectValid", arguments => _valid.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == Player);
        _engine.SetReturnHandler("GetIsDM", _ => _isDM);
        _engine.SetReturnHandler("GetIsDMPossessed", _ => _isPossessed);
        _engine.SetReturnHandler("GetObjectUUID", arguments => (uint)arguments[0]! == Player ? PlayerUuid : string.Empty);
        _engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! == BeastObject ? Player : ObjectInvalid);
        _engine.SetReturnHandler("GetAssociate", arguments =>
            (AssociateType)arguments[0]! == AssociateType.Henchman && (uint)arguments[1]! == Player && _henchman != 0
                ? _henchman
                : ObjectInvalid);
        _engine.SetReturnHandler("GetItemInSlot", arguments =>
        {
            var slot = (InventorySlot)arguments[0]!;
            return slot switch
            {
                InventorySlot.CreatureArmor => Skin,
                InventorySlot.CreatureLeft => Claw,
                _ => ObjectInvalid
            };
        });
        _engine.SetReturnHandler("ItemPropertyCustom", arguments =>
        {
            var handle = _nextHandle++;
            _properties[handle] = ((ItemPropertyType)arguments[0]!, (int)arguments[1]!, (int)arguments[2]!);
            return EngineStructures.Create<ItemProperty>(handle);
        });
        _engine.SetReturnHandler("CreateObject", _ => NewObject());
        _engine.SetReturnHandler("CreateItemOnObject", _ => NewObject());
        _engine.SetReturnHandler("StringToObject", arguments => (string)arguments[0]! == "npc-hex" ? Npc : ObjectInvalid);
        _engine.SetReturnHandler("GetArea", _ => Area);
        _engine.SetReturnHandler("GetModule", _ => Module);
        _engine.SetReturnHandler("GetLocation", _ => EngineStructures.Create<Location>());
        _engine.SetReturnHandler("Get2DARowCount", _ => 3);
        _engine.SetReturnHandler("Get2DAString", arguments => (int)arguments[2]! switch
        {
            1 => "5",
            2 => "not a number",
            _ => "10"
        });
        _events.SetReturnHandler("GetEventData", arguments => (string)arguments[0]! == "NPC" ? "npc-hex" : "incubator1");
        _engine.SetReturn("OBJECT_SELF", Player);

        _henchman = 0;
        _isDM = false;
        _isPossessed = false;
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(EventsPluginRuntime.Override(_events));
        _scopes.Add(CreaturePluginRuntime.Override(_creature));
        _scopes.Add(ItemPluginRuntime.Override(_itemPlugin));
        _scopes.Add(_container.Install());

        _service.Beasts.CacheData();
        _engine.ClearCalls();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    protected uint _henchman;
    protected bool _isDM;
    protected bool _isPossessed;

    // ---- helpers

    protected uint NewObject()
    {
        var id = _nextObject++;
        _valid.Add(id);
        return id;
    }

    protected IReadOnlyList<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    protected List<(ItemPropertyType Type, int SubType, int Cost)> Added(uint item) =>
        _engine.CallsTo("AddItemProperty")
            .Where(call => (uint)call.Arguments[2]! == item)
            .Select(call => _properties[((ItemProperty)call.Arguments[1]!).Handle])
            .ToList();

    /// <summary>Makes <see cref="Beast"/> a spawned beast of a type that belongs to the player and stores its entity.</summary>
    protected Beast ActiveBeast(int level = 1, int xp = 0, BeastType type = BeastType.Aradile)
    {
        _service.Beasts.SetBeastId(BeastObject, BeastId);
        _service.Beasts.SetBeastType(BeastObject, type);
        _henchman = BeastObject;
        return StoredBeast(level, xp, type);
    }

    protected Beast StoredBeast(int level = 1, int xp = 0, BeastType type = BeastType.Aradile, string id = BeastId)
    {
        var beast = new Beast
        {
            Id = id,
            Name = "Rex",
            OwnerPlayerId = PlayerUuid,
            Type = type,
            Level = level,
            XP = xp
        };
        _db.With(beast);
        return beast;
    }

    protected Beast Saved() => _db.Store.Saved.OfType<Beast>().Last();

    /// <summary>The seed whose draws, replayed with <see cref="System.Random"/>, satisfy <paramref name="rolls"/>.</summary>
    protected static int SeedWhere(Func<System.Random, bool> rolls)
    {
        for (var seed = 1; seed < 50000; seed++)
        {
            if (rolls(new System.Random(seed)))
                return seed;
        }

        throw new InvalidOperationException("No seed gives the wanted rolls.");
    }

    protected static IncubationJob Job(int mutationChance = 0, BeastType type = BeastType.Aradile)
    {
        return new IncubationJob
        {
            Id = "job-1",
            ParentPropertyId = "incubator1",
            PlayerId = PlayerUuid,
            BeastDNAType = type,
            MutationChance = mutationChance,
            AttackPurity = 400,
            AccuracyPurity = 300,
            EvasionPurity = 200,
            LearningPurity = 100,
            XPPenalty = 100
        };
    }

    /// <summary>Runs the actions the service handed to <c>AssignCommand</c>, and the ones those hand to <c>DelayCommand</c>.</summary>
    protected void RunScheduledCommands()
    {
        foreach (var call in _engine.CallsTo("AssignCommand"))
            ((Action)call.Arguments[1]!).Invoke();
        foreach (var call in _engine.CallsTo("DelayCommand"))
            ((Action)call.Arguments[1]!).Invoke();
    }

    /// <summary>Records the field notes the service grants.</summary>
    protected sealed class RecordingFieldNotes : IIncubationFieldNoteService
    {
        public List<(uint Player, BeastType Beast)> Granted { get; } = new();

        public void EnsureRegistered()
        {
        }

        public void LoadDisplayText()
        {
        }

        public void GrantDiscoveredNote(uint player, BeastType producedBeast) => Granted.Add((player, producedBeast));

        public IReadOnlyCollection<FieldNoteDetail> GetAllNotes() => Array.Empty<FieldNoteDetail>();

        public bool TryGetNoteForTarget(BeastType target, out FieldNoteDetail detail)
        {
            detail = null!;
            return false;
        }

        public bool TryGetNoteForKeyItem(KeyItemType keyItem, out FieldNoteDetail detail)
        {
            detail = null!;
            return false;
        }
    }

    /// <summary>Answers <see cref="IPropertyService.GetPropertyId"/>, the only member the beast service calls.</summary>
    public class PropertyIdStub : DispatchProxy
    {
        public Dictionary<uint, string> Ids { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == nameof(IPropertyService.GetPropertyId))
                return Ids.GetValueOrDefault((uint)args![0]!, string.Empty);

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    /// <summary>
    /// Three beasts: Aradile (50 levels, two mutations: Bearbug needs nothing and weighs 10, Wraid needs a blue lyase
    /// and weighs 50), Bearbug and Wraid (one level each).
    /// </summary>
    public sealed class SampleBeastDefinition : IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build()
        {
            var builder = new BeastBuilder();
            builder.Create(BeastType.Aradile)
                .Name("Aradile")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .AppearanceScale(1.5f)
                .SoundSetId(442)
                .PortraitId(4279)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Bruiser)
                .CanMutateInto(BeastType.Bearbug)
                .MutationWeight(10)
                .CanMutateInto(BeastType.Wraid)
                .MutationWeight(50)
                .MutationRequiresLyaseColor(EnzymeColorType.Blue, 1);

            for (var level = 1; level <= 50; level++)
            {
                AddLevel(builder, level);
            }

            builder.Create(BeastType.Bearbug)
                .Name("Bearbug")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Force);
            AddLevel(builder, 1);

            builder.Create(BeastType.Wraid)
                .Name("Wraid")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Evasion);
            AddLevel(builder, 1);

            return builder.Build();
        }

        private static void AddLevel(BeastBuilder builder, int level)
        {
            builder.AddLevel()
                .HP(100 * level)
                .FP(2 * level)
                .STM(3 * level)
                .DMG(level)
                .AttackDelay(ItemPropertyAttackDelay.Delay230)
                .Stat(AbilityType.Might, 10 + level)
                .Stat(AbilityType.Perception, 20 + level)
                .Stat(AbilityType.Vitality, 30 + level)
                .Stat(AbilityType.Willpower, 40 + level)
                .Stat(AbilityType.Agility, 50 + level)
                .Stat(AbilityType.Social, 60 + level)
                .MaxAttackBonus(100)
                .MaxAccuracyBonus(200)
                .MaxEvasionBonus(300);
        }
    }
}
