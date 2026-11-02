using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The windows the Acting Mayor slice adds or extends: the DM city window, the Succession tab of Manage City, and the story
/// request tab of City Affairs. Each builds with no layout warnings and is wired to its own partial.
/// </summary>
public class CitySuccessionGuiTests
{
    #region DM city window

    [Test]
    public void TheDMCityWindowBuildsWithoutLayoutWarnings()
    {
        using var validation = GuiLayoutValidator.BeginValidationOnlyBuild();

        var window = new DMCitiesDefinition().BuildWindow();

        window.Type.Should().Be(GuiWindowType.DMCities);
        window.LayoutFindings.Should().BeEmpty();
    }

    [Test]
    public void TheDMCityWindowUsesTheStandardLayoutWithOnePartial()
    {
        var definition = ReadServer("Feature", "GuiDefinition", "DMCitiesDefinition.cs");

        definition.Should().Contain("window.AddStandardLayout(");
        definition.Should().Contain(".DefinePartialView(DMCitiesViewModel.MainContentPartial, AddMainContent)");
        definition.Should().Contain("layout.SetContentPartialElement(DMCitiesViewModel.MainContentElement);");
    }

    [Test]
    public void TheCitiesCommandOpensTheWindowForDMsAndAdminsOnly()
    {
        var commands = new DMChatCommand().BuildChatCommands();

        commands.Should().ContainKey("cities");
        commands.Should().ContainKey("citywindow");

        var command = commands["cities"];
        command.Authorization.Should().Be(AuthorizationLevel.DM | AuthorizationLevel.Admin);
        command.AvailableToAllOnTestEnvironment.Should().BeFalse("the window summons councils and changes requests");
        command.RequiresTarget.Should().BeFalse();
        command.DoAction.Should().NotBeNull();
    }

    [Test]
    public void EveryDMCityActionAsksWhetherStaffAreActing()
    {
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "DMCitiesViewModel.cs");

        Between(viewModel, "public Action OnClickRefresh()", "public Action OnClickTake()").Should().Contain("IsStaff()");
        Between(viewModel, "private void ChangeRequest(", "public Action OnClickSummon()").Should().Contain("IsStaff()");
        Between(viewModel, "public Action OnClickSummon()", "").Should().Contain("IsStaff()");
    }

    #endregion

    #region Manage City Succession tab

    [Test]
    public void ManageCityHasASuccessionTabWithItsOwnPartial()
    {
        ManageCityViewModel.SuccessionTabPartial.Should().NotBeNullOrWhiteSpace();
        ManageCityViewModel.SuccessionTabPartial.Should().NotBe(ManageCityViewModel.CityTabPartial);
        ManageCityViewModel.SuccessionTabPartial.Should().NotBe(ManageCityViewModel.EdictsTabPartial);

        var definition = ReadServer("Feature", "GuiDefinition", "ManageCityDefinition.cs");
        definition.Should().Contain(".DefinePartialView(ManageCityViewModel.SuccessionTabPartial, AddSuccessionTab)");
        definition.Should().Contain(".AddOption(\"Succession\")");
    }

    [Test]
    public void EverySuccessionActionChecksTheRoleInTheService()
    {
        var service = ReadServer("Service", "CitySuccession.cs");

        Between(service, "public static string NameActingMayor(", "public static string EndActingMayor(").Should().Contain("CityPowerRules.GetRole(");
        Between(service, "public static string EndActingMayor(", "private static string ClearTerm(").Should().Contain("CityPowerRules.GetRole(");
        Between(service, "public static string BeginStepDown(", "public static string AcceptStepDown(").Should().Contain("CityPowerRules.GetRole(");
        Between(service, "public static string AcceptStepDown(", "public static string CancelStepDown(").Should().Contain("StepDownRules.CheckAcceptance(");
        Between(service, "public static string CancelStepDown(", "private static void Cancel(").Should().Contain("StepDownRules.CheckCancel(");
    }

    [Test]
    public void AcceptingOpensTheSnapElectionOfTheStepDownKind()
    {
        var service = ReadServer("Service", "CitySuccession.cs");

        Between(service, "public static string AcceptStepDown(", "public static string CancelStepDown(")
            .Should().Contain("Property.OpenElection(now, city, ElectionKind.SnapStepDown, StepDownRules.GetElectionId(city.Id, stepDown));");
    }

    #endregion

    #region Manage City gates

    [Test]
    public void ManageCityReadsEveryPowerThroughTheCityPowers()
    {
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");
        var refresh = Between(viewModel, "private bool RefreshPermissions()", "private bool HasManageUpgradesPermission()");

        refresh.Should().Contain("CityPowers.Resolve(");
        refresh.Should().Contain("CanAccessTreasury = access.Can(CityPower.WithdrawTreasury);");
        refresh.Should().Contain("CanEditTaxes = access.Can(CityPower.ChangeTaxes);");
        refresh.Should().Contain("CanManageUpkeep = access.Can(CityPower.PayUpkeep);");
        refresh.Should().Contain("CanManageEdicts = access.Can(CityPower.EnactEdicts);");
        refresh.Should().Contain("CanRepealEdicts = access.Can(CityPower.RepealEdicts);");
        refresh.Should().Contain("access.Can(CityPower.StartProjects)");
        refresh.Should().NotContain("permission.Permissions[", "the permission record is read only through the city powers");
    }

    [Test]
    public void PayingUpkeepChecksThePermissionBeforeAndAfterTheConfirmation()
    {
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");
        var pay = Between(viewModel, "public Action PayUpkeep()", "public static void ResolveTaxes(");

        pay.Split("!CanManageUpkeep").Length.Should().BeGreaterThanOrEqualTo(3, "both the click and the confirmed action check it");
    }

    [Test]
    public void TheUpgradesAreGatedByTheProjectsPower()
    {
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");

        Between(viewModel, "private bool HasManageUpgradesPermission()", "/// <summary>A station's Jump Beacon")
            .Should().Contain("CityPower.StartProjects");
    }

    [Test]
    public void AStandInWithoutARecordCannotRenameTheCity()
    {
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "ManageCityViewModel.cs");
        var save = Between(viewModel, "public Action SaveChanges()", "private void PostTaxChangesToGazette(");

        save.Should().Contain("if (_hasPermissionRecord)");
        save.Should().Contain("dbCity.CustomName = CityName;");
    }

    [Test]
    public void TheCityTerminalAsksTheCityPowers()
    {
        var property = ReadServer("Service", "Property.cs");

        Between(property, "public static void OpenCityManagementMenu()", "public static string GetCityLevelName(int level)")
            .Should().Contain("CityPowers.CanOpenManageCity(");
    }

    #endregion

    #region City Affairs

    [Test]
    public void CityAffairsHasAStoryRequestTabWithItsOwnPartial()
    {
        CityAffairsViewModel.StoryTabPartial.Should().NotBeNullOrWhiteSpace();
        CityAffairsViewModel.StoryTabPartial.Should().NotBe(CityAffairsViewModel.SituationsTabPartial);
        CityAffairsViewModel.StoryTabPartial.Should().NotBe(CityAffairsViewModel.OfficersTabPartial);

        var definition = ReadServer("Feature", "GuiDefinition", "CityAffairsDefinition.cs");
        definition.Should().Contain(".DefinePartialView(CityAffairsViewModel.StoryTabPartial, AddStoryTab)");
        definition.Should().Contain(".SetMaxLength(StoryRequestRules.MaxLength)");
    }

    [Test]
    public void OnlyTheSittingMayorSeesTheAppointControls()
    {
        var viewModel = ReadServer("Feature", "GuiDefinition", "ViewModel", "CityAffairsViewModel.cs");

        viewModel.Should().Contain("_isMayor = city != null && CityPowers.IsMayor(city, GetObjectUUID(Player));");
    }

    [Test]
    public void EveryOfficerChangeRequiresTheSittingMayor()
    {
        var officers = ReadServer("Service", "CityOfficers.cs");

        officers.Split("CityPowers.IsMayor(").Length.Should().BeGreaterThanOrEqualTo(4, "appointing, changing a title, and removing all ask");
        Between(officers, "public static bool CanHandle(", "public static string GetTitle(")
            .Should().Contain("CityRole.DepartedMayor").And.Contain("CityRole.ActingMayor");
    }

    [Test]
    public void PostingAStoryRequestGoesThroughTheDMFeedAndNotTheGazette()
    {
        var service = ReadServer("Service", "CityStoryRequests.cs");

        service.Should().Contain("CityGazette.PostStoryRequest(");
        service.Should().NotContain("CityGazette.Post(");
    }

    #endregion

    #region Elections and the new mayor

    [Test]
    public void TheStepDownElectionCountsTheInterimHolderAsTheIncumbent()
    {
        var property = ReadServer("Service", "Property.cs");

        Between(property, "private static string GetElectionIncumbentId(", "internal static Election OpenElection(")
            .Should().Contain("CitySuccession.GetIncumbentId(city, election.Kind)");
    }

    [Test]
    public void TheMayorWhoSteppedDownIsBlockedAtRegistration()
    {
        var ballot = ReadServer("Service", "ElectionBallot.cs");

        Between(ballot, "public static string RegisterCandidate(", "public static string UpdateStatement(")
            .Should().Contain("CheckCanRun(election, playerId)");
        Between(ballot, "private static string CheckCanRun(", "private static string CheckRegistrationChange(")
            .Should().Contain("StepDownRules.CheckCandidate(");
    }

    [Test]
    public void ASeatChangeRunsTheNewMayorPathAndEndsTheActingMayor()
    {
        var property = ReadServer("Service", "Property.cs");
        var conclude = Between(property, "private static void ConcludeElection(", "private static void BroadcastElectionResult(");

        var seat = conclude.IndexOf("transferPermissions(outcome.WinnerPlayerId);", System.StringComparison.Ordinal);
        var succession = conclude.IndexOf("CitySuccession.ApplyElectionEnd(", System.StringComparison.Ordinal);
        var save = conclude.IndexOf("DB.Set(city);", System.StringComparison.Ordinal);
        var announce = conclude.IndexOf("CitySuccession.AnnounceElectionEnd(", System.StringComparison.Ordinal);

        seat.Should().BeGreaterThan(0);
        succession.Should().BeGreaterThan(seat);
        save.Should().BeGreaterThan(succession);
        announce.Should().BeGreaterThan(save, "the Gazette is posted after the city is saved");
    }

    [Test]
    public void ServerStartEndsATermThatRanOutAndCancelsAStepDownNobodyAccepted()
    {
        var property = ReadServer("Service", "Property.cs");
        var processCities = Between(property, "private static void ProcessCities()", "private static void ProcessCityCitizenRequirement(");

        processCities.Should().Contain("CitySuccession.Process(now, city);");
        var afterElections = processCities.IndexOf("ProcessCityElections(now, city);", System.StringComparison.Ordinal);
        processCities.IndexOf("CitySuccession.Process(now, city);", System.StringComparison.Ordinal).Should().BeGreaterThan(afterElections);
    }

    [Test]
    public void ACitizenWhoLeavesEndsTheirTermThroughTheSharedLeaveHook()
    {
        var officers = ReadServer("Service", "CityOfficers.cs");

        Between(officers, "public static void OnCitizenLeft(", "public static bool RemoveDepartedOfficers(")
            .Should().Contain("CitySuccession.OnCitizenLeft(city, playerId);");
    }

    #endregion

    #region Permission window

    [Test]
    public void AMayorWhoSteppedDownCannotGrantCityPermissions()
    {
        var window = ReadServer("Feature", "GuiDefinition", "ViewModel", "PropertyPermissionsViewModel.cs");

        window.Should().Contain("CityPowers.IsDepartedMayor(");
        Between(window, "private bool CanAdjustPermission(", "private bool CanAdjustGrantPermission(").Should().Contain("IsDepartedMayorOfThisCity()");
        Between(window, "private bool CanAdjustGrantPermission(", "private void LoadPlayerInfo()").Should().Contain("IsDepartedMayorOfThisCity()");
        Between(window, "public Action OnClickSaveChanges()", "public Action OnClickReset()").Should().Contain("IsDepartedMayorOfThisCity()");
    }

    #endregion

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray())).Replace("\r\n", "\n");

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, System.StringComparison.Ordinal);
        from.Should().BeGreaterThanOrEqualTo(0, start);
        if (end.Length == 0)
            return source.Substring(from);

        var to = source.IndexOf(end, from, System.StringComparison.Ordinal);
        to.Should().BeGreaterThan(from, end);
        return source.Substring(from, to - from);
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
