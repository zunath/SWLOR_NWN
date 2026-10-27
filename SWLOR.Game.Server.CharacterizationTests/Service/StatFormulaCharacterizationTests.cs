using System.Reflection;
using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the number-in, number-out formulas of <see cref="Stat"/>: maximum FP and stamina, the Attack, Accuracy, Evasion,
/// Defense, Detection and Stealth ratings, healing amounts, effect scaling and the beast regeneration rules. None of them
/// touches the engine or random numbers.
/// </summary>
public class StatFormulaCharacterizationTests
{
    [Test]
    public void PublicConstants()
    {
        var table = new SnapshotTable(
            "Stat (public const fields)",
            "Every public constant of the Stat class: base pools, deflection and guard caps, the combat readiness and NPC detection caps, " +
            "the movement speed bounds and the beast regeneration tuning.",
            "All public const fields of Stat, sorted by name.",
            new[] { "constant" },
            new[] { "type", "value" });

        var constants = typeof(Stat)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal);
        foreach (var field in constants)
            table.Add(field.Name, field.FieldType.Name, field.GetRawConstantValue());

        Snapshot.Verify("Stat", "PublicConstants", table);
    }

    [Test]
    public void GetMaxFP()
    {
        var table = new SnapshotTable(
            "Stat.GetMaxFP(int baseFP, int willpower, int bonus)",
            "Maximum FP: the base pool plus 3 per point of Willpower plus the flat bonus.",
            "base FP {-5,0,10,25,70} x Willpower {-3,0,1,2,5,10,25,60} x bonus {-10,0,7,40}.",
            new[] { "baseFP", "willpower", "bonus" },
            new[] { "maxFP" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 10, 25, 70 },
                     new[] { -3, 0, 1, 2, 5, 10, 25, 60 },
                     new[] { -10, 0, 7, 40 }))
        {
            table.Add(row[0], row[1], row[2], Stat.GetMaxFP(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Stat", "GetMaxFP", table);
    }

    [Test]
    public void GetMaxStamina()
    {
        var table = new SnapshotTable(
            "Stat.GetMaxStamina(int baseStamina, int might, int bonus)",
            "Maximum stamina: the base pool plus 1.5 per point of Might (integer arithmetic: might * 3 / 2) plus the flat bonus.",
            "base stamina {-5,0,10,25,70} x Might {-3,-1,0,1,2,3,5,10,25,61} x bonus {-10,0,7}. Odd Might values show the integer rounding.",
            new[] { "baseStamina", "might", "bonus" },
            new[] { "maxStamina" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 10, 25, 70 },
                     new[] { -3, -1, 0, 1, 2, 3, 5, 10, 25, 61 },
                     new[] { -10, 0, 7 }))
        {
            table.Add(row[0], row[1], row[2], Stat.GetMaxStamina(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Stat", "GetMaxStamina", table);
    }

    [Test]
    public void IsStaminaRestorationBlocked()
    {
        var table = new SnapshotTable(
            "Stat.IsStaminaRestorationBlocked(bool isPlayer, bool inStealth)",
            "A stealthed player gains no stamina from any source; NPCs are never blocked.",
            "Both booleans, all four combinations.",
            new[] { "isPlayer", "inStealth" },
            new[] { "blocked" });

        foreach (var isPlayer in new[] { false, true })
        {
            foreach (var inStealth in new[] { false, true })
                table.Add(isPlayer, inStealth, Stat.IsStaminaRestorationBlocked(isPlayer, inStealth));
        }

        Snapshot.Verify("Stat", "IsStaminaRestorationBlocked", table);
    }

    [Test]
    public void GetAttack()
    {
        var table = RatingTable(
            "Stat.GetAttack(int level, int stat, int bonus)",
            "Raw Attack: 8 plus 2 per level plus the stat plus the bonus.");

        foreach (var row in RatingGrid())
            table.Add(row[0], row[1], row[2], Stat.GetAttack(row[0], row[1], row[2]));

        Snapshot.Verify("Stat", "GetAttack", table);
    }

    [Test]
    public void GetAccuracy()
    {
        var table = RatingTable(
            "Stat.GetAccuracy(int level, int stat, int bonus)",
            "Raw Accuracy: 8 plus 2 per level plus the stat plus the bonus.");

        foreach (var row in RatingGrid())
            table.Add(row[0], row[1], row[2], Stat.GetAccuracy(row[0], row[1], row[2]));

        Snapshot.Verify("Stat", "GetAccuracy", table);
    }

    [Test]
    public void GetEvasion()
    {
        var table = RatingTable(
            "Stat.GetEvasion(int level, int stat, int bonus)",
            "Raw Evasion: 8 plus 2 per level plus the stat plus the bonus.");

        foreach (var row in RatingGrid())
            table.Add(row[0], row[1], row[2], Stat.GetEvasion(row[0], row[1], row[2]));

        Snapshot.Verify("Stat", "GetEvasion", table);
    }

    [Test]
    public void GetEvasionWithNativeArmor()
    {
        var table = new SnapshotTable(
            "Stat.GetEvasion(int level, int stat, int bonus, int nativeArmorClass, bool hasNpcStatBudget)",
            "Raw Evasion with the native armor class folded in at 5 per point, unless the creature has an NPC stat skin that already carries the whole Evasion budget.",
            "level {0,10,50} x stat {0,20} x bonus {-10,0,25} x native armor class {-2,0,3,8} x has NPC stat budget {false,true}.",
            new[] { "level", "stat", "bonus", "nativeArmorClass", "hasNpcStatBudget" },
            new[] { "evasion" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 10, 50 },
                     new[] { 0, 20 },
                     new[] { -10, 0, 25 },
                     new[] { -2, 0, 3, 8 },
                     new[] { 0, 1 }))
        {
            table.Add(row[0], row[1], row[2], row[3], row[4] == 1,
                Stat.GetEvasion(row[0], row[1], row[2], row[3], row[4] == 1));
        }

        Snapshot.Verify("Stat", "GetEvasionWithNativeArmor", table);
    }

    [Test]
    public void CalculateDefense()
    {
        var table = new SnapshotTable(
            "Stat.CalculateDefense(int defenderStat, int skillLevel, int defenseBonus)",
            "Defense: 8 plus 1.2 per skill level (float arithmetic, truncated toward zero) plus the stat plus the bonus.",
            "stat {-5,0,10,50,150} x skill level {-1,0,1,5,10,33,60,100} x bonus {-20,0,15,100}.",
            new[] { "defenderStat", "skillLevel", "defenseBonus" },
            new[] { "defense" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 10, 50, 150 },
                     new[] { -1, 0, 1, 5, 10, 33, 60, 100 },
                     new[] { -20, 0, 15, 100 }))
        {
            table.Add(row[0], row[1], row[2], Stat.CalculateDefense(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Stat", "CalculateDefense", table);
    }

    [Test]
    public void CalculateDetectionRating()
    {
        var table = new SnapshotTable(
            "Stat.CalculateDetectionRating(int perception, int willpower, int equipmentBonus, int adjustment, bool detectMode)",
            "Detection: Perception plus Willpower plus equipment plus adjustments, plus 5 in detect mode, never below zero.",
            "Perception {0,5,20} x Willpower {0,8,30} x equipment {-10,0,12} x adjustment {-50,0,9} x detect mode {false,true}.",
            new[] { "perception", "willpower", "equipmentBonus", "adjustment", "detectMode" },
            new[] { "detection" });

        foreach (var row in Grid.Cross(
                     new[] { 0, 5, 20 },
                     new[] { 0, 8, 30 },
                     new[] { -10, 0, 12 },
                     new[] { -50, 0, 9 },
                     new[] { 0, 1 }))
        {
            table.Add(row[0], row[1], row[2], row[3], row[4] == 1,
                Stat.CalculateDetectionRating(row[0], row[1], row[2], row[3], row[4] == 1));
        }

        Snapshot.Verify("Stat", "CalculateDetectionRating", table);
    }

    [Test]
    public void ApplyNPCDetectionCap()
    {
        var table = new SnapshotTable(
            "Stat.ApplyNPCDetectionCap(int detection, bool isNPC)",
            "Detection is never negative, and an NPC's is capped at Stat.MaximumNPCDetection.",
            "detection {-5,0,1,49,50,51,200} x isNPC {false,true}.",
            new[] { "detection", "isNPC" },
            new[] { "detection" });

        foreach (var detection in new[] { -5, 0, 1, 49, 50, 51, 200 })
        {
            foreach (var isNPC in new[] { false, true })
                table.Add(detection, isNPC, Stat.ApplyNPCDetectionCap(detection, isNPC));
        }

        Snapshot.Verify("Stat", "ApplyNPCDetectionCap", table);
    }

    [Test]
    public void CalculateStealthRating()
    {
        var table = new SnapshotTable(
            "Stat.CalculateStealthRating(int agility, int equipmentBonus, int adjustment)",
            "Stealth: twice Agility plus equipment plus adjustments, never below zero.",
            "Agility {-2,0,3,10,40} x equipment {-20,0,15} x adjustment {-30,0,8}.",
            new[] { "agility", "equipmentBonus", "adjustment" },
            new[] { "stealth" });

        foreach (var row in Grid.Cross(
                     new[] { -2, 0, 3, 10, 40 },
                     new[] { -20, 0, 15 },
                     new[] { -30, 0, 8 }))
        {
            table.Add(row[0], row[1], row[2], Stat.CalculateStealthRating(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Stat", "CalculateStealthRating", table);
    }

    [Test]
    public void CalculateOutgoingAbilityHealingAmount()
    {
        var table = new SnapshotTable(
            "Stat.CalculateOutgoingAbilityHealingAmount(int amount, int adjustment)",
            "A healing ability's amount raised by the percent adjustment (rounded up); a non-positive amount or adjustment changes nothing.",
            "amount {-5,0,1,7,33,100,1000} x percent adjustment {-20,0,1,10,33,50,100,250}.",
            new[] { "amount", "adjustment" },
            new[] { "healing" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 7, 33, 100, 1000 },
                     new[] { -20, 0, 1, 10, 33, 50, 100, 250 }))
        {
            table.Add(row[0], row[1], Stat.CalculateOutgoingAbilityHealingAmount(row[0], row[1]));
        }

        Snapshot.Verify("Stat", "CalculateOutgoingAbilityHealingAmount", table);
    }

    [Test]
    public void CalculateEffectiveHealingAmount()
    {
        var table = new SnapshotTable(
            "Stat.CalculateEffectiveHealingAmount(int amount, int currentHP, int maxHP)",
            "The healing that actually lands: nothing on a dead target, otherwise the amount limited to the missing HP.",
            "amount {-5,0,10,50,500} x current HP {-5,0,1,50,99,100,150} x max HP {-5,0,100,200}.",
            new[] { "amount", "currentHP", "maxHP" },
            new[] { "effectiveHealing" });

        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 10, 50, 500 },
                     new[] { -5, 0, 1, 50, 99, 100, 150 },
                     new[] { -5, 0, 100, 200 }))
        {
            table.Add(row[0], row[1], row[2], Stat.CalculateEffectiveHealingAmount(row[0], row[1], row[2]));
        }

        Snapshot.Verify("Stat", "CalculateEffectiveHealingAmount", table);
    }

    [Test]
    public void ScaleEffect()
    {
        var table = new SnapshotTable(
            "Stat.ScaleEffect(int baseAmount, int primaryStat, float primaryRate, int secondaryStat, float secondaryRate)",
            "Raises a positive effect amount by its primary and secondary stat (each scaled by its rate, never negative), rounded up; a non-positive amount is returned unchanged.",
            "base amount {-5,0,1,7,10,37,100} x primary stat {-10,0,15,50,120} x secondary stat {-10,0,20,100} x rate pair {(0.01,0.005) the defaults, (0.02,0), (0,0.01)}.",
            new[] { "baseAmount", "primaryStat", "primaryRate", "secondaryStat", "secondaryRate" },
            new[] { "scaledAmount" });

        var ratePairs = new[] { (0.01f, 0.005f), (0.02f, 0f), (0f, 0.01f) };
        foreach (var row in Grid.Cross(
                     new[] { -5, 0, 1, 7, 10, 37, 100 },
                     new[] { -10, 0, 15, 50, 120 },
                     new[] { -10, 0, 20, 100 },
                     new[] { 0, 1, 2 }))
        {
            var (primaryRate, secondaryRate) = ratePairs[row[3]];
            table.Add(row[0], row[1], primaryRate, row[2], secondaryRate,
                Stat.ScaleEffect(row[0], row[1], primaryRate, row[2], secondaryRate));
        }

        Snapshot.Verify("Stat", "ScaleEffect", table);
    }

    [Test]
    public void GetAbilityNameShort()
    {
        var table = new SnapshotTable(
            "Stat.GetAbilityNameShort(AbilityType type)",
            "The three-letter label of an attribute; anything unknown is INV.",
            "Every AbilityType member plus two undefined values.",
            new[] { "abilityType", "abilityName" },
            new[] { "label" });

        var types = Enum.GetValues<AbilityType>()
            .Concat(new[] { (AbilityType)6, (AbilityType)(-2) })
            .OrderBy(type => (int)type);
        foreach (var type in types)
            table.Add((int)type, Enum.IsDefined(type) ? type.ToString() : "(undefined)", Stat.GetAbilityNameShort(type));

        Snapshot.Verify("Stat", "GetAbilityNameShort", table);
    }

    [Test]
    public void IsNaturalStaminaRegenerationAvailable()
    {
        var table = new SnapshotTable(
            "Stat.IsNaturalStaminaRegenerationAvailable(long availableAtTicks, long currentTicks)",
            "Natural regeneration is available when no wait was set (zero or less) or the wait has passed.",
            "available-at ticks {-1,0,1,100,1000} x current ticks {-1,0,1,99,100,101,1000,2000}.",
            new[] { "availableAtTicks", "currentTicks" },
            new[] { "available" });

        foreach (var availableAt in new[] { -1L, 0L, 1L, 100L, 1000L })
        {
            foreach (var current in new[] { -1L, 0L, 1L, 99L, 100L, 101L, 1000L, 2000L })
                table.Add(availableAt, current, Stat.IsNaturalStaminaRegenerationAvailable(availableAt, current));
        }

        Snapshot.Verify("Stat", "IsNaturalStaminaRegenerationAvailable", table);
    }

    [Test]
    public void GetBeastNaturalRegenAmount()
    {
        var table = new SnapshotTable(
            "Stat.GetBeastNaturalRegenAmount(int attribute)",
            "FP or stamina a beast restores per heartbeat: 1 plus one per ten points of the governing attribute (never negative).",
            "attribute {-10,-1,0,1,9,10,11,19,20,55,100,999}.",
            new[] { "attribute" },
            new[] { "amount" });

        foreach (var attribute in new[] { -10, -1, 0, 1, 9, 10, 11, 19, 20, 55, 100, 999 })
            table.Add(attribute, Stat.GetBeastNaturalRegenAmount(attribute));

        Snapshot.Verify("Stat", "GetBeastNaturalRegenAmount", table);
    }

    private static SnapshotTable RatingTable(string function, string description)
    {
        return new SnapshotTable(
            function,
            description,
            "level {-1,0,1,10,30,60,100} x stat {-5,0,10,50,150} x bonus {-20,0,15,100}.",
            new[] { "level", "stat", "bonus" },
            new[] { "rating" });
    }

    private static IEnumerable<int[]> RatingGrid()
    {
        return Grid.Cross(
            new[] { -1, 0, 1, 10, 30, 60, 100 },
            new[] { -5, 0, 10, 50, 150 },
            new[] { -20, 0, 15, 100 });
    }
}
