using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Pins that each hook point of the Edict effects (Player City Revamp, "Edict hook points") still calls the helper that applies the
/// effect. The engine-bound code cannot run in a unit test, so the math is tested in <see cref="EdictBonusRulesTests"/> and
/// <see cref="EdictFacilityAccessTests"/>, and this class checks the wiring that feeds them.
/// </summary>
public class EdictEffectsWiringTests
{
    private static string ReadServerSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }

    [Test]
    public void DeathDebt_HalvesTheMedicalCenterBonusWhileASituationHasItRationing()
    {
        var method = ExtractMethod(ReadServerSource("Service", "Death.cs"), "public static int ApplyPenalties(uint player)");

        method.Should().Contain("EdictBonusRules.GetMedicalCenterReduction(");
        method.Should().Contain("PropertyUpgradeType.MedicalCenterLevel");
        method.Should().Contain("EdictEffects.IsMedicalCenterHalved(dbPlayer)");
    }

    [Test]
    public void GuildTasks_AddTheCharterToBothTheGuildPointsAndTheCredits()
    {
        var points = ExtractMethod(ReadServerSource("Service", "Guild.cs"), "public static int CalculateGPReward(uint player, GuildType guild, int baseAmount)");
        var credits = ExtractMethod(ReadServerSource("Service", "Quest.cs"), "public static int CalculateQuestGoldReward(uint player, bool isGuildQuest, int baseAmount)");

        points.Should().Contain("EdictEffects.GetGuildCharterBonus(dbPlayer)").And.Contain("(charterBonus * baseAmount)");
        credits.Should().Contain("EdictEffects.GetGuildCharterBonus(");
        credits.IndexOf("EdictEffects.GetGuildCharterBonus(", StringComparison.Ordinal)
            .Should().BeGreaterThan(credits.IndexOf("if (isGuildQuest)", StringComparison.Ordinal), "only guild quests get the charter");
    }

    [Test]
    public void CombatXp_PassesHuntingGroundsToEveryAwardOfAKill_AndNothingElseDoes()
    {
        var combat = ReadServerSource("Service", "CombatPoint.cs");

        combat.Should().Contain("EdictEffects.GetHuntingGroundsPoints(dbPlayer, GetArea(player))");
        combat.Split("extraBonusPoints: huntingGroundsPoints").Length.Should().Be(3, "the shared-points award and the exempt-skill award each pass it once");

        var skill = ReadServerSource("Service", "Skill.cs");
        skill.Should().Contain("bonusPercentage += extraBonusPoints / 100f;");
        skill.IndexOf("bonusPercentage += extraBonusPoints / 100f;", StringComparison.Ordinal)
            .Should().BeGreaterThan(skill.IndexOf("if (!ignoreBonuses)", StringComparison.Ordinal), "ignored bonuses ignore the extra points too");
    }

    [Test]
    public void ShipRepairs_AddTheDrydockSubsidyToTheStarportDiscount()
    {
        var method = ExtractMethod(
            ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ShipManagementViewModel.cs"),
            "private int CalculateRepairBill(PlayerShip ship)");

        method.Should().Contain("EdictEffects.GetDrydockSubsidyDiscount(dbPlayer)");
        method.Should().Contain("PropertyUpgradeType.StarportLevel");
    }

    [Test]
    public void Crafting_RecordsTheStationCityAtStart_AndScalesTheXpAtSettlement()
    {
        var lifecycle = ReadServerSource("Service", "CraftService", "Minigame", "CraftSessionLifecycle.cs");

        lifecycle.Should().Contain("CityId = _world.GetStationCityId(player)");
        ExtractMethod(lifecycle, "private int PacedBaseXp(").Should()
            .Contain("EdictBonusRules.ScaleCraftXp(paced, _world.GetCraftXpBonusPoints(dbPlayer, record.CityId))");
        lifecycle.Split("PacedBaseXp(dbPlayer, record, recipe").Length.Should().Be(3, "a success and a failure both settle through it");

        var world = ReadServerSource("Service", "CraftService", "Minigame", "NwnCraftSessionWorld.cs");
        world.Should().Contain("Property.GetCityIdForArea(GetArea(player))");
        world.Should().Contain("EdictEffects.GetArtisansCharterPoints(dbPlayer, craftCityId)");
    }

    [Test]
    public void Incubation_AddsTheBreedersCharterToEveryPurityStatExceptMutationChanceAndTheXpPenalty()
    {
        var method = ExtractMethod(
            ReadServerSource("Feature", "GuiDefinition", "ViewModel", "IncubatorViewModel.cs"),
            "private void StartJob(IncubationJob job)");

        method.Should().Contain("EdictEffects.HasBreedersCharter(");
        method.Should().Contain("EdictBonusRules.GetBreedersCharterBonus(stageGain, MaxStageIncrease)");

        var withBonus = new[]
        {
            "_attack", "_accuracy", "_evasion", "_learning", "_physicalDefense", "_forceDefense", "_iceResistance", "_fireResistance",
            "_poisonResistance", "_electricalResistance", "_mindResistance", "_mobilityResistance", "_traumaResistance", "_disruptionResistance"
        };
        foreach (var stat in withBonus)
        {
            var stage = "_stage" + char.ToUpperInvariant(stat[1]) + stat[2..];
            method.Should().Contain($"BreedersBonus({stage})", $"{stat} gets the bonus");
        }

        method.Should().NotContain("BreedersBonus(_stageMutationChance)");
        method.Should().NotContain("BreedersBonus(_stageXPPenalty)");

        // The bonus is added after the stage cap and before the 100% cap.
        method.Should().Contain("MaxStageIncrease : _stageAttack) + BreedersBonus(_stageAttack);");
        method.Should().Contain("attackPurity > MaxStat ? MaxStat : attackPurity");
    }

    [Test]
    public void BankAndStarport_BothApplyTheAccessOrderThroughOneHelper()
    {
        var bank = ExtractMethod(ReadServerSource("Service", "Bank.cs"), "public static string GetCityBankAccessFailure(uint player, uint bank)");
        var starport = ExtractMethod(
            ReadServerSource("Feature", "PropertyLayoutDefinition", "StarportLayoutDefinition.cs"),
            "private static bool CanAccess(uint player)");

        bank.Should().Contain("EdictEffects.GetFacilityFailure(dbPlayer, cityId, FacilityAccessRules.BankNotWelcomeMessage)");
        bank.Should().NotContain("StarportDeparture", "the departure pass reaches no Bank");
        starport.Should().Contain("EdictEffects.GetFacilityFailure(dbPlayer, cityId, FacilityAccessRules.StarportNotWelcomeMessage)");
        starport.Should().Contain("StarportDeparture.HasPass(dbPlayer, cityId)");
        starport.IndexOf("Station.StationCityVariable", StringComparison.Ordinal)
            .Should().BeLessThan(starport.IndexOf("GetFacilityFailure", StringComparison.Ordinal), "a station's flights terminal still admits everyone first");
    }

    [Test]
    public void Docking_StartsTheDeparturePass_AndTheBootSweepMovesStrandedShips()
    {
        var dock = ReadServerSource("Feature", "DialogDefinition", "StarportDockDialog.cs");
        var property = ReadServerSource("Service", "Property.cs");

        dock.Should().Contain("StarportDeparture.OnArrived(playerId, dbProperty.OwnerPlayerId, dbStarport.Id)");
        property.IndexOf("StarportDeparture.ReturnStrandedShip(property);", StringComparison.Ordinal)
            .Should().BePositive().And.BeLessThan(property.IndexOf("ReturnStarshipToSafeDock(property);", StringComparison.Ordinal));
    }

    [Test]
    public void StationBuildings_ApplyEmbargoAndAllianceToTheBeaconAndTheShipyard()
    {
        var source = ReadServerSource("Service", "StationBuildings.cs");

        source.Should().Contain("EdictEffects.IsAllyOf(DB.Get<WorldProperty>(beacon.StationId), dbPlayer)");
        source.Should().Contain("EdictEffects.IsEmbargoedBy(station, DB.Get<Player>(GetObjectUUID(player)))");
        source.Split("IsEmbargoed(player,").Length.Should().Be(5, "the shipyard check and the three beacon checks");
    }
}
