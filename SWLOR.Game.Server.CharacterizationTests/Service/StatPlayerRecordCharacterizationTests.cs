using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the stat bookkeeping that works on a <see cref="Player"/> record without the engine: the <c>Stat.Adjust*</c>
/// methods, the equipment totals and how they are written into the record, and stat adjustment sources.
/// </summary>
public class StatPlayerRecordCharacterizationTests
{
    private static readonly (string Name, Action<Player, int> Adjust, Func<Player, int> Read)[] RecordAdjustments =
    {
        (nameof(Stat.AdjustAccuracy), Stat.AdjustAccuracy, player => player.Accuracy),
        (nameof(Stat.AdjustAttack), Stat.AdjustAttack, player => player.Attack),
        (nameof(Stat.AdjustCombatReadiness), Stat.AdjustCombatReadiness, player => player.CombatReadiness),
        (nameof(Stat.AdjustDetection), Stat.AdjustDetection, player => player.Detection),
        (nameof(Stat.AdjustEvasion), Stat.AdjustEvasion, player => player.Evasion),
        (nameof(Stat.AdjustForceAttack), Stat.AdjustForceAttack, player => player.ForceAttack),
        (nameof(Stat.AdjustFPRegen), Stat.AdjustFPRegen, player => player.FPRegen),
        (nameof(Stat.AdjustHPRegen), Stat.AdjustHPRegen, player => player.HPRegen),
        (nameof(Stat.AdjustLockpicking), Stat.AdjustLockpicking, player => player.Lockpicking),
        (nameof(Stat.AdjustPoisonBonus), Stat.AdjustPoisonBonus, player => player.PoisonBonus),
        (nameof(Stat.AdjustSTMRegen), Stat.AdjustSTMRegen, player => player.STMRegen),
        (nameof(Stat.AdjustStealth), Stat.AdjustStealth, player => player.Stealth),
        (nameof(Stat.AdjustTrapBonus), Stat.AdjustTrapBonus, player => player.TrapBonus),
        (nameof(Stat.AdjustTrapDisarm), Stat.AdjustTrapDisarm, player => player.TrapDisarm),
    };

    private static readonly (string Name, Action<Player, SkillType, int> Adjust, Func<Player, Dictionary<SkillType, int>> Read)[] SkillAdjustments =
    {
        (nameof(Stat.AdjustControl), Stat.AdjustControl, player => player.Control),
        (nameof(Stat.AdjustCPBonus), Stat.AdjustCPBonus, player => player.CPBonus),
        (nameof(Stat.AdjustCraftsmanship), Stat.AdjustCraftsmanship, player => player.Craftsmanship),
    };

    [Test]
    public void AdjustPlayerRecordValues()
    {
        var table = new SnapshotTable(
            "Stat.Adjust* methods that add to one numeric field of a Player record",
            "Each method adds its amount to one field (negative results are allowed). The table applies two adjustments in a row to a new Player.",
            "14 methods x adjustment pairs {(5,-8),(0,3),(-4,-4)}.",
            new[] { "method", "firstAdjustment", "secondAdjustment" },
            new[] { "newPlayerValue", "afterFirst", "afterSecond" })
        {
            AllowConstantOutputs = true
        };

        foreach (var (name, adjust, read) in RecordAdjustments)
        {
            foreach (var (first, second) in new[] { (5, -8), (0, 3), (-4, -4) })
            {
                var player = new Player();
                var initial = read(player);
                adjust(player, first);
                var afterFirst = read(player);
                adjust(player, second);
                table.Add(name, first, second, initial, afterFirst, read(player));
            }
        }

        Snapshot.Verify("Stat", "AdjustPlayerRecordValues", table);
    }

    [Test]
    public void AdjustDefense()
    {
        var table = new SnapshotTable(
            "Stat.AdjustDefense(Player entity, CombatDamageType type, int adjustBy)",
            "Adds to the player's stored Defense for a damage type; only the damage types that use a Defense rating are recorded.",
            "every CombatDamageType member, adjusted by +7 and then by -2.",
            new[] { "damageType", "damageTypeName" },
            new[] { "defensesAfterFirst", "defensesAfterSecond" });

        foreach (var type in Enum.GetValues<CombatDamageType>().OrderBy(type => DefinitionCell.Id(type)))
        {
            var player = new Player();
            Stat.AdjustDefense(player, type, 7);
            var afterFirst = DefenseText(player);
            Stat.AdjustDefense(player, type, -2);
            table.Add(DefinitionCell.Id(type), type, afterFirst, DefenseText(player));
        }

        Snapshot.Verify("Stat", "AdjustDefense", table);
    }

    [Test]
    public void AdjustSkillRecords()
    {
        var table = new SnapshotTable(
            "Stat.AdjustControl / AdjustCPBonus / AdjustCraftsmanship (Player entity, SkillType, int)",
            "Adds to the per-skill value of a Player record, creating the entry at zero first.",
            "3 methods x skills {Invalid, Armor, Lightsaber}, adjusted by +6 and then by -10 (the second step shows the entry is kept and can go negative).",
            new[] { "method", "skill" },
            new[] { "afterFirst", "afterSecond" });

        foreach (var (name, adjust, read) in SkillAdjustments)
        {
            foreach (var skill in new[] { SkillType.Invalid, SkillType.Armor, SkillType.Lightsaber })
            {
                var player = new Player();
                adjust(player, skill, 6);
                var afterFirst = SkillValuesText(read(player));
                adjust(player, skill, -10);
                table.Add(name, skill, afterFirst, SkillValuesText(read(player)));
            }
        }

        Snapshot.Verify("Stat", "AdjustSkillRecords", table);
    }

    [Test]
    public void EquipmentTotalsAddSumsWornGear()
    {
        var table = new SnapshotTable(
            "EquipmentStatTotals.Add(ItemPropertyType, int subtype, int amount)",
            "Sums the Defense (per defense damage type), Evasion, FP and Stamina item properties of worn gear; every other property, and Defense of a damage type that has no Defense rating, is ignored.",
            "Item property lists applied in order to a new EquipmentStatTotals. Defense subtypes are CombatDamageType ids (1 Physical, 2 Force, 3 Fire, 99 undefined, which is refused with an exception).",
            new[] { "scenario" },
            new[] { "physicalDefense", "forceDefense", "fireDefense", "evasion", "fp", "stamina" });

        var scenarios = new (string Name, (ItemPropertyType Type, int Subtype, int Amount)[] Properties)[]
        {
            ("none", Array.Empty<(ItemPropertyType, int, int)>()),
            ("physical defense 5", new[] { (ItemPropertyType.Defense, 1, 5) }),
            ("physical 5 + physical 3 + force 2", new[] { (ItemPropertyType.Defense, 1, 5), (ItemPropertyType.Defense, 1, 3), (ItemPropertyType.Defense, 2, 2) }),
            ("fire defense 10 (no Defense rating)", new[] { (ItemPropertyType.Defense, 3, 10) }),
            ("undefined damage type 99 defense 4", new[] { (ItemPropertyType.Defense, 99, 4) }),
            ("negative physical defense -6", new[] { (ItemPropertyType.Defense, 1, -6) }),
            ("evasion 4 + evasion -1", new[] { (ItemPropertyType.Evasion, 0, 4), (ItemPropertyType.Evasion, 0, -1) }),
            ("fp 3 + stamina 6", new[] { (ItemPropertyType.FP, 0, 3), (ItemPropertyType.Stamina, 0, 6) }),
            ("HP bonus 10 (not an equipment record stat)", new[] { (ItemPropertyType.HPBonus, 0, 10) }),
            ("mixed", new[]
            {
                (ItemPropertyType.Defense, 1, 5), (ItemPropertyType.Defense, 2, 2), (ItemPropertyType.Evasion, 0, 4),
                (ItemPropertyType.FP, 0, 6), (ItemPropertyType.Stamina, 0, 9), (ItemPropertyType.HPBonus, 0, 20)
            })
        };

        foreach (var (name, properties) in scenarios)
        {
            EquipmentStatTotals totals;
            try
            {
                totals = BuildTotals(properties);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                // A Defense property whose subtype is not a known damage type is refused outright.
                table.Add(name, exception.GetType().Name, null, null, null, null, null);
                continue;
            }

            table.Add(
                name,
                totals.GetDefense(CombatDamageType.Physical),
                totals.GetDefense(CombatDamageType.Force),
                totals.GetDefense(CombatDamageType.Fire),
                totals.Evasion,
                totals.FP,
                totals.Stamina);
        }

        Snapshot.Verify("Stat", "EquipmentStatTotals", table);
    }

    [Test]
    public void EquipmentTotalsGetRecordStat()
    {
        var table = new SnapshotTable(
            "EquipmentStatTotals.GetRecordStat(ItemPropertyType type)",
            "The equipment record an item property type feeds.",
            "Defense, Evasion, FP, Stamina, HPBonus, ACBonus and Invalid.",
            new[] { "itemPropertyType" },
            new[] { "recordStat" });

        foreach (var type in new[]
                 {
                     ItemPropertyType.Defense, ItemPropertyType.Evasion, ItemPropertyType.FP, ItemPropertyType.Stamina,
                     ItemPropertyType.HPBonus, ItemPropertyType.ACBonus, ItemPropertyType.Invalid
                 })
        {
            table.Add(type, EquipmentStatTotals.GetRecordStat(type));
        }

        Snapshot.Verify("Stat", "EquipmentStatTotalsGetRecordStat", table);
    }

    [Test]
    public void ApplyEquipmentStatTotals()
    {
        var table = new SnapshotTable(
            "Stat.ApplyEquipmentStatTotals(Player entity, EquipmentStatTotals totals, EquipmentRecordStats stats)",
            "Writes worn-gear totals into the selected equipment records of a player (Defense per type, Evasion, max FP and max stamina above their bases) " +
            "and reports which records actually changed.",
            "gear {nothing worn, physical 5 force 2 evasion 4 FP 6 stamina 9} x record before {new player, already matching that gear, stale values, legacy record with no Defense map} " +
            "x records to rebuild {None, Defense, Evasion, FP, Stamina, All, Defense|FP}. Stat.BaseFP and Stat.BaseSTM are 10.",
            new[] { "gear", "recordBefore", "stats" },
            new[] { "corrected", "recordAfter" });

        var gear = new (string Name, (ItemPropertyType, int, int)[] Properties)[]
        {
            ("nothing worn", Array.Empty<(ItemPropertyType, int, int)>()),
            ("worn", new[]
            {
                (ItemPropertyType.Defense, 1, 5), (ItemPropertyType.Defense, 2, 2), (ItemPropertyType.Evasion, 0, 4),
                (ItemPropertyType.FP, 0, 6), (ItemPropertyType.Stamina, 0, 9)
            })
        };
        var starts = new (string Name, Func<Player> Create)[]
        {
            ("new player", () => NewPlayer(physical: 0, force: 0, evasion: 0, maxFP: 0, maxStamina: 0)),
            ("matching", () => NewPlayer(physical: 5, force: 2, evasion: 4, maxFP: 16, maxStamina: 19)),
            ("stale", () => NewPlayer(physical: 1, force: 9, evasion: 7, maxFP: 30, maxStamina: 30)),
            ("legacy", () =>
            {
                var player = NewPlayer(physical: 0, force: 0, evasion: 7, maxFP: 10, maxStamina: 10);
                player.Defenses = null!;
                return player;
            })
        };
        var selections = new[]
        {
            EquipmentRecordStats.None, EquipmentRecordStats.Defense, EquipmentRecordStats.Evasion, EquipmentRecordStats.FP,
            EquipmentRecordStats.Stamina, EquipmentRecordStats.All, EquipmentRecordStats.Defense | EquipmentRecordStats.FP
        };

        foreach (var (gearName, properties) in gear)
        {
            foreach (var (startName, create) in starts)
            {
                foreach (var selection in selections)
                {
                    var player = create();
                    var corrected = Stat.ApplyEquipmentStatTotals(player, BuildTotals(properties), selection);
                    table.Add(gearName, startName, selection, corrected, RecordText(player));
                }
            }
        }

        Snapshot.Verify("Stat", "ApplyEquipmentStatTotals", table);
    }

    [Test]
    public void StatAdjustmentSourceLookups()
    {
        var table = new SnapshotTable(
            "StatAdjustmentSource (indexer and GetModifierGroup)",
            "A stat payload keeps its own key: the indexer returns the payload's amount for a stat (zero when the payload lacks it) and the modifier group is key:statId.",
            "two sources (one with Attack 3 and DefensePercentAdjustment -2, one empty) x stats {Attack, DefensePercentAdjustment, Accuracy} x trigger {Attack, Invalid, 1167}.",
            new[] { "source", "stat", "trigger" },
            new[] { "amount", "modifierGroup" });

        var sources = new (string Label, StatAdjustmentSource Source)[]
        {
            ("with stats", new StatAdjustmentSource("perk-a",
                new Dictionary<StatType, int> { { StatType.Attack, 3 }, { StatType.DefensePercentAdjustment, -2 } })),
            ("empty", new StatAdjustmentSource("effect-b", new Dictionary<StatType, int>()))
        };

        foreach (var (label, source) in sources)
        {
            foreach (var stat in new[] { StatType.Attack, StatType.DefensePercentAdjustment, StatType.Accuracy })
            {
                foreach (var trigger in new[] { StatType.Attack, StatType.Invalid, (StatType)1167 })
                    table.Add(label, stat, trigger, source[stat], source.GetModifierGroup(trigger));
            }
        }

        Snapshot.Verify("Stat", "StatAdjustmentSource", table);
    }

    private static EquipmentStatTotals BuildTotals((ItemPropertyType Type, int Subtype, int Amount)[] properties)
    {
        var totals = new EquipmentStatTotals();
        foreach (var (type, subtype, amount) in properties)
            totals.Add(type, subtype, amount);
        return totals;
    }

    private static Player NewPlayer(int physical, int force, int evasion, int maxFP, int maxStamina)
    {
        return new Player
        {
            Defenses = new Dictionary<CombatDamageType, int>
            {
                { CombatDamageType.Physical, physical },
                { CombatDamageType.Force, force }
            },
            Evasion = evasion,
            MaxFP = maxFP,
            MaxStamina = maxStamina
        };
    }

    private static string RecordText(Player player)
    {
        return $"Defenses={DefenseText(player)} Evasion={player.Evasion} MaxFP={player.MaxFP} MaxStamina={player.MaxStamina}";
    }

    private static string DefenseText(Player player)
    {
        if (player.Defenses == null)
            return "null";

        return "{" + string.Join(",", player.Defenses
            .OrderBy(entry => DefinitionCell.Id(entry.Key))
            .Select(entry => $"{entry.Key}:{entry.Value}")) + "}";
    }

    private static string SkillValuesText(Dictionary<SkillType, int> values)
    {
        return "{" + string.Join(",", values
            .OrderBy(entry => DefinitionCell.Id(entry.Key))
            .Select(entry => $"{entry.Key}:{entry.Value}")) + "}";
    }
}
