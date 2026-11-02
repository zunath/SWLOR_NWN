using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Pins the wiring of House specializations and City Projects (Player City Revamp, "Homes, gardens, and projects"): where each hook
/// calls the rules, and the order of the steps that move money and items. The engine-bound services cannot run in a unit test, so the
/// rules are tested in <see cref="HouseSpecializationRulesTests"/> and <see cref="CityProjectRulesTests"/>, and this class checks the
/// code that feeds them.
/// </summary>
public class CityProjectWiringTests
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

    private static void ShouldComeBefore(string method, string first, string second)
    {
        var a = method.IndexOf(first, StringComparison.Ordinal);
        var b = method.IndexOf(second, StringComparison.Ordinal);
        a.Should().BeGreaterThanOrEqualTo(0, $"'{first}' should be there");
        b.Should().BeGreaterThanOrEqualTo(0, $"'{second}' should be there");
        a.Should().BeLessThan(b, $"'{first}' should come before '{second}'");
    }

    #region House specializations

    [Test]
    public void Craftsmanship_AndControl_AddTheWorkshopBonus()
    {
        var stat = ReadServerSource("Service", "Stat.cs");

        foreach (var signature in new[]
                 {
                     "public static int CalculateCraftsmanship(uint player, SkillType craftingSkillType)",
                     "public static int CalculateControl(uint player, SkillType craftingSkillType)"
                 })
        {
            var method = ExtractMethod(stat, signature);
            method.Should().Contain("HouseSpecializations.GetWorkshopBonus(player, dbPlayer, craftingSkillType)");
            method.Should().Contain("+ workshopBonus");
        }
    }

    [Test]
    public void ACraft_RecordsTheWorkshopBonus_WhenItOpens()
    {
        var start = ExtractMethod(ReadServerSource("Service", "CraftService", "Minigame", "CraftSessionLifecycle.cs"), "public CraftSessionStartResult Start(");

        start.Should().Contain("WorkshopBonus = _world.GetWorkshopBonus(player, recipe.Skill)");
        start.Should().Contain("CityId = _world.GetStationCityId(player)", "the Workshop and Artisans' Charter are both recorded on the session");
    }

    [Test]
    public void TheWorkshopBonus_IsReadFromTheHouseThePlayerStandsIn()
    {
        ReadServerSource("Service", "CraftService", "Minigame", "NwnCraftSessionWorld.cs")
            .Should().Contain("GetWorkshopBonus(uint player, SkillType skill) => HouseSpecializations.GetLiveWorkshopBonus(player, skill)");

        var live = ExtractMethod(ReadServerSource("Service", "HouseSpecializations.cs"), "public static int GetLiveWorkshopBonus(uint player, SkillType craftSkill)");
        live.Should().Contain("PropertyType.House").And.Contain("HouseSpecializationRules.GetWorkshopBonus(");
    }

    [Test]
    public void ChangingTheSpecialization_IsOneSaveOfTheHouse()
    {
        var change = ExtractMethod(ReadServerSource("Service", "HouseSpecializations.cs"), "public static HouseSpecializationRefusal Change(");

        change.Should().Contain("DB.Get<WorldProperty>(houseId)", "the house is read again, so a stale window cannot apply an old choice");
        ShouldComeBefore(change, "CheckChange(", "HouseSpecializationRules.Apply(");
        ShouldComeBefore(change, "HouseSpecializationRules.Apply(", "DB.Set(house)");
        change.Split("DB.Set(").Length.Should().Be(2, "the type, the timer and the storage are saved together");
    }

    [Test]
    public void TheGreenhouse_IsTheHouseSpecialization_ForPlotsAndForYield()
    {
        var farming = ReadServerSource("Service", "Farming.cs");

        farming.Should().Contain("public static bool IsGreenhouse(WorldProperty property) => HouseSpecializations.IsGreenhouse(property);");
        ExtractMethod(farming, "public static FarmingRefusal GetSiteRefusal(WorldProperty plot, CropDefinition crop)").Should().Contain("HouseSpecializations.IsPlotAboveLimit(plot)");
        ExtractMethod(farming, "public static FarmingResult Plant(").Should().Contain("GetSiteRefusal(structure, crop)");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "GardenPlotViewModel.cs").Should().Contain("Farming.GetSiteRefusal(structure,");
        ReadServerSource("Service", "Property.cs").Should().Contain("FarmingRules.GetPlotLimit(property.PropertyType, Farming.IsGreenhouse(property))");
    }

    [Test]
    public void TheStorage_RefusesDepositsOverTheLimit_AndNeverChecksWithdrawals()
    {
        var storage = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "PropertyItemStorageViewModel.cs");

        ExtractMethod(storage, "public Action OnStoreItem()").Should().Contain("HouseSpecializationRules.CanDeposit(itemCount, property.ItemStorageCount)");
        ExtractMethod(storage, "public Action OnRetrieveItem()").Should().NotContain("CanDeposit").And.NotContain("limit reached", "withdrawals always work");
    }

    [Test]
    public void TheSpecializationWindow_OpensFromManageStructures_ForAHouseInACity()
    {
        var model = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ManageStructuresViewModel.cs");

        model.Should().Contain("IsHouseSpecializationVisible = property.PropertyType == PropertyType.House && HouseSpecializations.IsInCity(property)");
        ExtractMethod(model, "public Action OnOpenHouseSpecialization()").Should().Contain("GuiWindowType.HouseSpecialization");
    }

    [Test]
    public void TheKeeper_IsReadFromThePermissionRecordOnTheHouse_NeverFromTheOwnerOfRecord()
    {
        var service = ReadServerSource("Service", "HouseSpecializations.cs");
        var keeper = ExtractMethod(service, "public static bool IsKeeper(string houseId, string playerId)");

        keeper.Should().Contain("WorldPropertyPermission").And.Contain("HouseSpecializationRules.IsKeeper(record)");
        service.Should().NotContain("OwnerPlayerId", "a house in a city is owned by the mayor at the time, whoever placed it");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "HouseSpecializationViewModel.cs").Should().NotContain("OwnerPlayerId");
        ExtractMethod(service, "public static HouseSpecializationRefusal CheckChange(").Should().Contain("IsKeeper(house.Id, playerId)");
    }

    [Test]
    public void ThePlacerOfAHouse_IsGivenEveryHousePermissionAndTheRightToGrantIt_WhileTheMayorIsTheOwner()
    {
        var property = ReadServerSource("Service", "Property.cs");

        // The premise of the keeper rule, pinned: if either changes, the rule should be looked at again.
        var create = ExtractMethod(property, "private static WorldProperty CreateProperty(");
        create.Should().Contain("creatorPermissions.Permissions[permission] = true;");
        create.Should().Contain("creatorPermissions.GrantPermissions[permission] = true;");
        create.Should().Contain("ownerPermissions.GrantPermissions[permission] = true;");

        ExtractMethod(property, "public static void CreateBuilding(").Should().Contain("city.OwnerPlayerId");
        property.Should().Contain("_permissionsByPropertyType[PropertyType.House] = new List<PropertyPermissionType>");
        var houseList = property.Substring(property.IndexOf("_permissionsByPropertyType[PropertyType.House]", StringComparison.Ordinal), 500);
        houseList.Should().Contain("PropertyPermissionType.EditStructures");
    }

    [Test]
    public void OnlyTheOwnerOfRecord_CanGiveSomeoneTheRightToGrantAHousePermission()
    {
        var window = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "PropertyPermissionsViewModel.cs");

        ExtractMethod(window, "private bool CanAdjustGrantPermission(").Should().Contain("playerId == ownerPlayerId");
    }

    #endregion

    #region City Projects

    [Test]
    public void TheUpgradeButtons_StartProjects_NotInstantUpgrades()
    {
        var city = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");

        foreach (var (method, type) in new[]
                 {
                     ("UpgradeMedicalCenterLevel", "MedicalCenterUpgrade"),
                     ("UpgradeStarportLevel", "StarportUpgrade"),
                     ("UpgradeCantinaLevel", "CantinaUpgrade"),
                     ("UpgradeJumpBeaconLevel", "JumpBeaconUpgrade")
                 })
        {
            ExtractMethod(city, $"public Action {method}()").Should().Contain($"StartProject(CityProjectType.{type})");
        }

        city.Should().NotContain("initialPrice", "the treasury pays through the project's escrow, not at the click");
        city.Should().NotContain("Upgrades[upgradeType] = ", "a project applies the level when it completes");
    }

    [Test]
    public void StartingAProject_IsGatedByTheStartProjectsPower_AndHoldsTheCreditsThroughEscrow()
    {
        var projects = ReadServerSource("Service", "CityProjects.cs");

        ExtractMethod(projects, "public static CityProjectRefusal CheckStart(").Should().Contain("CityPowers.Can(city, playerId, CityPower.StartProjects)");

        var start = ExtractMethod(projects, "public static CityProjectRefusal Start(");
        ShouldComeBefore(start, "DB.Set(project)", "CityTreasury.PostEscrow(");
        start.Should().Contain("restrictedFirst: true");
        ShouldComeBefore(start, "CityTreasury.PostEscrow(", "project.State = CityProjectState.Open");
    }

    [Test]
    public void ADonation_IsRecordedBeforeTheItemsAreTaken_AndOnlyUpToTheNeed()
    {
        var donate = ExtractMethod(ReadServerSource("Service", "CityProjects.cs"), "public static int Donate(");

        donate.Should().Contain("CityProjectRules.Accept(");
        ShouldComeBefore(donate, "DB.Set(project)", "TakeItems(player, resref, accepted)");
        donate.Should().NotContain("CityPowers", "anyone, visitors included, can donate");
    }

    [Test]
    public void Cancelling_ReturnsMaterialsAndEachCreditPortion_ThenDeletesTheProject()
    {
        var projects = ReadServerSource("Service", "CityProjects.cs");

        var cancel = ExtractMethod(projects, "public static bool Cancel(string projectId, string playerId)");
        ShouldComeBefore(cancel, "CityProjectState.Cancelling", "FinishCancel(project, city)");

        var finish = ExtractMethod(projects, "private static void FinishCancel(CityProject project, WorldProperty city)");
        ShouldComeBefore(finish, "ReturnMaterials(project)", "CityTreasury.RefundEscrow(city, project.Id)");
        ShouldComeBefore(finish, "CityTreasury.RefundEscrow(city, project.Id)", "DB.Delete<CityProject>(project.Id)");

        var materials = ExtractMethod(projects, "private static void ReturnMaterials(CityProject project)");
        materials.Should().Contain("CityProjectRules.GetOverflowRecordId(").And.Contain("new OverflowItem");
    }

    [Test]
    public void Completing_PaysTheEscrowAndAppliesTheLevelInOneSaveOfTheCity()
    {
        var finish = ExtractMethod(ReadServerSource("Service", "CityProjects.cs"), "private static void FinishComplete(CityProject project, WorldProperty city)");

        ShouldComeBefore(finish, "CityTreasuryRules.PayOutEscrow(city, project.Id)", "CityProjectRules.ApplyCompletion(city, project)");
        ShouldComeBefore(finish, "CityProjectRules.ApplyCompletion(city, project)", "DB.Set(city)");
        ShouldComeBefore(finish, "DB.Set(city)", "DB.Delete<CityProject>(project.Id)");
        finish.Split("DB.Set(city)").Length.Should().Be(2);
    }

    [Test]
    public void TheProjectIsTheJournal_ARestartRepeatsOnlyTheStepsThatDidNotFinish()
    {
        var process = ExtractMethod(ReadServerSource("Service", "CityProjects.cs"), "private static void Process(CityProject project, DateTime now)");

        process.Should().Contain("case CityProjectState.Starting:").And.Contain("city.Escrow.ContainsKey(project.Id)");
        process.Should().Contain("case CityProjectState.Cancelling:").And.Contain("FinishCancel(project, city)");
        process.Should().Contain("case CityProjectState.Completing:").And.Contain("FinishComplete(project, city)");
        process.Should().Contain("CityProjectRules.IsDue(project, now)");
        process.Should().Contain("city.IsQueuedForDeletion", "a city being removed cancels its projects when it is deleted");
    }

    [Test]
    public void ProjectsComplete_AtServerStart_BeforeTheCleanUp()
    {
        var load = ExtractMethod(ReadServerSource("Service", "Property.cs"), "public static void OnModuleLoad()");

        ShouldComeBefore(load, "ProcessCities()", "CityProjects.ProcessAtServerStart()");
        ShouldComeBefore(load, "CityProjects.ProcessAtServerStart()", "CleanUpData()");
    }

    [Test]
    public void ARemovedCity_CancelsItsProjects_AndReturnsTheMaterials()
    {
        var delete = ExtractMethod(ReadServerSource("Service", "Property.cs"), "public static void DeleteProperty(WorldProperty property)");

        delete.Should().Contain("CityProjects.OnCityDeleted(property)");

        var removed = ExtractMethod(ReadServerSource("Service", "CityProjects.cs"), "public static void OnCityDeleted(WorldProperty city)");
        removed.Should().Contain("ReturnMaterials(project)");
        removed.Should().NotContain("RefundEscrow", "the credits go with the treasury");
    }

    [Test]
    public void TheWindows_AreInTheReservedRange_AndReachableByVisitors()
    {
        var types = ReadServerSource("Service", "GuiService", "GuiWindowType.cs");
        types.Should().Contain("HouseSpecialization = 117,").And.Contain("CityProjects = 118,");

        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "CityGazetteViewModel.cs").Should().Contain("GuiWindowType.CityProjects");
        ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.Projects.cs").Should().Contain("GuiWindowType.CityProjects");
    }

    #endregion
}
