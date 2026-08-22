using FluentAssertions;
using Microsoft.VisualBasic.FileIO;
using NUnit.Framework;
using System.Reflection;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class StanceStatusEffectTests
{
    private const uint Player = 0x01000001;

    [SetUp]
    public void SetUp()
    {
        StatusEffect.CacheData();
        ResetStatusEffects();
    }

    [TearDown]
    public void TearDown()
    {
        ResetStatusEffects();
    }

    [Test]
    public void StanceStatusEffects_UseExclusiveStanceSourceType()
    {
        var stanceTypes = typeof(BerserkerStanceStatusEffect).Assembly
            .GetTypes()
            .Where(type =>
                type.Namespace == typeof(BerserkerStanceStatusEffect).Namespace &&
                type.Name.EndsWith("StanceStatusEffect") &&
                !type.IsAbstract)
            .ToList();

        stanceTypes.Should().NotBeEmpty();

        foreach (var stanceType in stanceTypes)
        {
            var statusEffect = (IStatusEffect)Activator.CreateInstance(stanceType)!;
            statusEffect.SourceType.Should().Be(
                StatusEffectSourceType.Stance,
                $"{stanceType.Name} should deactivate other active stances when applied");
        }
    }

    [Test]
    public void BlazingSpikes_UsesExclusiveStanceSourceType()
    {
        new BlazingSpikesStatusEffect().SourceType.Should().Be(StatusEffectSourceType.Stance);
    }

    [Test]
    public void SpotterStance_AggregatesCombatStatAdjustments()
    {
        AddActiveEffect(Player, new SpotterStanceStatusEffect());

        Stat.GetStatAdjustmentExcludingTemporaryModifiers(Player, StatType.AccuracyPercentAdjustment)
            .Should()
            .Be(15);
        Stat.GetStatAdjustmentExcludingTemporaryModifiers(Player, StatType.RangedEvasionPercentAdjustment)
            .Should()
            .Be(15);
        Stat.GetStatAdjustmentExcludingTemporaryModifiers(Player, StatType.AttackDelayReductionPercent)
            .Should()
            .Be(-10);
    }

    [Test]
    public void BibleStanceToggleStatusEffects_UseExclusiveStanceSourceType()
    {
        var failures = new List<string>();

        foreach (var (description, statusEffectType) in GetBibleStanceStatusEffects())
        {
            var statusEffect = (IStatusEffect)Activator.CreateInstance(statusEffectType)!;
            if (statusEffect.SourceType != StatusEffectSourceType.Stance)
            {
                failures.Add(
                    $"{description}: {statusEffectType.Name} should use {StatusEffectSourceType.Stance} but uses {statusEffect.SourceType}.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void BibleStanceLimitedToggles_RemovePreviousStanceBeforeApplying()
    {
        var failures = new List<string>();

        foreach (var (description, statusEffectType) in GetBibleStanceStatusEffects())
        {
            ResetStatusEffects();
            AddActiveEffect(Player, new TestStanceAStatusEffect());

            StatusEffect.RemoveOtherStanceStatuses(
                Player,
                statusEffectType,
                removeNativeEffect: false);

            var remainingEffects = StatusEffect.GetCreatureStatusEffects(Player).GetAllEffects();
            if (remainingEffects.Count != 0)
            {
                failures.Add(
                    $"{description}: activating {statusEffectType.Name} left {remainingEffects.Count} previous stance status effect(s) active.");
            }
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void ActivatingActiveStanceAgain_RemovesTheStance()
    {
        AddActiveEffect(Player, new TestStanceAStatusEffect());

        StatusEffect.RemoveStatusEffect(
            Player,
            typeof(TestStanceAStatusEffect),
            sendsWornOffMessage: false,
            removeNativeEffect: false);

        StatusEffect.GetCreatureStatusEffects(Player).GetAllEffects().Should().BeEmpty();
    }

    [Test]
    public void ActivatingDifferentStance_RemovesPreviousStanceBeforeApplyingNewOne()
    {
        AddActiveEffect(Player, new TestStanceAStatusEffect());

        StatusEffect.RemoveOtherStanceStatuses(
            Player,
            typeof(TestStanceBStatusEffect),
            removeNativeEffect: false);
        AddActiveEffect(Player, new TestStanceBStatusEffect());

        StatusEffect.GetCreatureStatusEffects(Player)
            .GetAllEffects()
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<TestStanceBStatusEffect>();
    }

    private static void AddActiveEffect(uint creature, IStatusEffect statusEffect)
    {
        statusEffect.ApplyEffect(creature, creature, -1);
        var tracker = GetOrCreateCreatureEffects(creature);
        tracker.Add(statusEffect);
    }

    private static CreatureStatusEffect GetOrCreateCreatureEffects(uint creature)
    {
        var effects = CreatureEffects();
        if (!effects.TryGetValue(creature, out var tracker))
        {
            tracker = new CreatureStatusEffect();
            effects[creature] = tracker;
        }

        return tracker;
    }

    private static void ResetStatusEffects()
    {
        CreatureEffects().Remove(Player);
    }

    private static IReadOnlyList<BibleStanceStatusEffect> GetBibleStanceStatusEffects()
    {
        var root = FindRepositoryRoot();
        var rows = ReadBibleStanceRows(root / "SWLOR.Game.Server" / "Readmes" / "CombatUpgradeBiblePerkManifest.csv");
        var abilities = BuildAbilities();
        var result = new List<BibleStanceStatusEffect>();
        var failures = new List<string>();

        foreach (var row in rows)
        {
            // A Bible "Stance" row maps to the toggle ability that cleans its stance status on refund.
            // Mimicry stances deliberately share their display name with the NPC ability they mimic, so
            // match on the toggle (exactly one refund-cleaned status effect) rather than the name alone.
            var matches = abilities
                .Where(ability => ability.Name == row.PerkName && ability.StatusEffectTypesRemovedOnPerkRefund.Count == 1)
                .ToArray();

            if (matches.Length != 1)
            {
                failures.Add($"{row.Description}: expected one toggle ability named '{row.PerkName}' but found {matches.Length}.");
                continue;
            }

            var statusEffectTypes = matches[0].StatusEffectTypesRemovedOnPerkRefund;
            if (statusEffectTypes.Count != 1)
            {
                failures.Add($"{row.Description}: expected one refund-cleaned status effect but found {statusEffectTypes.Count}.");
                continue;
            }

            result.Add(new BibleStanceStatusEffect(row.Description, statusEffectTypes[0]));
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
        result.Should().NotBeEmpty();
        return result;
    }

    private static IReadOnlyList<AbilityDetail> BuildAbilities()
    {
        var result = new List<AbilityDetail>();
        var definitionTypes = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
            result.AddRange(definition.BuildAbilities().Values);
        }

        return result;
    }

    private static IReadOnlyList<BibleStanceRow> ReadBibleStanceRows(PathInfo path)
    {
        using var parser = new TextFieldParser(path.FullName);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.ReadFields();

        var rows = new List<BibleStanceRow>();
        while (!parser.EndOfData)
        {
            var cells = parser.ReadFields();
            if (cells == null || cells.Length < 18)
                continue;

            if (!cells[7].Equals("Stance", StringComparison.OrdinalIgnoreCase) ||
                !IsImplementedStatus(cells[17]))
            {
                continue;
            }

            rows.Add(new BibleStanceRow(cells[0], cells[1], cells[2], cells[4]));
        }

        return rows;
    }

    private static bool IsImplementedStatus(string devStatus)
    {
        return devStatus.Equals("Implemented", StringComparison.OrdinalIgnoreCase) ||
               devStatus.Equals("Design Added", StringComparison.OrdinalIgnoreCase);
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR.Game.Server", "Readmes", "CombatUpgradeBiblePerkManifest.csv")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private static Dictionary<uint, CreatureStatusEffect> CreatureEffects()
    {
        return (Dictionary<uint, CreatureStatusEffect>)typeof(StatusEffect)
            .GetField("_creatureEffects", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }

    private sealed record BibleStanceRow(string Tab, string Row, string Style, string PerkName)
    {
        public string Description => $"{Tab}/{Style}/{Row} {PerkName}";
    }

    private sealed record BibleStanceStatusEffect(string Description, Type StatusEffectType);

    public sealed class TestStanceAStatusEffect : StatusEffectBase
    {
        public override string Name => "Test Stance A";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
    }

    public sealed class TestStanceBStatusEffect : StatusEffectBase
    {
        public override string Name => "Test Stance B";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.Stance;
    }
}
