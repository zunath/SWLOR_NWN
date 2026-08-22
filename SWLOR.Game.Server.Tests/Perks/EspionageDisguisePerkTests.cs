using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

public class EspionageDisguisePerkTests
{
    [Test]
    public void FalseIdentities_GrantsOneSlotPerRank()
    {
        var perk = BuildPerk("FalseIdentities", PerkType.FalseIdentities);

        perk.PerkLevels.Count.Should().Be(3);

        AssertLevel(perk.PerkLevels[1], 2, 10, StatType.AdditionalDisguiseSlots, 1);
        AssertLevel(perk.PerkLevels[2], 3, 28, StatType.AdditionalDisguiseSlots, 2);
        AssertLevel(perk.PerkLevels[3], 4, 44, StatType.AdditionalDisguiseSlots, 3);

        // Only the first rank anchors the passive trait feat; higher ranks replace the stat value.
        perk.PerkLevels[1].GrantedFeats.Should().Contain(FeatType.FalseIdentitiesTrait);
        perk.PerkLevels[2].GrantedFeats.Should().BeEmpty();
        perk.PerkLevels[3].GrantedFeats.Should().BeEmpty();
    }

    [Test]
    public void CoverStory_ReducesTheActivationDelay()
    {
        var perk = BuildPerk("CoverStory", PerkType.CoverStory);

        perk.PerkLevels.Count.Should().Be(2);

        AssertLevel(perk.PerkLevels[1], 3, 20, StatType.DisguiseSwapCooldownReductionPercent, 40);
        AssertLevel(perk.PerkLevels[2], 3, 40, StatType.DisguiseSwapCooldownReductionPercent, 70);

        perk.PerkLevels[1].GrantedFeats.Should().Contain(FeatType.CoverStoryTrait);
        perk.PerkLevels[2].GrantedFeats.Should().BeEmpty();
    }

    [Test]
    public void DisguiseSlotLimit_AddsThePerkStatToTheBaseAllowance()
    {
        Disguise.CalculateDisguiseSlotLimit(Player.DefaultDisguiseSlotLimit, 0).Should().Be(1);
        Disguise.CalculateDisguiseSlotLimit(Player.DefaultDisguiseSlotLimit, 1).Should().Be(2);
        Disguise.CalculateDisguiseSlotLimit(Player.DefaultDisguiseSlotLimit, 2).Should().Be(3);
        Disguise.CalculateDisguiseSlotLimit(Player.DefaultDisguiseSlotLimit, 3).Should().Be(4);
    }

    [Test]
    public void DisguiseSlotLimit_StacksOnTopOfAnAdministrativeGrant()
    {
        Disguise.CalculateDisguiseSlotLimit(3, 3).Should().Be(6);
    }

    [Test]
    public void DisguiseSlotLimit_NeverFallsBelowTheDefaultAllowance()
    {
        Disguise.CalculateDisguiseSlotLimit(Player.DefaultDisguiseSlotLimit, -5)
            .Should().Be(Player.DefaultDisguiseSlotLimit);
    }

    [Test]
    public void ActivationDelay_MatchesTheDesignedCoverStoryBands()
    {
        Disguise.CalculateActivationDelay(0).TotalMinutes.Should().Be(30);
        Disguise.CalculateActivationDelay(40).TotalMinutes.Should().Be(18);
        Disguise.CalculateActivationDelay(70).TotalMinutes.Should().Be(9);
    }

    [Test]
    public void ActivationDelay_IsFlooredSoStackedReductionsCannotRemoveIt()
    {
        Disguise.CalculateActivationDelay(100).TotalMinutes
            .Should().Be(Disguise.MinimumActivationDelayMinutes);
        Disguise.CalculateActivationDelay(500).TotalMinutes
            .Should().Be(Disguise.MinimumActivationDelayMinutes);
    }

    [Test]
    public void ActivationDelay_IgnoresNegativeReductions()
    {
        Disguise.CalculateActivationDelay(-40).TotalMinutes.Should().Be(Disguise.ActivationDelayMinutes);
    }

    [Test]
    public void DisguiseWindow_ReportsTheEffectiveDelayRatherThanTheBaseDelay()
    {
        var root = FindRepositoryRoot();
        var viewModel = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "DisguiseViewModel.cs"));
        var definition = File.ReadAllText(Path.Combine(
            root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "DisguiseDefinition.cs"));

        // Every surface that quotes the delay has to quote the player's own delay, or a player
        // with Cover Story is told 30 minutes while actually waiting 18 or 9.
        viewModel.Should().Contain("Disguise.GetActivationDelay(Player).TotalMinutes");
        viewModel.Should().Contain("ActivationDelayNote = $\"Activating starts a {GetActivationDelayMinutes()}-minute cooldown");
        definition.Should().Contain("BindText(model => model.ActivationDelayNote)");

        viewModel.Should().NotContain("30-minute");
        definition.Should().NotContain("30-minute");
    }

    [Test]
    public void DisguisePerkChanges_RefreshTheirVisibleEffectsWithoutReloadingTheDisguiseList()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "DisguiseViewModel.cs"));
        var purchaseRefresh = ExtractMethod(source, "public void Refresh(PerkAcquiredRefreshEvent payload)");
        var refundRefresh = ExtractMethod(source, "public void Refresh(PerkRefundedRefreshEvent payload)");
        var capacityRefresh = ExtractMethod(source, "private void RefreshSlotCapacity(string playerId, Player dbPlayer)");
        var delayRefresh = ExtractMethod(source, "private void RefreshActivationDelayNote()");
        var perkRefresh = ExtractMethod(source, "private void RefreshPerkDependentBindings(PerkType perkType)");

        source.Should().Contain("IGuiRefreshable<PerkAcquiredRefreshEvent>");
        source.Should().Contain("IGuiRefreshable<PerkRefundedRefreshEvent>");
        capacityRefresh.Should().Contain("Disguise.GetDisguiseSlotLimit(Player, dbPlayer)");
        capacityRefresh.Should().Contain("SlotBarLabel = $\"Disguise Slots   {usedSlots} / {slotLimit}\"");
        capacityRefresh.Should().Contain("SlotUsageProgress = slotLimit <= 0");
        capacityRefresh.Should().Contain("SlotUsageColor = usedSlots >= slotLimit");
        delayRefresh.Should().Contain("ActivationDelayNote = $\"Activating starts a {GetActivationDelayMinutes()}-minute cooldown");
        perkRefresh.Should().Contain("case PerkType.FalseIdentities:");
        perkRefresh.Should().Contain("RefreshSlotCapacity();");
        perkRefresh.Should().Contain("case PerkType.CoverStory:");
        perkRefresh.Should().Contain("RefreshActivationDelayNote();");

        foreach (var refreshMethod in new[] { purchaseRefresh, refundRefresh })
        {
            refreshMethod.Should().Contain("RefreshPerkDependentBindings(payload.Type);");
            refreshMethod.Should().NotContain("LoadList(");
        }
    }

    [Test]
    public void DisguiseService_ReadsThePerkStatsRatherThanCheckingPerksDirectly()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "SWLOR.Game.Server",
            "Service",
            "Disguise.cs"));

        source.Should().Contain("Stat.GetStatAdjustment(player, StatType.AdditionalDisguiseSlots)");
        source.Should().Contain("Stat.GetStatAdjustment(player, StatType.DisguiseSwapCooldownReductionPercent)");
        source.Should().NotContain("PerkType.FalseIdentities");
        source.Should().NotContain("PerkType.CoverStory");
    }

    private static void AssertLevel(PerkLevel level, int price, int espionageRank, StatType stat, int amount)
    {
        level.Price.Should().Be(price);

        var requirement = level.Requirements
            .OfType<PerkRequirementSkill>()
            .Should()
            .ContainSingle()
            .Which;
        requirement.Type.Should().Be(SkillType.Espionage);
        requirement.RequiredRank.Should().Be(espionageRank);

        var bonus = level.StatBonuses.Should().ContainSingle(x => x.Stat == stat).Which;

        // These are flat bonuses, so the calculation ignores the creature argument.
        bonus.Calculate(0).Should().Be(amount);
    }

    private static PerkDetail BuildPerk(string methodName, PerkType perkType)
    {
        var definition = new EspionagePerkDefinition();
        typeof(EspionagePerkDefinition)
            .GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(definition, null);

        var builder = typeof(EspionagePerkDefinition)
            .GetField("_builder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(definition);

        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
            .GetField("_perks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(builder)!;

        return perks[perkType];
    }

    private static string ExtractMethod(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        signatureIndex.Should().BeGreaterThanOrEqualTo(0);

        var openingBraceIndex = source.IndexOf('{', signatureIndex);
        openingBraceIndex.Should().BeGreaterThan(signatureIndex);

        var depth = 0;
        for (var index = openingBraceIndex; index < source.Length; index++)
        {
            switch (source[index])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                        return source[signatureIndex..(index + 1)];
                    break;
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
