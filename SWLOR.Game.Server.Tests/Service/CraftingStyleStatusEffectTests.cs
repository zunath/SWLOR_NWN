using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Pins the crafting style classification: a style (Overclock, Safe Mode, and the Smithery, Agriculture and Gathering styles that
/// follow) is a <see cref="StatusEffectSourceType.CraftingStyle"/> status effect, and no cleanse, purge, dispel, stat-based
/// removal or clear on death takes one. The in-engine half is <c>CraftingStyleStatusEngineTests</c>.
/// </summary>
public class CraftingStyleStatusEffectTests
{
    private static readonly StatType MoveSpeed = StatType.MovementSpeedPercentAdjustment;

    private static readonly string[] BulkRemovals =
    {
        "RemoveCleanseableStatusEffects",
        "RemoveFirstCleanseableStatusEffect",
        "RemoveFirstStatusEffectByCategory",
        "RemoveFirstBeneficialCombatStatusEffect",
        "RemoveStatusEffectsWithStat",
        "RemoveStatusEffectsWithNegativeStat",
        "RemoveAllStatusEffects"
    };

    /// <summary>
    /// The public removals that name what they remove: a class, a class and source, a source type (Stance, Command, or
    /// CraftingStyle if a caller asks for it), or the creature itself (logout, a failed application that left nothing). A style
    /// leaves through these only when something asks for it. A new public removal that picks effects by a property of the effect
    /// (a category, a cleanse type, a stat, "any beneficial one") belongs in StatusEffectSelectors instead, and in
    /// <see cref="BulkRemovals"/>.
    /// </summary>
    private static readonly string[] RemovalsThatNameTheirTarget =
    {
        "RemoveStatusEffect",
        "RemoveFirstStatusEffect",
        "RemoveStatusEffectFromAllTargetsBySource",
        "RemoveStatusEffectsFromAllTargetsBySource",
        "RemoveOtherStanceStatuses",
        "RemoveOtherCommandStatuses",
        "RemoveStatusEffectBySourceType",
        "RemoveCreature"
    };

    private sealed class ProbeEffect : StatusEffectBase
    {
        private readonly StatusEffectSourceType _sourceType;
        private readonly StatusEffectCategory _categories;
        private readonly StatusEffectCleanseType _cleanseTypes;

        // The status effect cache creates every StatusEffectBase subclass it finds, this one included.
        public ProbeEffect() : this(StatusEffectSourceType.Normal, StatusEffectCategory.None, StatusEffectCleanseType.None)
        {
        }

        public ProbeEffect(StatusEffectSourceType sourceType, StatusEffectCategory categories, StatusEffectCleanseType cleanseTypes,
            params (StatType Stat, int Value)[] stats)
        {
            _sourceType = sourceType;
            _categories = categories;
            _cleanseTypes = cleanseTypes;
            foreach (var (stat, value) in stats)
                StatGroup.Stats[stat] = value;
        }

        public override string Name => "Probe";
        public override EffectIconType Icon => EffectIconType.Invalid;
        public override StatusEffectSourceType SourceType => _sourceType;
        public override StatusEffectCategory Categories => _categories;
        public override StatusEffectCleanseType CleanseTypes => _cleanseTypes;
    }

    // A style and an ordinary effect that match every selector the same way, so only the classification tells them apart.
    private static (ProbeEffect Style, ProbeEffect Ordinary) Pair() => (
        new ProbeEffect(StatusEffectSourceType.CraftingStyle, StatusEffectCategory.Buff | StatusEffectCategory.Debuff,
            StatusEffectCleanseType.Purify | StatusEffectCleanseType.TreatmentKit2, (MoveSpeed, -10), (StatType.CircuitLoadBoostPercent, 5)),
        new ProbeEffect(StatusEffectSourceType.Normal, StatusEffectCategory.Buff | StatusEffectCategory.Debuff,
            StatusEffectCleanseType.Purify | StatusEffectCleanseType.TreatmentKit2, (MoveSpeed, -10), (StatType.CircuitLoadBoostPercent, 5)));

    private static IStatusEffect[] RealStyles() => CircuitStyles.All
        .Select(style => (IStatusEffect)Activator.CreateInstance(CircuitStyles.StatusEffectOf(style))!)
        .Concat(ForgeStyles.All.Select(style => (IStatusEffect)Activator.CreateInstance(ForgeStyles.StatusEffectOf(style))!))
        .ToArray();

    [Test]
    public void EveryCircuitAndForgeStyleIsACraftingStyle_PassiveAndGoneOnLogout_YetStillABuff()
    {
        var styles = RealStyles();
        styles.Should().HaveCount(CircuitStyles.All.Count + ForgeStyles.All.Count);
        foreach (var style in styles)
        {
            style.SourceType.Should().Be(StatusEffectSourceType.CraftingStyle, style.Name);
            StatusEffectSelectors.IsCraftingStyle(style).Should().BeTrue(style.Name);
            style.ActivationType.Should().Be(StatusEffectActivationType.Passive, style.Name);
            style.PersistsOnLogout.Should().BeFalse(style.Name);
            // The premise of the fix: the category alone marks a purge target, so the classification has to be what protects it.
            (style.Categories & StatusEffectCategory.Buff).Should().Be(StatusEffectCategory.Buff, style.Name);
        }
    }

    [Test]
    public void TheBulkSelectors_NeverReturnARealStyle()
    {
        var styles = RealStyles();

        StatusEffectSelectors.BeneficialInCombat(styles).Should().BeEmpty("a purge of one beneficial effect is the reported path");
        StatusEffectSelectors.WithCategory(styles, StatusEffectCategory.Buff).Should().BeEmpty();
        StatusEffectSelectors.WithStat(styles, StatType.CircuitLoadBoostPercent).Should().BeEmpty();
        StatusEffectSelectors.WithNegativeStat(styles, StatType.CircuitLoadBoostPercent).Should().BeEmpty("Safe Mode lowers it");
        StatusEffectSelectors.WithStat(styles, StatType.ForgePressOutputPercent).Should().BeEmpty();
        StatusEffectSelectors.WithNegativeStat(styles, StatType.ForgeHoneOutputPercent).Should().BeEmpty("Hot Forging lowers it");
        StatusEffectSelectors.Removable(styles).Should().BeEmpty();
        StatusEffectSelectors.Removable(styles, includeCraftingStyles: true).Should().BeEquivalentTo(styles,
            "a full reset takes them when it asks for them");
    }

    [Test]
    public void EachSelector_SkipsAStyleThatMatchesAndKeepsAnOrdinaryEffectThatMatches()
    {
        var (style, ordinary) = Pair();
        var both = new IStatusEffect[] { style, ordinary };

        StatusEffectSelectors.WithCleanseType(both, StatusEffectCleanseType.Purify).Should().Equal(ordinary);
        StatusEffectSelectors.WithCleanseType(both, StatusEffectCleanseType.TreatmentKit2).Should().Equal(ordinary);
        StatusEffectSelectors.WithCategory(both, StatusEffectCategory.Buff).Should().Equal(ordinary);
        StatusEffectSelectors.WithCategory(both, StatusEffectCategory.Debuff).Should().Equal(ordinary);
        StatusEffectSelectors.BeneficialInCombat(both).Should().Equal(ordinary);
        StatusEffectSelectors.WithStat(both, MoveSpeed).Should().Equal(ordinary);
        StatusEffectSelectors.WithStat(both, StatType.CircuitLoadBoostPercent).Should().Equal(ordinary);
        StatusEffectSelectors.WithNegativeStat(both, MoveSpeed).Should().Equal(ordinary);
        StatusEffectSelectors.Removable(both).Should().Equal(ordinary);
        StatusEffectSelectors.Removable(both, includeCraftingStyles: true).Should().Equal(style, ordinary);
    }

    [Test]
    public void AnEffectOfAnotherSourceTypeIsNeverACraftingStyle()
    {
        foreach (var sourceType in Enum.GetValues<StatusEffectSourceType>().Where(type => type != StatusEffectSourceType.CraftingStyle))
        {
            StatusEffectSelectors.IsCraftingStyle(
                new ProbeEffect(sourceType, StatusEffectCategory.Buff, StatusEffectCleanseType.None)).Should().BeFalse(sourceType.ToString());
        }
    }

    [Test]
    public void EveryBulkRemovalInTheStatusEffectService_ChoosesItsEffectsThroughTheSelectors()
    {
        var methods = StatusEffectMethods();

        foreach (var name in BulkRemovals)
        {
            var method = methods.Single(method => method.Identifier.ValueText == name);
            Regex.Replace(method.ToString(), @"\s+", string.Empty).Should().Contain("StatusEffectSelectors.", $"{name} must not remove a crafting style");
        }
    }

    [Test]
    public void EveryPublicRemovalInTheStatusEffectService_IsEitherGuardedBySelectorsOrNamesWhatItRemoves()
    {
        var publicRemovals = StatusEffectMethods()
            .Where(method => method.Identifier.ValueText.StartsWith("Remove") &&
                             method.Modifiers.Any(modifier => modifier.Kind() == SyntaxKind.PublicKeyword))
            .Select(method => method.Identifier.ValueText)
            .Distinct()
            .ToList();

        publicRemovals.Should().Contain(BulkRemovals, "the pinned names must still exist");
        publicRemovals.Should().Contain(RemovalsThatNameTheirTarget, "the pinned names must still exist");
        publicRemovals.Except(BulkRemovals).Except(RemovalsThatNameTheirTarget).Should().BeEmpty(
            "a new public removal either picks its effects with StatusEffectSelectors or names the class, source or creature it " +
            "removes; add it to BulkRemovals or RemovalsThatNameTheirTarget with that reason");
    }

    [Test]
    public void OnlyAFullResetAsksForCraftingStylesToBeRemoved()
    {
        var root = FindRepositoryRoot();
        var callers = Directory.EnumerateFiles(Path.Combine(root.FullName, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                           !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(path => File.ReadAllText(path).Contains("includeCraftingStyles: true"))
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name)
            .ToList();

        callers.Should().Equal("CharacterFullRebuildViewModel.cs", "Space.cs");
    }

    private static List<MethodDeclarationSyntax> StatusEffectMethods()
    {
        var root = FindRepositoryRoot();
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "StatusEffect.cs"))).GetRoot();
        return syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().ToList();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }
}
