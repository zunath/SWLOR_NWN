using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The worn-gear transcript of <see cref="EquipmentStats"/> (P5-16h, PR 0): a fixed gear set is worn, removed and swapped through
/// the real equip and unequip handlers, the three refresh handlers and <c>StatRecord.RefreshPlayerEquipmentStats</c>, on a fake
/// world, and the transcript holds, after each step, what each saved player record changed from a new one, every call the code
/// made that writes to the engine (skin properties, hit points, local variables, refresh events) and any exception. It is
/// captured on the code before the stat actions move into static family types and compared after it. A coverage run of this
/// class over <c>EquipmentStats</c> shows every line and both sides of every branch of every stat action; the cases below are
/// chosen for that. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> only for a deliberate change.
/// </summary>
[NonParallelizable]
public class EquipmentStatsWornGearTranscriptTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/ViewModelSplit/EquipmentStatsWornGear.txt";

    private const uint Player1 = 100;
    private const uint DmWearer = 103;
    private const uint PossessedWearer = 104;
    private const uint Npc = 110;
    private const uint NpcSkin = 111;
    private const uint BareNpc = 112;
    private const uint BareNpcSkin = 113;

    private const uint Chest = 201;
    private const uint Helmet = 202;
    private const uint Boots = 203;
    private const uint Rifle = 204;
    private const uint ReadinessItem = 205;
    private const uint GloveItem = 206;
    private const uint CompanionPistolItem = 207;
    private const uint CraftItem = 208;
    private const uint CreatureItemWithAccuracy = 209;
    private const uint BadSubtypeItem = 210;
    private const uint SecondChest = 211;
    private const uint Made = 299;

    private FacadeContainer _facades = null!;
    private IDisposable _installed = null!;
    private InMemoryDBService _db = null!;
    private FakeNWScript _engine = null!;
    private FakeObjectPlugin _object = null!;
    private FakeEventsPlugin _events = null!;
    private ItemWorld _items = null!;
    private List<IDisposable> _scopes = null!;
    private List<string> _refreshes = null!;
    private readonly Dictionary<uint, Player> _records = new();
    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _dms = new();
    private readonly HashSet<uint> _possessed = new();
    private uint _self;
    private uint _eventItem;
    private int _eventSlot;
    private int _made;

    [SetUp]
    public void SetUp()
    {
        _db = new InMemoryDBService();
        _refreshes = new List<string>();
        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy.On("PublishRefreshEvent", args =>
        {
            _refreshes.Add(args[1]!.GetType().Name + ":" + JsonConvert.SerializeObject(args[1]));
            return null;
        });
        _facades = new FacadeContainer(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IDBService>(_db));
            services.Replace(ServiceDescriptor.Singleton(gui));
        });
        _installed = _facades.Install();

        _engine = new FakeNWScript();
        _object = new FakeObjectPlugin();
        _events = new FakeEventsPlugin();
        _items = new ItemWorld(_engine);
        _ = new LocalVariables(_engine);
        _scopes = new List<IDisposable>
        {
            NWScriptRuntime.Override(_engine),
            ObjectPluginRuntime.Override(_object),
            EventsPluginRuntime.Override(_events),
            CreaturePluginRuntime.Override(new FakeCreaturePlugin())
        };
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetObjectUUID", args => "uuid-" + (uint)args[0]!);
        _engine.SetReturnHandler("StringToObject", args => uint.Parse((string)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDM", args => _dms.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDMPossessed", args => _possessed.Contains((uint)args[0]!));
        _events.SetReturnHandler("GetEventData", args => (string)args[0]! == "ITEM" ? _eventItem.ToString() : _eventSlot.ToString());
        _engine.SetReturnHandler("ItemPropertyCustom", args =>
        {
            _items.AddProperty(Made, (ItemPropertyType)args[0]!, (int)args[1]!, (int)args[2]!);
            return _items.Property(Made, _made++);
        });
        _engine.SetReturnHandler("GetMaxHitPoints", _ => (int)(_object.CallsTo("SetMaxHitPoints").LastOrDefault()?.Arguments[1] ?? 100));

        _items.Add(Made);
        foreach (var wearer in new[] { Player1, DmWearer, PossessedWearer, Npc, BareNpc })
            _items.Add(wearer);
        _items.Add(NpcSkin, baseItem: BaseItem.CreatureItem);
        _items.Add(BareNpcSkin, baseItem: BaseItem.CreatureItem);
        _items.Equip(Npc, InventorySlot.CreatureArmor, NpcSkin);
        _items.Equip(BareNpc, InventorySlot.CreatureArmor, BareNpcSkin);

        foreach (var player in new[] { Player1, DmWearer, PossessedWearer })
        {
            _players.Add(player);
            _records[player] = new Player("uuid-" + player);
            _db.With(_records[player]);
        }

        _dms.Add(DmWearer);
        _possessed.Add(PossessedWearer);
        EquipmentStats.RegisterStatActions();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _installed.Dispose();
        _facades.Dispose();
    }

    private void Add(uint item, BaseItem baseItem, params (ItemPropertyType Type, int SubType, int Amount)[] properties)
    {
        _items.Add(item, baseItem: baseItem);
        foreach (var (type, subType, amount) in properties)
            _items.AddProperty(item, type, subType, amount);
    }

    private void BuildGear()
    {
        Add(Chest, BaseItem.Armor,
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 5), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 3),
            (ItemPropertyType.Resistance, (int)ResistanceType.Fire, 4), (ItemPropertyType.Evasion, -1, 2),
            (ItemPropertyType.Control, 1, 2), (ItemPropertyType.Craftsmanship, 1, 3), (ItemPropertyType.CPBonus, 1, 4));
        Add(SecondChest, BaseItem.Armor,
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 7), (ItemPropertyType.Evasion, -1, 6), (ItemPropertyType.FP, -1, 9));
        Add(Helmet, BaseItem.Helmet,
            (ItemPropertyType.HPBonus, -1, 10), (ItemPropertyType.FP, -1, 20), (ItemPropertyType.FPRegen, -1, 1),
            (ItemPropertyType.Stamina, -1, 15), (ItemPropertyType.STMRegen, -1, 1), (ItemPropertyType.Stealth, -1, 3),
            (ItemPropertyType.Detection, -1, 4));
        Add(Boots, BaseItem.Boots,
            (ItemPropertyType.TrapBonus, -1, 2), (ItemPropertyType.Disarm, -1, 3), (ItemPropertyType.PoisonBonus, -1, 4),
            (ItemPropertyType.Lockpicking, -1, 5));
        Add(Rifle, BaseItem.Longsword,
            (ItemPropertyType.Accuracy, -1, 8), (ItemPropertyType.Attack, -1, 6), (ItemPropertyType.ForceAttack, -1, 7));
        Add(ReadinessItem, BaseItem.Belt, (ItemPropertyType.CombatReadiness, -1, 5), (ItemPropertyType.Accuracy, -1, 3),
            (ItemPropertyType.ForceAttack, -1, 2), (ItemPropertyType.Attack, -1, 4));
        Add(GloveItem, BaseItem.PowerGlove, (ItemPropertyType.Attack, -1, 5), (ItemPropertyType.HPBonus, -1, 4));
        Add(CompanionPistolItem, BaseItem.CompanionPistol, (ItemPropertyType.HPBonus, -1, 50), (ItemPropertyType.Attack, -1, 9));
        Add(CraftItem, BaseItem.Gloves,
            (ItemPropertyType.Control, 1, 1), (ItemPropertyType.Control, 2, 2), (ItemPropertyType.Control, 3, 3), (ItemPropertyType.Control, 4, 4),
            (ItemPropertyType.Craftsmanship, 1, 1), (ItemPropertyType.Craftsmanship, 2, 2), (ItemPropertyType.Craftsmanship, 3, 3),
            (ItemPropertyType.Craftsmanship, 4, 4), (ItemPropertyType.CPBonus, 1, 1), (ItemPropertyType.CPBonus, 2, 2),
            (ItemPropertyType.CPBonus, 3, 3), (ItemPropertyType.CPBonus, 4, 4));
        Add(CreatureItemWithAccuracy, BaseItem.CreatureItem, (ItemPropertyType.Accuracy, -1, 5));
        Add(BadSubtypeItem, BaseItem.Armor,
            (ItemPropertyType.Defense, (int)CombatDamageType.Fire, 9), (ItemPropertyType.Resistance, 9999, 5),
            (ItemPropertyType.Resistance, (int)ResistanceType.Ice, 6));
    }

    [Test]
    public void WornGearProducesTheCapturedTranscript()
    {
        BuildGear();
        foreach (var (type, subType, amount) in new[]
                 {
                     (ItemPropertyType.FP, -1, 2), (ItemPropertyType.FPRegen, -1, 2), (ItemPropertyType.Stamina, -1, 2),
                     (ItemPropertyType.STMRegen, -1, 2), (ItemPropertyType.CombatReadiness, -1, 2), (ItemPropertyType.Attack, -1, 2),
                     (ItemPropertyType.Accuracy, -1, 2), (ItemPropertyType.ForceAttack, -1, 2), (ItemPropertyType.Evasion, -1, 2),
                     (ItemPropertyType.Stealth, -1, 2), (ItemPropertyType.Detection, -1, 2), (ItemPropertyType.TrapBonus, -1, 2),
                     (ItemPropertyType.Disarm, -1, 2), (ItemPropertyType.PoisonBonus, -1, 2), (ItemPropertyType.Lockpicking, -1, 2),
                     (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 2), (ItemPropertyType.Defense, (int)CombatDamageType.Force, 90),
                     (ItemPropertyType.Resistance, (int)ResistanceType.Fire, 4), (ItemPropertyType.Resistance, (int)ResistanceType.Ice, 90),
                     (ItemPropertyType.NPCHP, -1, 100)
                 })
            _items.AddProperty(NpcSkin, type, subType, amount);

        var text = new StringBuilder();
        text.Append("registered ").Append(string.Join(",", EquipmentStats.HandledPropertyTypes.Select(type => type.ToString()))).Append('\n');
        // What each record held after the step before (at first, as it was made); a step lists only what it changed.
        var previous = _records.ToDictionary(pair => pair.Key, pair => Flatten(JObject.FromObject(pair.Value)));
        var step = 0;

        void Step(string name, Action action)
        {
            _engine.ClearCalls();
            _object.ClearCalls();
            _refreshes.Clear();
            text.Append("== step ").Append(++step).Append(' ').Append(name).Append('\n');
            try
            {
                action();
            }
            catch (Exception exception)
            {
                text.Append("exception ").Append(exception.GetType().Name).Append(' ').Append(FirstLine(exception.Message)).Append('\n');
            }

            foreach (var call in _engine.Calls.Concat(_object.Calls).Where(IsWrite))
                text.Append("call ").Append(call.Member).Append('(').Append(string.Join(",", call.Arguments.Select(Format))).Append(")\n");
            foreach (var refresh in _refreshes)
                text.Append("refresh ").Append(refresh).Append('\n');
            foreach (var (creature, record) in _records)
            {
                var current = Flatten(JObject.FromObject(record));
                foreach (var (path, value) in current.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                    if (!previous[creature].TryGetValue(path, out var original) || original != value)
                        text.Append("record ").Append(creature).Append(' ').Append(path).Append('=').Append(value).Append('\n');
                previous[creature] = current;
            }
        }

        void Refresh(uint creature, uint item, InventorySlot slot)
        {
            _self = creature;
            _eventItem = item;
            _eventSlot = (int)slot;
            EquipmentStats.RefreshCombatReadinessAfterEquipmentChange();
            EquipmentStats.RefreshMaxHPAfterEquipmentChange();
            EquipmentStats.RefreshEquipmentRecordsAfterEquipmentChange();
            _facades.Get<IStatRecordService>().RefreshPlayerEquipmentStats(creature);
        }

        void Equip(uint creature, uint item, InventorySlot slot)
        {
            _self = creature;
            _eventItem = item;
            _eventSlot = (int)slot;
            EquipmentStats.ApplyStats();
            _items.Equip(creature, slot, item);
            Refresh(creature, item, slot);
        }

        void Unequip(uint creature, uint item, InventorySlot slot)
        {
            _self = creature;
            _eventItem = item;
            _eventSlot = (int)slot;
            EquipmentStats.RemoveStats();
            _items.Equip(creature, slot, ItemWorld.ObjectInvalid);
            Refresh(creature, item, slot);
        }

        // A player wears each item, takes it off and swaps it.
        Step("player wears the chest", () => Equip(Player1, Chest, InventorySlot.Chest));
        Step("player wears the helmet", () => Equip(Player1, Helmet, InventorySlot.Head));
        Step("player wears the boots", () => Equip(Player1, Boots, InventorySlot.Boots));
        Step("player wields the rifle", () => Equip(Player1, Rifle, InventorySlot.RightHand));
        Step("player wears the readiness item", () => Equip(Player1, ReadinessItem, InventorySlot.Belt));
        Step("player wears the craft gloves", () => Equip(Player1, CraftItem, InventorySlot.Arms));
        Step("player wears the creature item with accuracy", () => Equip(Player1, CreatureItemWithAccuracy, InventorySlot.CreatureArmor));
        Step("player wears the bad subtype item", () => Equip(Player1, BadSubtypeItem, InventorySlot.Neck));
        Step("player swaps the chest", () => Equip(Player1, SecondChest, InventorySlot.Chest));
        Step("player removes the swapped chest", () => Unequip(Player1, SecondChest, InventorySlot.Chest));
        Step("player removes the helmet", () => Unequip(Player1, Helmet, InventorySlot.Head));
        Step("player removes the boots", () => Unequip(Player1, Boots, InventorySlot.Boots));
        Step("player removes the rifle", () => Unequip(Player1, Rifle, InventorySlot.RightHand));
        Step("player removes the readiness item", () => Unequip(Player1, ReadinessItem, InventorySlot.Belt));
        Step("player removes the craft gloves", () => Unequip(Player1, CraftItem, InventorySlot.Arms));
        Step("player removes the creature item", () => Unequip(Player1, CreatureItemWithAccuracy, InventorySlot.CreatureArmor));
        Step("player removes the bad subtype item", () => Unequip(Player1, BadSubtypeItem, InventorySlot.Neck));

        // A Power Glove counts once per hand; a companion pistol never counts.
        Step("player wears the power glove", () => Equip(Player1, GloveItem, InventorySlot.RightHand));
        Step("player removes the power glove", () => Unequip(Player1, GloveItem, InventorySlot.RightHand));
        Step("player wears the companion pistol", () => Equip(Player1, CompanionPistolItem, InventorySlot.LeftHand));
        Step("player removes the companion pistol", () => Unequip(Player1, CompanionPistolItem, InventorySlot.LeftHand));
        Step("an unworn item is unequipped", () => Unequip(Player1, Helmet, InventorySlot.Head));

        // Unknown craft subtypes.
        foreach (var type in new[] { ItemPropertyType.Control, ItemPropertyType.Craftsmanship, ItemPropertyType.CPBonus })
        {
            var item = 300u + (uint)type;
            Add(item, BaseItem.Gloves, (type, 99, 2));
            Step("player wears an unknown " + type + " subtype", () => Equip(Player1, item, InventorySlot.Arms));
            Step("player removes an unknown " + type + " subtype", () => Unequip(Player1, item, InventorySlot.Arms));
        }

        // Staff are skipped by every action.
        Step("a DM wears the gear", () =>
        {
            Equip(DmWearer, Chest, InventorySlot.Chest);
            Equip(DmWearer, Helmet, InventorySlot.Head);
            Equip(DmWearer, Rifle, InventorySlot.RightHand);
            Unequip(DmWearer, Chest, InventorySlot.Chest);
        });
        Step("a possessed creature wears the gear", () =>
        {
            Equip(PossessedWearer, Chest, InventorySlot.Chest);
            Equip(PossessedWearer, Boots, InventorySlot.Boots);
            Unequip(PossessedWearer, Chest, InventorySlot.Chest);
        });

        // The actions themselves skip staff when called directly, as the stat revision migration and the refreshes call them.
        foreach (var (name, wearer) in new[] { ("a DM", DmWearer), ("a possessed creature", PossessedWearer) })
        {
            Step(name + " has every item's stats applied directly", () =>
            {
                foreach (var item in new[] { Chest, Helmet, Boots, Rifle, ReadinessItem, CraftItem, GloveItem })
                {
                    EquipmentStats.ApplyItemStats(wearer, item, true);
                    EquipmentStats.ApplyItemStats(wearer, item, false);
                }
            });
        }

        // The refresh handlers publish the status refreshes when the record was wrong.
        Step("a stale fp and stamina record is corrected and refreshed", () =>
        {
            _records[Player1].MaxFP = 500;
            _records[Player1].MaxStamina = 500;
            Refresh(Player1, Helmet, InventorySlot.Head);
        });
        Step("a stale hit point record is corrected and refreshed", () =>
        {
            _records[Player1].MaxHP = 5;
            Refresh(Player1, Helmet, InventorySlot.Head);
        });

        // An NPC skin that already holds values takes the amounts instead of a record.
        foreach (var (name, item, slot) in new[]
                 {
                     ("chest", Chest, InventorySlot.Chest), ("helmet", Helmet, InventorySlot.Head), ("boots", Boots, InventorySlot.Boots),
                     ("rifle", Rifle, InventorySlot.RightHand), ("readiness item", ReadinessItem, InventorySlot.Belt),
                     ("craft gloves", CraftItem, InventorySlot.Arms), ("creature item", CreatureItemWithAccuracy, InventorySlot.CreatureArmor),
                     ("bad subtype item", BadSubtypeItem, InventorySlot.Neck), ("power glove", GloveItem, InventorySlot.RightHand),
                     ("companion pistol", CompanionPistolItem, InventorySlot.LeftHand)
                 })
        {
            Step("npc wears the " + name, () => Equip(Npc, item, slot));
            Step("npc removes the " + name, () => Unequip(Npc, item, slot));
        }

        // The removal rules: Defense at or below zero, Resistance at exactly zero, HP below zero, and the maximum.
        Add(400, BaseItem.Armor, (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 5), (ItemPropertyType.Resistance, (int)ResistanceType.Fire, 4),
            (ItemPropertyType.HPBonus, -1, 150), (ItemPropertyType.FP, -1, 5));
        _items.Equip(Npc, InventorySlot.Chest, 400);
        Step("npc removes more defense, hit points and fp than it holds and exactly its resistance", () => Unequip(Npc, 400, InventorySlot.Chest));
        _object.SetReturn("GetCurrentHitPoints", 500);
        Step("npc adds hit points above its maximum", () => Equip(Npc, Helmet, InventorySlot.Head));
        _object.SetReturn("GetCurrentHitPoints", 1);
        Step("npc adds hit points below its maximum", () => Equip(Npc, Helmet, InventorySlot.Head));

        // An NPC whose skin holds nothing.
        Step("bare npc wears the chest", () => Equip(BareNpc, Chest, InventorySlot.Chest));
        Step("bare npc wears the helmet", () => Equip(BareNpc, Helmet, InventorySlot.Head));
        Step("bare npc removes the helmet", () => Unequip(BareNpc, Helmet, InventorySlot.Head));
        Step("bare npc wears the rifle", () => Equip(BareNpc, Rifle, InventorySlot.RightHand));

        // ApplyStatAmount: zero, negative, a weapon, a companion pistol, an unregistered type, a Power Glove and both directions.
        Step("amount zero", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.Evasion, -1, 0, true));
        Step("amount negative", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.Evasion, -1, -3, true));
        Step("amount for an unregistered type", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.AbilityBonus, -1, 4, true));
        Step("amount added", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.Evasion, -1, 4, true));
        Step("amount removed", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.Evasion, -1, 4, false));
        Step("amount on a weapon skips accuracy", () => EquipmentStats.ApplyStatAmount(Player1, Rifle, ItemPropertyType.Accuracy, -1, 4, true));
        Step("amount on a companion pistol", () => EquipmentStats.ApplyStatAmount(Player1, CompanionPistolItem, ItemPropertyType.Attack, -1, 4, true));
        Step("amount on a power glove counts twice", () => EquipmentStats.ApplyStatAmount(Player1, GloveItem, ItemPropertyType.Attack, -1, 4, true));
        Step("amount for a defense type", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.Defense, (int)CombatDamageType.Physical, 4, true));
        Step("amount for a craft subtype", () => EquipmentStats.ApplyStatAmount(Player1, Chest, ItemPropertyType.Control, 2, 4, true));

        var actual = text.ToString().Replace("\r\n", "\n");
        var path = Path.Combine(FindRepositoryRoot(), FixturePath.Replace('/', Path.DirectorySeparatorChar));
        if (Environment.GetEnvironmentVariable("SWLOR_UPDATE_SNAPSHOTS") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, new UTF8Encoding(false).GetBytes(actual));
        }

        File.Exists(path).Should().BeTrue("the transcript is committed beside the tests");
        var expected = new UTF8Encoding(false).GetString(File.ReadAllBytes(path)).Replace("\r\n", "\n");
        if (expected != actual)
        {
            var expectedLines = expected.Split('\n');
            var actualLines = actual.Split('\n');
            var index = 0;
            while (index < expectedLines.Length && index < actualLines.Length && expectedLines[index] == actualLines[index])
                index++;
            Assert.Fail($"The gear transcript differs at line {index + 1}: expected '{(index < expectedLines.Length ? expectedLines[index] : "<end>")}' " +
                        $"but got '{(index < actualLines.Length ? actualLines[index] : "<end>")}'.");
        }
    }

    private static bool IsWrite(RecordedCall call) =>
        !call.Member.StartsWith("Get", StringComparison.Ordinal) && !call.Member.StartsWith("Is", StringComparison.Ordinal) &&
        call.Member is not ("OBJECT_SELF" or "StringToObject" or "StringToInt" or "ResManGetAliasFor");

    private static string Format(object? argument) => argument switch
    {
        null => "null",
        ItemProperty property => "ip#" + property.Handle,
        _ => Convert.ToString(argument, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
    };

    private static string FirstLine(string text) => text.Split('\n')[0].TrimEnd('\r');

    private static Dictionary<string, string> Flatten(JToken token)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var leaf in token.SelectTokens("$..*").OfType<JValue>())
            values[leaf.Path] = leaf.ToString(Formatting.None, Array.Empty<JsonConverter>());
        return values;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln.");
    }
}
