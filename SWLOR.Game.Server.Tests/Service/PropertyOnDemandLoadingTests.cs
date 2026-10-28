using FluentAssertions;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace SWLOR.Game.Server.Tests.Service;

public class PropertyOnDemandLoadingTests
{
    [Test]
    public void PropertyTypes_DeclareOnDemandOnlyForPrivatePlayerInstances()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PropertyService",
            "PropertyType.cs"));

        source.Should().Contain("[PropertyType(\"Apartment\", true, PropertyPublicType.AlwaysPrivate, PropertySpawnType.Instance, PropertyLoadType.OnDemand)]");
        source.Should().Contain("[PropertyType(\"Starship\", true, PropertyPublicType.AlwaysPrivate, PropertySpawnType.Instance, PropertyLoadType.OnDemand)]");
        source.Should().Contain("[PropertyType(\"City Hall\", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]");
        source.Should().Contain("[PropertyType(\"Bank\", true, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]");
        source.Should().Contain("[PropertyType(\"Starport\", false, PropertyPublicType.AlwaysPublic, PropertySpawnType.Instance, PropertyLoadType.Startup)]");
        source.Should().Contain("[PropertyType(\"House\", true, PropertyPublicType.Adjustable, PropertySpawnType.Instance, PropertyLoadType.Startup)]");
        Regex.Matches(source, @"PropertyLoadType\.OnDemand")
            .Should()
            .HaveCount(2);
    }

    [Test]
    public void PersistentLocation_DoesNotJumpToInvalidInstanceArea()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PersistentLocation.cs")).Replace("\r\n", "\n");
        var loadBody = ExtractMethod(source, "public static void LoadLocation(uint player)");

        loadBody.Should().Contain("var locationArea = Area.GetAreaByResref(dbPlayer.LocationAreaResref);");
        loadBody.Should().Contain("if (!GetIsObjectValid(locationArea))");
        loadBody.Should().Contain("Log.WriteStructured(");
        loadBody.Should().Contain("Persistent location area resolution failed");
        loadBody.Should().Contain("return;");
    }

    [Test]
    public void RegisteredInstances_AreNotReadDirectlyOutsidePropertyService()
    {
        var root = FindRepositoryRoot();
        var serverRoot = Path.Combine(root.FullName, "SWLOR.Game.Server");
        var offenders = Directory
            .GetFiles(serverRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bGetRegisteredInstance\s*\("))
            .ToList();

        offenders.Should().BeEmpty();
    }

    [Test]
    public void PropertyDiagnostics_ExposeOperationalFieldsForRuntimeRepair()
    {
        var root = FindRepositoryRoot();
        var diagnosticSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PropertyService",
            "PropertyLoadDiagnostic.cs"));
        var viewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PropertyDiagnosticsViewModel.cs"));

        // PropertyLoaderTests proves the loader fills these fields; this proves the window shows them.
        diagnosticSource.Should().Contain("public string OwnerPlayerId { get; set; }");
        diagnosticSource.Should().Contain("public string QueuePriority { get; set; }");
        diagnosticSource.Should().Contain("public int SpawnedChildCount { get; set; }");
        diagnosticSource.Should().Contain("public int ExpectedChildCount { get; set; }");
        diagnosticSource.Should().Contain("public bool IsLoadedAreaValid { get; set; }");
        diagnosticSource.Should().Contain("public string LastPhase { get; set; }");
        viewModelSource.Should().Contain("Owner: {diagnostic.OwnerPlayerId}");
        viewModelSource.Should().Contain("Priority: {diagnostic.QueuePriority}");
        viewModelSource.Should().Contain("Children: {diagnostic.SpawnedChildCount} / {diagnostic.ExpectedChildCount}");
        viewModelSource.Should().Contain("Area Valid: {diagnostic.IsLoadedAreaValid}");
        viewModelSource.Should().Contain("Phase: {diagnostic.LastPhase}");
    }

    [Test]
    public void PropertyDiagnostics_ActionsRequireAdminAuthorization()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PropertyDiagnosticsViewModel.cs")).Replace("\r\n", "\n");

        source.Should().Contain("private bool IsAdminAuthorized(string action)");
        source.Should().Contain("Authorization.GetAuthorizationLevel(Player) == AuthorizationLevel.Admin");
        source.Should().Contain("Property diagnostics authorization denied");
        source.Should().Contain("Property diagnostics admin action");

        AssertActionStartsWithAdminCheck(source, "public Action OnRefresh() => () =>", "LoadDiagnostics(\"Refreshed property diagnostics.\");");
        AssertActionStartsWithAdminCheck(source, "public Action OnRetryLoad() => () =>", "Property.RetryPropertyLoad(diagnostic.PropertyId);");
        AssertActionStartsWithAdminCheck(source, "public Action OnAbortQueue() => () =>", "Property.AbortQueuedPropertyLoad(diagnostic.PropertyId);");
        AssertActionStartsWithAdminCheck(source, "public Action OnNotifyWaiters() => () =>", "Property.NotifyPropertyLoadWaitersForStaff(diagnostic.PropertyId);");
    }

    [Test]
    public void PropertyDiagnostics_SelectPropertyBoundsChecksNuiIndex()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "PropertyDiagnosticsViewModel.cs")).Replace("\r\n", "\n");
        var selectBody = ExtractMethod(source, "public Action OnSelectProperty() => () =>");

        selectBody.Should().Contain("var index = NuiGetEventArrayIndex();");
        selectBody.Should().Contain("if (index < 0 || index >= PropertySelections.Count)");
        selectBody.IndexOf("if (index < 0 || index >= PropertySelections.Count)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(selectBody.IndexOf("PropertySelections[_selectedPropertyIndex] = false;", StringComparison.Ordinal));
        selectBody.IndexOf("if (index < 0 || index >= PropertySelections.Count)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(selectBody.IndexOf("_selectedPropertyIndex = index;", StringComparison.Ordinal));
    }

    [Test]
    public void StartupPropertyLoadProgress_ReplacesPerPropertyConsoleSpam()
    {
        // The static Log has no seam a unit test can read, so the wording and the console flag are checked in the source
        // of the loader, where the code lives; PropertyLoaderTests covers what the loader does.
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Domain",
            "World",
            "PropertyLoader.cs")).Replace("\r\n", "\n");
        var completeBody = ExtractMethod(source, "private void CompletePropertyLoad(PropertyLoadJob job)");
        var progressBody = ExtractMethod(source, "private void LogStartupPropertyLoadProgress(bool force = false)");

        source.Should().Contain("private const int PropertyLoadProgressReportInterval = 25;");
        completeBody.Should().Contain("Log.Write(LogGroup.Property, $\"Property '{job.Property.CustomName}' ({job.PropertyId}) loaded on {job.Priority} queue.\");");
        completeBody.Should().NotContain("loaded on {job.Priority} queue.\", true)");
        completeBody.Should().NotContain("printToConsole");
        progressBody.Should().Contain("remaining");
        progressBody.Should().Contain("PropertyLoadProgressReportInterval");
        progressBody.Should().Contain("Log.Write(LogGroup.Property");
        progressBody.Should().Contain("true");
    }

    [Test]
    public void EmergencyExit_OnlyRunsAfterDestinationResolves()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition",
            "PropertyExitDialog.cs")).Replace("\r\n", "\n");
        var returnBody = ExtractMethod(source, "private bool ReturnToLastDockedPosition(uint player, PropertyLocation propertyLocation)");
        var pageBody = ExtractMethod(source, "private void MainPageInit(ConversationMenuPage page)");

        returnBody.Should().Contain("return false;");
        returnBody.Should().Contain("return true;");
        pageBody.Should().Contain("if (ReturnToLastDockedPosition(player, propertyLocation))");
        pageBody.Should().Contain("Space.PerformEmergencyExit(area);");
    }

    [Test]
    public void PlayerStarportDocking_RequiresLoadedStarport()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "DialogDefinition",
            "StarportDockDialog.cs")).Replace("\r\n", "\n");
        var pageBody = ExtractMethod(source, "private void MainPageInit(ConversationMenuPage page)");

        pageBody.Should().Contain("Property.GetPropertyLoadState(dockPoint.PropertyId)");
        pageBody.Should().Contain("starportLoadState != PropertyLoadState.Loaded");
        pageBody.Should().Contain("Player starport docking denied");
        pageBody.Should().Contain("load-state");
        pageBody.Should().Contain("Property.TryGetLoadedInstance(dockPoint.PropertyId, out var starportInstance)");
        pageBody.Should().Contain("instance-unavailable");
        pageBody.Should().Contain("!GetLocalBool(starportInstance.Area, \"BUILDING_EXIT_SET\")");
        pageBody.Should().Contain("building-exit-not-ready");
        pageBody.Should().Contain("This starport is still loading. Please try again shortly.");
    }

    [Test]
    public void ShipManagement_DoesNotLabelUnloadedDockInstanceAsInSpace()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ShipManagementViewModel.cs")).Replace("\r\n", "\n");
        var loadBody = ExtractMethod(source, "private void LoadShip()");
        var locationBody = ExtractMethod(source, "private uint GetShipLocation(WorldProperty property, out bool isDockInstanceLoading)");

        loadBody.Should().Contain("var currentLocation = GetShipLocation(property, out var isDockInstanceLoading);");
        loadBody.Should().Contain("var isInSpace = property.Positions.ContainsKey(PropertyLocationType.CurrentPosition);");
        loadBody.Should().Contain("ShipLocation = isInSpace");
        loadBody.Should().Contain("\"Docked (loading...)\"");
        loadBody.Should().NotContain("ShipLocation = currentLocation == OBJECT_INVALID ? \"In Space\" : GetName(currentLocation);");

        locationBody.Should().Contain("isDockInstanceLoading = false;");
        locationBody.Should().Contain("Property.TryGetLoadedInstance(landingLocation.InstancePropertyId, out var instance)");
        locationBody.Should().Contain("isDockInstanceLoading = true;");
    }

    [Test]
    public void PlayerStarportDockpoints_AreRemovedByPropertyId()
    {
        var root = FindRepositoryRoot();
        var spaceSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Space.cs")).Replace("\r\n", "\n");
        var structureSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "PropertyService",
            "StructureChangedAction.cs")).Replace("\r\n", "\n");
        var registerBody = ExtractMethod(spaceSource, "public static void RegisterLandingPoint(uint waypoint, uint area, bool isNPC, string propertyId)");
        var removeBody = ExtractMethod(spaceSource, "public static void RemoveLandingPointByPropertyId(string propertyId)");
        var retrieveBody = ExtractMethod(structureSource, "private static Action<WorldProperty, uint> RetrieveStarport()");

        registerBody.Should().Contain("RemoveLandingPointByPropertyId(propertyId);");
        registerBody.Should().Contain("if (_dockPoints[planet].ContainsKey(dockPointId))");
        registerBody.Should().Contain("DeleteLocalString(waypoint, \"STARSHIP_DOCKPOINT_ID\");");
        removeBody.Should().Contain("x.Value.PropertyId == propertyId");
        retrieveBody.Should().Contain("if (dbInterior == null)");
        retrieveBody.Should().Contain("Space.RemoveLandingPointByPropertyId(interiorId);");
        retrieveBody.IndexOf("Space.RemoveLandingPointByPropertyId(interiorId);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(retrieveBody.IndexOf("if (dbInterior == null)", StringComparison.Ordinal));
        retrieveBody.Should().Contain("if (!Property.TryGetLoadedInstance(interiorId, out var instance))");
    }

    [Test]
    public void PropertyDiagnostics_AreExposedThroughDedicatedAdminCommand()
    {
        var commands = new SWLOR.Game.Server.Feature.ChatCommandDefinition.AdminChatCommand().BuildChatCommands();
        commands.Should().ContainKey("propertydiagnostics");
        commands["propertydiagnostics"].Authorization
            .Should().Be(SWLOR.Game.Server.Enumeration.AuthorizationLevel.Admin);

        var root = FindRepositoryRoot();
        var adminChatSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "ChatCommandDefinition",
            "AdminChatCommand.cs"));
        var staffDefinitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ManageStaffDefinition.cs"));
        var staffViewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "ManageDMsViewModel.cs"));

        var commandBody = ExtractMethod(adminChatSource, "private void PropertyDiagnosticsCommand()");
        commandBody.Should().Contain("_builder.Create(\"propertydiagnostics\")");
        commandBody.Should().Contain(".Permissions(AuthorizationLevel.Admin)");
        // A possessing DM is logged as their avatar, and DMPresence shows the window on the possessed creature.
        commandBody.Should().Contain("var dm = DMPresence.GetDM(user);");
        commandBody.Should().Contain("Log.WriteStructured(");
        commandBody.Should().Contain("LogGroup.Property");
        commandBody.Should().Contain("Property diagnostics toggled:");
        commandBody.Should().Contain("GetName(dm)");
        commandBody.Should().Contain("GetObjectUUID(dm)");
        commandBody.IndexOf("Log.WriteStructured(", StringComparison.Ordinal)
            .Should().BeLessThan(commandBody.IndexOf("DMPresence.ToggleWindow(", StringComparison.Ordinal));
        // Opened like the other DM windows, so it follows the DM through possession.
        commandBody.Should().Contain("DMPresence.ToggleWindow(user, GuiWindowType.PropertyDiagnostics);");
        commandBody.Should().NotContain("Gui.TogglePlayerWindow(");
        staffDefinitionSource.Should().NotContain("OnClickPropertyDiagnostics");
        staffViewModelSource.Should().NotContain("GuiWindowType.PropertyDiagnostics");

        // The chat command is the only way in; the DM Admin Tools window does not list it.
        var adminToolsDefinitionSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "DMAdminToolsDefinition.cs"));
        var adminToolsViewModelSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "DMAdminToolsViewModel.cs"));
        adminToolsDefinitionSource.Should().NotContain("Property Diagnostics");
        adminToolsViewModelSource.Should().NotContain("GuiWindowType.PropertyDiagnostics");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should exist");

        var bodyStart = source.IndexOf('{', start);
        bodyStart.Should().BeGreaterThanOrEqualTo(0, $"signature '{signature}' should have a body");

        var depth = 0;
        for (var i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
                depth++;
            else if (source[i] == '}')
                depth--;

            if (depth == 0)
                return source.Substring(start, i - start + 1);
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static void AssertActionStartsWithAdminCheck(string source, string signature, string protectedOperation)
    {
        var body = ExtractMethod(source, signature);

        body.Should().Contain("if (!IsAdminAuthorized(");
        body.Should().Contain("return;");
        body.IndexOf("if (!IsAdminAuthorized(", StringComparison.Ordinal)
            .Should()
            .BeLessThan(body.IndexOf(protectedOperation, StringComparison.Ordinal));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
