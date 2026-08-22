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
    public void PrivateAdjustableInstanceProperties_LoadOnDemand()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var onDemandBody = ExtractMethod(source, "private static bool IsPropertyOnDemand(WorldProperty property)");
        var startupBody = ExtractMethod(source, "private static bool IsPropertyStartupLoaded(WorldProperty property)");

        onDemandBody.Should().Contain("detail.PublicSetting == PropertyPublicType.Adjustable");
        onDemandBody.Should().Contain("!property.IsPubliclyAccessible");
        startupBody.Should().Contain("!IsPropertyOnDemand(property)");
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
    public void PropertyEntry_GatesThroughLoadStateBeforeJumping()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var enterPropertyBody = ExtractMethod(source, "public static void EnterProperty(uint player, string propertyId)");
        var enterBuildingBody = ExtractMethod(source, "public static void EnterBuilding()");
        var resolveBody = ExtractMethod(source, "public static bool TryResolveEnterableInstance(uint player, string propertyId, out PropertyInstance instance)");
        var loadingMessageBody = ExtractMethod(source, "private static void SendPropertyLoadingMessage(uint player)");

        enterPropertyBody.Should().Contain("TryResolveEnterableInstance(player, property.Id, out var instance)");
        enterBuildingBody.Should().Contain("TryResolveEnterableInstance(player, interior.Id, out var instance)");
        enterBuildingBody.Should().Contain("var interiorIds = building.ChildPropertyIds[PropertyChildType.Interior];");
        enterBuildingBody.Should().Contain("if (interiorIds.Count != 1)");
        enterBuildingBody.IndexOf("if (interiorIds.Count != 1)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(enterBuildingBody.IndexOf("var interiorId = interiorIds.Single();", StringComparison.Ordinal));
        resolveBody.Should().Contain("var detail = _propertyTypes[property.PropertyType];");
        resolveBody.Should().Contain("if (detail.SpawnType != PropertySpawnType.Instance)");
        resolveBody.IndexOf("if (detail.SpawnType != PropertySpawnType.Instance)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(resolveBody.IndexOf("AddPropertyLoadWaiter(player, propertyId);", StringComparison.Ordinal));
        resolveBody.Should().Contain("QueuePropertyLoad(propertyId, PropertyLoadPriority.PlayerRequest)");
        resolveBody.Should().Contain("SendPropertyLoadingMessage(player)");
        loadingMessageBody.Should().Contain("SendMessageToPC(player, \"This property is still loading. Please try again shortly.\");");
        loadingMessageBody.Should().NotContain("FloatingTextStringOnCreature");
        source.Should().Contain("This property is still loading. Please try again shortly.");
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
    public void LoadedState_WithMissingInstance_IsTreatedAsUnloaded()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var stateBody = ExtractMethod(source, "public static PropertyLoadState GetPropertyLoadState(string propertyId)");

        stateBody.Should().Contain("if (state != PropertyLoadState.Loaded)");
        stateBody.IndexOf("if (state != PropertyLoadState.Loaded)", StringComparison.Ordinal)
            .Should()
            .BeLessThan(stateBody.IndexOf("var dbProperty = DB.Get<WorldProperty>(propertyId);", StringComparison.Ordinal));
        stateBody.Should().Contain("if (dbProperty == null)");
        stateBody.Should().Contain("SetPropertyLoadState(propertyId, PropertyLoadState.Failed);");
        stateBody.Should().Contain("_propertyLoadFailures[propertyId] = \"Property does not exist in the database.\";");
        stateBody.Should().Contain("return PropertyLoadState.Failed;");
        stateBody.Should().Contain("GetIsObjectValid(registeredInstance.Area)");
        stateBody.Should().Contain("SetPropertyLoadState(propertyId, PropertyLoadState.Unloaded);");
        stateBody.IndexOf("SetPropertyLoadState(propertyId, PropertyLoadState.Unloaded);", StringComparison.Ordinal)
            .Should()
            .BeLessThan(stateBody.IndexOf("return PropertyLoadState.Unloaded;", StringComparison.Ordinal));
        stateBody.Should().Contain("return PropertyLoadState.Unloaded;");
        stateBody.Should().Contain("_propertyInstances.TryGetValue(propertyId, out var existingInstance)");
        stateBody.Should().Contain("GetIsObjectValid(existingInstance.Area)");
        stateBody.Should().Contain("_completedInstanceSpawnActions.Remove(propertyId);");
    }

    [Test]
    public void StaffNotify_DoesNotClearWaitersWhileLoadIsInProgress()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var staffNotifyBody = ExtractMethod(source, "public static void NotifyPropertyLoadWaitersForStaff(string propertyId)");
        var nonTerminalBody = ExtractMethod(source, "private static void NotifyPropertyLoadWaitersWithoutClearing(string propertyId, string message)");

        staffNotifyBody.Should().Contain("state == PropertyLoadState.Loaded");
        staffNotifyBody.Should().Contain("state == PropertyLoadState.Failed");
        staffNotifyBody.Should().Contain("NotifyPropertyLoadWaitersWithoutClearing(");
        nonTerminalBody.Should().NotContain("_propertyLoadWaiters.Remove(propertyId)");
    }

    [Test]
    public void LoadFailures_RecordOperationalContextForStaffDiagnostics()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var failBody = ExtractMethod(source, "private static void FailPropertyLoad(PropertyLoadJob job, string failure)");

        failBody.Should().Contain("Type: {property?.PropertyType.ToString() ?? \"Unknown\"}");
        failBody.Should().Contain("Layout: {property?.Layout.ToString() ?? \"Unknown\"}");
        failBody.Should().Contain("Phase: {job.Phase}");
        failBody.Should().Contain("StructureId: {structureId}");
        failBody.Should().Contain("Error: {failure}");
    }

    [Test]
    public void PropertyDiagnostics_ExposeOperationalFieldsForRuntimeRepair()
    {
        var root = FindRepositoryRoot();
        var propertySource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
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
        var diagnosticsBody = ExtractMethod(propertySource, "public static List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics()");

        diagnosticSource.Should().Contain("public string OwnerPlayerId { get; set; }");
        diagnosticSource.Should().Contain("public string QueuePriority { get; set; }");
        diagnosticSource.Should().Contain("public int SpawnedChildCount { get; set; }");
        diagnosticSource.Should().Contain("public int ExpectedChildCount { get; set; }");
        diagnosticSource.Should().Contain("public bool IsLoadedAreaValid { get; set; }");
        diagnosticSource.Should().Contain("public string LastPhase { get; set; }");
        diagnosticsBody.Should().Contain("OwnerPlayerId = property.OwnerPlayerId");
        diagnosticsBody.Should().Contain("QueuePriority = job?.Priority.ToString() ?? string.Empty");
        diagnosticsBody.Should().Contain("SpawnedChildCount = spawnedChildCount");
        diagnosticsBody.Should().Contain("ExpectedChildCount = expectedChildCount");
        diagnosticsBody.Should().Contain("IsLoadedAreaValid = isLoadedAreaValid");
        diagnosticsBody.Should().Contain("LastPhase = job?.Phase.ToString() ?? state.ToString()");
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
    public void CompletedInteriorLoad_RequeuesExteriorStructure()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var completeBody = ExtractMethod(source, "private static void CompletePropertyLoad(PropertyLoadJob job)");
        var queueBody = ExtractMethod(source, "private static bool QueueExteriorStructuresForInterior(string interiorPropertyId)");

        completeBody.Should().Contain("QueueExteriorStructuresForInterior(job.PropertyId);");
        queueBody.Should().Contain("interior.ParentPropertyId");
        queueBody.Should().Contain("QueueStartupWorldProperty(structure);");
        queueBody.Should().Contain("return true;");
    }

    [Test]
    public void StartupWorldStructureDependencies_QueueUnloadedInstances()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var resolveBody = ExtractMethod(source, "private static bool TryResolveWorldStructureArea(WorldProperty property, out uint area, out bool dependencyFailed)");

        resolveBody.Should().Contain("if (state == PropertyLoadState.Unloaded)");
        resolveBody.Should().Contain("QueuePropertyLoad(parent.Id, PropertyLoadPriority.Startup);");
        resolveBody.Should().Contain("if (interiorState == PropertyLoadState.Unloaded && !IsPropertyOnDemand(interior))");
        resolveBody.Should().Contain("QueuePropertyLoad(interiorId, PropertyLoadPriority.Startup);");
        resolveBody.Should().Contain("return IsPropertyOnDemand(interior) ||");
    }

    [Test]
    public void LoadProperties_ResetsScheduledProcessorFlagBeforeQueueingStartupLoads()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var loadBody = ExtractMethod(source, "private static void LoadProperties()");

        loadBody.Should().Contain("_propertyLoadProcessorScheduled = false;");
        loadBody.IndexOf("_propertyLoadProcessorScheduled = false;", StringComparison.Ordinal)
            .Should()
            .BeLessThan(loadBody.IndexOf("QueuePropertyLoad(property.Id, PropertyLoadPriority.Startup)", StringComparison.Ordinal));
    }

    [Test]
    public void RetryLoadedProperty_RequeuesExteriorStructureForRuntimeRepair()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var retryBody = ExtractMethod(source, "public static bool RetryPropertyLoad(string propertyId)");

        retryBody.Should().Contain("if (state == PropertyLoadState.Loaded)");
        retryBody.Should().Contain("return QueueExteriorStructuresForInterior(propertyId);");
    }

    [Test]
    public void QueueStartupWorldProperty_AlwaysEnsuresProcessorWhenAlreadyPending()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var queueBody = ExtractMethod(source, "private static void QueueStartupWorldProperty(WorldProperty property)");

        queueBody.Should().Contain("if (!_pendingStartupWorldPropertyIds.Contains(property.Id))");
        queueBody.Should().Contain("_pendingStartupWorldPropertyIds.Add(property.Id);");
        queueBody.Should().Contain("EnsurePropertyLoadProcessor();");
    }

    [Test]
    public void StartupWorldStructureBatch_HandlesSpawnFailuresWithoutStoppingQueue()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var processBody = ExtractMethod(source, "private static int ProcessStartupWorldProperties(int budget)");

        processBody.Should().Contain("try");
        processBody.Should().Contain("TryResolveWorldStructureArea(property, out area, out dependencyFailed);");
        processBody.Should().Contain("failed while resolving dependencies");
        processBody.Should().Contain("SpawnIntoWorld(property, area);");
        processBody.Should().Contain("catch (Exception ex)");
        processBody.Should().Contain("failed to spawn");
        processBody.Should().Contain("_pendingStartupWorldPropertyIds.RemoveAt(0);");
        processBody.Should().Contain("consumed++;");
    }

    [Test]
    public void ExistingWorldStructure_ReplaysStructureChangedAction()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var spawnBody = ExtractMethod(source, "private static void SpawnIntoWorld(WorldProperty property, uint area)");

        spawnBody.Should().Contain("_structurePropertyIdToPlaceable.TryGetValue(property.Id, out var existingPlaceable)");
        spawnBody.Should().Contain("RunStructureChangedEvent(property.StructureType, StructureChangeType.PositionChanged, property, existingPlaceable);");
    }

    [Test]
    public void InstanceSpawnAction_IsTrackedSeparatelyFromAreaRegistration()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var processBody = ExtractMethod(source, "private static int ProcessPropertyLoadJob(PropertyLoadJob job, int budget)");
        var spawnBody = ExtractMethod(source, "private static void SpawnIntoWorld(WorldProperty property, uint area)");
        var deleteBody = ExtractMethod(source, "public static void DeleteProperty(WorldProperty property)");
        var loadBody = ExtractMethod(source, "private static void LoadProperties()");

        source.Should().Contain("private static readonly HashSet<string> _completedInstanceSpawnActions = new();");
        processBody.Should().Contain("if (!_completedInstanceSpawnActions.Contains(job.PropertyId))");
        processBody.Should().Contain("if (!GetIsObjectValid(targetArea))");
        processBody.Should().Contain("Unable to create property area from resref");
        processBody.Should().Contain("layout.OnSpawnAction?.Invoke(targetArea);");
        processBody.Should().Contain("_completedInstanceSpawnActions.Add(job.PropertyId);");
        spawnBody.Should().Contain("if (!GetIsObjectValid(targetArea))");
        spawnBody.Should().Contain("Unable to create property area from resref");
        spawnBody.Should().Contain("if (!_completedInstanceSpawnActions.Contains(property.Id))");
        spawnBody.Should().Contain("layout.OnSpawnAction?.Invoke(existingInstance.Area);");
        deleteBody.Should().Contain("_completedInstanceSpawnActions.Remove(property.Id);");
        loadBody.Should().Contain("_completedInstanceSpawnActions.Clear();");
    }

    [Test]
    public void PropertyLoadBatch_StaggersAreaCreationAcrossScheduledPasses()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var processBody = ExtractMethod(source, "private static int ProcessPropertyLoadJob(PropertyLoadJob job, int budget)");

        source.Should().Contain("private const int PropertyLoadBatchSize = 5;");
        processBody.Should().Contain("job.Phase = PropertyLoadJobPhase.SpawnStructures;");
        processBody.Should().Contain("return Math.Max(budget, 1);");
        processBody.IndexOf("job.Phase = PropertyLoadJobPhase.SpawnStructures;", StringComparison.Ordinal)
            .Should()
            .BeLessThan(processBody.IndexOf("return Math.Max(budget, 1);", StringComparison.Ordinal));
    }

    [Test]
    public void StartupPropertyLoadProgress_ReplacesPerPropertyConsoleSpam()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "Property.cs")).Replace("\r\n", "\n");
        var queueBody = ExtractMethod(source, "private static bool QueuePropertyLoad(string propertyId, PropertyLoadPriority priority)");
        var completeBody = ExtractMethod(source, "private static void CompletePropertyLoad(PropertyLoadJob job)");
        var failBody = ExtractMethod(source, "private static void FailPropertyLoad(PropertyLoadJob job, string failure)");
        var abortBody = ExtractMethod(source, "public static bool AbortQueuedPropertyLoad(string propertyId)");
        var progressBody = ExtractMethod(source, "private static void LogStartupPropertyLoadProgress(bool force = false)");
        var loadBody = ExtractMethod(source, "private static void LoadProperties()");

        source.Should().Contain("private const int PropertyLoadProgressReportInterval = 25;");
        queueBody.Should().Contain("TrackStartupPropertyLoadQueued(propertyId, priority);");
        completeBody.Should().Contain("TrackStartupPropertyLoadCompleted(job.PropertyId);");
        completeBody.Should().Contain("LogStartupPropertyLoadProgress();");
        completeBody.Should().Contain("Log.Write(LogGroup.Property, $\"Property '{job.Property.CustomName}' ({job.PropertyId}) loaded on {job.Priority} queue.\");");
        completeBody.Should().NotContain("loaded on {job.Priority} queue.\", true)");
        completeBody.Should().NotContain("printToConsole");
        failBody.Should().Contain("TrackStartupPropertyLoadFailed(job.PropertyId);");
        abortBody.Should().Contain("TrackStartupPropertyLoadFailed(propertyId);");
        progressBody.Should().Contain("remaining");
        progressBody.Should().Contain("PropertyLoadProgressReportInterval");
        progressBody.Should().Contain("Log.Write(LogGroup.Property");
        progressBody.Should().Contain("true");
        loadBody.Should().Contain("ResetStartupPropertyLoadProgress();");
        loadBody.Should().Contain("LogStartupPropertyLoadProgress(true);");
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
        commandBody.Should().Contain("var player = user;");
        commandBody.Should().Contain("if (GetIsDMPossessed(player))");
        commandBody.Should().Contain("uiTarget = player;");
        commandBody.Should().Contain("player = GetMaster(player);");
        commandBody.Should().Contain("Log.WriteStructured(");
        commandBody.Should().Contain("LogGroup.Property");
        commandBody.Should().Contain("Property diagnostics toggled:");
        commandBody.Should().Contain("GetName(player)");
        commandBody.Should().Contain("GetObjectUUID(player)");
        commandBody.IndexOf("Log.WriteStructured(", StringComparison.Ordinal)
            .Should().BeLessThan(commandBody.IndexOf("Gui.TogglePlayerWindow(", StringComparison.Ordinal));
        commandBody.Should().Contain("GuiWindowType.PropertyDiagnostics,");
        commandBody.Should().Contain("uiTarget);");
        staffDefinitionSource.Should().NotContain("OnClickPropertyDiagnostics");
        staffViewModelSource.Should().NotContain("GuiWindowType.PropertyDiagnostics");
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
