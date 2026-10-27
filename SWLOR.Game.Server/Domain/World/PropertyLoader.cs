using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Instance loading: the registry of property area instances, their load states, failures and waiting players, the
    /// queue that loads them a few at a time, the startup load of the world properties, and spawning a property into
    /// the game world. The logic and state that used to be static in <c>Service.Property</c>, moved here unchanged.
    /// </summary>
    public class PropertyLoader : IPropertyLoader
    {
        private readonly IDBService _db;
        private readonly IAreaService _area;
        private readonly IPropertyCatalog _catalog;

        private readonly Dictionary<string, PropertyInstance> _propertyInstances = new();
        private readonly Dictionary<string, PropertyLoadState> _propertyLoadStates = new();
        private readonly Dictionary<string, string> _propertyLoadFailures = new();
        private readonly Dictionary<string, HashSet<string>> _propertyLoadWaiters = new();
        private readonly HashSet<string> _completedInstanceSpawnActions = new();
        private readonly Dictionary<string, uint> _structurePropertyIdToPlaceable = new();
        private readonly List<PropertyLoadJob> _propertyLoadJobs = new();
        private readonly List<string> _pendingStartupWorldPropertyIds = new();
        private readonly HashSet<string> _startupPropertyLoadIds = new();
        private readonly HashSet<string> _completedStartupPropertyLoadIds = new();
        private readonly HashSet<string> _failedStartupPropertyLoadIds = new();
        private bool _propertyLoadProcessorScheduled;
        private int _lastStartupPropertyLoadProgressLoaded = -1;
        private int _lastStartupPropertyLoadProgressRemaining = -1;
        private int _lastStartupPropertyLoadProgressFailed = -1;

        private const int PropertyLoadBatchSize = 5;
        private const int PropertyLoadProgressReportInterval = 25;
        private TimeSpan PropertyLoadBatchDelay => TimeSpan.FromSeconds(0.2d);

        public PropertyLoader(IDBService db, IAreaService area, IPropertyCatalog catalog)
        {
            _db = db;
            _area = area;
            _catalog = catalog;
        }

        private enum PropertyLoadPriority
        {
            Startup = 0,
            PlayerRequest = 1,
            StaffRequest = 2
        }

        private enum PropertyLoadJobPhase
        {
            CreateArea = 0,
            SpawnStructures = 1
        }

        private sealed class PropertyLoadJob
        {
            public string PropertyId { get; set; }
            public PropertyLoadPriority Priority { get; set; }
            public PropertyLoadJobPhase Phase { get; set; }
            public WorldProperty Property { get; set; }
            public uint Area { get; set; }
            public List<WorldProperty> Structures { get; set; } = new();
            public int StructureIndex { get; set; }
        }

        /// <summary>
        /// Assigns a property Id as a local variable to a specific object.
        /// </summary>
        /// <param name="obj">The object to assign</param>
        /// <param name="propertyId">The property Id to assign.</param>
        public void AssignPropertyId(uint obj, string propertyId)
        {
            SetLocalString(obj, "PROPERTY_ID", propertyId);
        }

        /// <summary>
        /// Retrieves the assigned property Id assigned to a specific object.
        /// Returns an empty string if not found.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>The property Id or an empty string if not found.</returns>
        public string GetPropertyId(uint obj)
        {
            return GetLocalString(obj, "PROPERTY_ID");
        }

        /// <summary>
        /// Registers an area instance to a given property Id.
        /// </summary>
        /// <param name="propertyId">The property Id</param>
        /// <param name="instance">The area instance to register</param>
        /// <param name="layoutType">The layout of the property.</param>
        public void RegisterInstance(string propertyId, uint instance, PropertyLayoutType layoutType)
        {
            AssignPropertyId(instance, propertyId);
            _propertyInstances[propertyId] = new PropertyInstance(instance, layoutType);
        }

        /// <summary>
        /// Retrieves the current load state for an instanced property.
        /// </summary>
        /// <param name="propertyId">The property Id to check.</param>
        /// <returns>The current property load state.</returns>
        public PropertyLoadState GetPropertyLoadState(string propertyId)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return PropertyLoadState.Failed;

            if (_propertyLoadStates.TryGetValue(propertyId, out var state))
            {
                if (state != PropertyLoadState.Loaded)
                    return state;

                if (_propertyInstances.TryGetValue(propertyId, out var registeredInstance) &&
                    GetIsObjectValid(registeredInstance.Area))
                    return state;

                var dbProperty = _db.Get<WorldProperty>(propertyId);
                if (dbProperty == null)
                {
                    _propertyInstances.Remove(propertyId);
                    _completedInstanceSpawnActions.Remove(propertyId);
                    SetPropertyLoadState(propertyId, PropertyLoadState.Failed);
                    _propertyLoadFailures[propertyId] = "Property does not exist in the database.";
                    return PropertyLoadState.Failed;
                }

                if (_catalog.GetPropertyDetail(dbProperty.PropertyType).SpawnType == PropertySpawnType.Instance)
                {
                    _propertyInstances.Remove(propertyId);
                    _completedInstanceSpawnActions.Remove(propertyId);
                    SetPropertyLoadState(propertyId, PropertyLoadState.Unloaded);
                    return PropertyLoadState.Unloaded;
                }

                return state;
            }

            if (_propertyInstances.TryGetValue(propertyId, out var existingInstance))
            {
                if (GetIsObjectValid(existingInstance.Area))
                    return PropertyLoadState.Loaded;

                _propertyInstances.Remove(propertyId);
                _completedInstanceSpawnActions.Remove(propertyId);
                return PropertyLoadState.Unloaded;
            }

            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
                return PropertyLoadState.Failed;

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            return detail.SpawnType == PropertySpawnType.Instance
                ? PropertyLoadState.Unloaded
                : PropertyLoadState.Loaded;
        }

        /// <summary>
        /// Retrieves the last failure associated with a property load.
        /// </summary>
        /// <param name="propertyId">The property Id to check.</param>
        /// <returns>The failure message or an empty string.</returns>
        public string GetPropertyLoadFailure(string propertyId)
        {
            return _propertyLoadFailures.TryGetValue(propertyId, out var failure)
                ? failure
                : string.Empty;
        }

        /// <summary>
        /// Retrieves runtime load diagnostics for all instanced properties.
        /// </summary>
        /// <returns>A list of property load diagnostics.</returns>
        public List<PropertyLoadDiagnostic> GetPropertyLoadDiagnostics()
        {
            var instanceTypes = _catalog.PropertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.Instance)
                .Select(x => (int)x.Key)
                .ToList();
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), instanceTypes);
            var count = (int)_db.SearchCount(query);
            var properties = _db.Search(query.AddPaging(count, 0));
            var diagnostics = new List<PropertyLoadDiagnostic>();

            foreach (var property in properties)
            {
                var detail = _catalog.GetPropertyDetail(property.PropertyType);
                var job = _propertyLoadJobs.FirstOrDefault(x => x.PropertyId == property.Id);
                var state = GetPropertyLoadState(property.Id);
                var expectedChildCount = job?.Structures.Count > 0
                    ? job.Structures.Count
                    : GetPropertyStructures(property.Id).Count;
                var spawnedChildCount = job != null
                    ? job.StructureIndex
                    : state == PropertyLoadState.Loaded
                        ? expectedChildCount
                        : 0;
                var isLoadedAreaValid = _propertyInstances.TryGetValue(property.Id, out var instance) &&
                                        GetIsObjectValid(instance.Area);
                diagnostics.Add(new PropertyLoadDiagnostic
                {
                    PropertyId = property.Id,
                    Name = property.CustomName,
                    OwnerPlayerId = property.OwnerPlayerId,
                    PropertyType = property.PropertyType,
                    LoadType = detail.LoadType,
                    State = state,
                    QueuePriority = job?.Priority.ToString() ?? string.Empty,
                    SpawnedChildCount = spawnedChildCount,
                    ExpectedChildCount = expectedChildCount,
                    IsLoadedAreaValid = isLoadedAreaValid,
                    LastPhase = job?.Phase.ToString() ?? state.ToString(),
                    Failure = GetPropertyLoadFailure(property.Id),
                    WaiterCount = _propertyLoadWaiters.TryGetValue(property.Id, out var waiters) ? waiters.Count : 0,
                    IsQueued = job != null
                });
            }

            return diagnostics
                .OrderBy(x => GetDiagnosticSortOrder(x.State))
                .ThenBy(x => x.PropertyType.ToString())
                .ThenBy(x => x.Name)
                .ToList();
        }

        private int GetDiagnosticSortOrder(PropertyLoadState state)
        {
            return state switch
            {
                PropertyLoadState.Failed => 0,
                PropertyLoadState.Loading => 1,
                PropertyLoadState.Queued => 2,
                PropertyLoadState.Unloaded => 3,
                PropertyLoadState.Loaded => 4,
                _ => 5
            };
        }

        /// <summary>
        /// Retrieves a loaded property instance without throwing if it is not ready.
        /// </summary>
        /// <param name="propertyId">The property Id to retrieve.</param>
        /// <param name="instance">The loaded property instance.</param>
        /// <returns>true if the instance is fully loaded; otherwise false.</returns>
        public bool TryGetLoadedInstance(string propertyId, out PropertyInstance instance)
        {
            instance = null;

            if (GetPropertyLoadState(propertyId) != PropertyLoadState.Loaded)
                return false;

            if (!_propertyInstances.TryGetValue(propertyId, out var registeredInstance))
                return false;

            if (!GetIsObjectValid(registeredInstance.Area))
                return false;

            instance = registeredInstance;
            return true;
        }

        public void SetPropertyLoadState(string propertyId, PropertyLoadState state)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return;

            _propertyLoadStates[propertyId] = state;
        }

        public bool IsPropertyOnDemand(WorldProperty property)
        {
            if (property == null)
                return false;

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            return detail.SpawnType == PropertySpawnType.Instance &&
                   (detail.LoadType == PropertyLoadType.OnDemand ||
                    detail.PublicSetting == PropertyPublicType.Adjustable &&
                    !property.IsPubliclyAccessible);
        }

        private bool IsPropertyStartupLoaded(WorldProperty property)
        {
            if (property == null)
                return false;

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            return detail.SpawnType == PropertySpawnType.Instance &&
                   detail.LoadType == PropertyLoadType.Startup &&
                   !IsPropertyOnDemand(property);
        }

        private void AddPropertyLoadWaiter(uint player, string propertyId)
        {
            if (!GetIsPC(player) || string.IsNullOrWhiteSpace(propertyId))
                return;

            if (!_propertyLoadWaiters.ContainsKey(propertyId))
                _propertyLoadWaiters[propertyId] = new HashSet<string>();

            _propertyLoadWaiters[propertyId].Add(GetObjectUUID(player));
        }

        private void NotifyPropertyLoadWaiters(string propertyId, bool loaded)
        {
            if (!_propertyLoadWaiters.TryGetValue(propertyId, out var waiterIds) ||
                waiterIds.Count <= 0)
            {
                return;
            }

            var property = _db.Get<WorldProperty>(propertyId);
            var propertyName = property?.CustomName ?? "The property";
            var message = loaded
                ? $"{propertyName} is ready. Please try entering again."
                : $"{propertyName} could not be loaded. Please notify staff.";

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!waiterIds.Contains(GetObjectUUID(player)))
                    continue;

                SendMessageToPC(player, message);
            }

            _propertyLoadWaiters.Remove(propertyId);
        }

        public void NotifyPropertyLoadWaitersForStaff(string propertyId)
        {
            var state = GetPropertyLoadState(propertyId);
            if (state == PropertyLoadState.Loaded ||
                state == PropertyLoadState.Failed)
            {
                NotifyPropertyLoadWaiters(propertyId, state == PropertyLoadState.Loaded);
                return;
            }

            NotifyPropertyLoadWaitersWithoutClearing(
                propertyId,
                "This property is still loading. Please try again shortly.");
        }

        private void NotifyPropertyLoadWaitersWithoutClearing(string propertyId, string message)
        {
            if (!_propertyLoadWaiters.TryGetValue(propertyId, out var waiterIds) ||
                waiterIds.Count <= 0)
            {
                return;
            }

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!waiterIds.Contains(GetObjectUUID(player)))
                    continue;

                SendMessageToPC(player, message);
            }
        }

        private void SendPropertyLoadingMessage(uint player)
        {
            SendMessageToPC(player, "This property is still loading. Please try again shortly.");
        }

        public void SendPropertyLoadFailedMessage(uint player)
        {
            SendMessageToPC(player, "This property could not be loaded. Please notify staff.");
        }

        /// <summary>
        /// Retrieves a usable property instance or starts loading it if it is not ready.
        /// </summary>
        /// <param name="player">The player attempting to enter the property.</param>
        /// <param name="propertyId">The property Id to load.</param>
        /// <param name="instance">The loaded property instance.</param>
        /// <returns>true if the property is ready to enter; otherwise false.</returns>
        public bool TryResolveEnterableInstance(uint player, string propertyId, out PropertyInstance instance)
        {
            instance = null;

            if (TryGetLoadedInstance(propertyId, out instance))
                return true;

            var state = GetPropertyLoadState(propertyId);
            if (state == PropertyLoadState.Failed)
            {
                SendPropertyLoadFailedMessage(player);
                return false;
            }

            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
            {
                SendPropertyLoadFailedMessage(player);
                return false;
            }

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            if (detail.SpawnType != PropertySpawnType.Instance)
            {
                SendPropertyLoadFailedMessage(player);
                return false;
            }

            AddPropertyLoadWaiter(player, propertyId);

            if (state == PropertyLoadState.Unloaded)
            {
                QueuePropertyLoad(propertyId, PropertyLoadPriority.PlayerRequest);
            }
            else if (state == PropertyLoadState.Queued)
            {
                PromotePropertyLoad(propertyId, PropertyLoadPriority.PlayerRequest);
            }

            SendPropertyLoadingMessage(player);
            return false;
        }

        private bool QueuePropertyLoad(string propertyId, PropertyLoadPriority priority)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return false;

            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
            {
                SetPropertyLoadState(propertyId, PropertyLoadState.Failed);
                _propertyLoadFailures[propertyId] = "Property does not exist in the database.";
                return false;
            }

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            if (detail.SpawnType != PropertySpawnType.Instance)
                return false;

            var state = GetPropertyLoadState(propertyId);
            if (state == PropertyLoadState.Loaded)
                return true;

            if (state == PropertyLoadState.Failed && priority != PropertyLoadPriority.StaffRequest)
                return false;

            TrackStartupPropertyLoadQueued(propertyId, priority);

            var existing = _propertyLoadJobs.FirstOrDefault(x => x.PropertyId == propertyId);
            if (existing != null)
            {
                if (priority > existing.Priority)
                    existing.Priority = priority;

                EnsurePropertyLoadProcessor();
                return true;
            }

            _propertyLoadFailures.Remove(propertyId);
            _failedStartupPropertyLoadIds.Remove(propertyId);
            SetPropertyLoadState(propertyId, PropertyLoadState.Queued);
            _propertyLoadJobs.Add(new PropertyLoadJob
            {
                PropertyId = propertyId,
                Priority = priority
            });

            EnsurePropertyLoadProcessor();
            return true;
        }

        private void PromotePropertyLoad(string propertyId, PropertyLoadPriority priority)
        {
            var job = _propertyLoadJobs.FirstOrDefault(x => x.PropertyId == propertyId);
            if (job != null && priority > job.Priority)
                job.Priority = priority;
        }

        public bool RetryPropertyLoad(string propertyId)
        {
            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
                return false;

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            if (detail.SpawnType != PropertySpawnType.Instance)
                return false;

            var state = GetPropertyLoadState(propertyId);
            if (state == PropertyLoadState.Loaded)
                return QueueExteriorStructuresForInterior(propertyId);

            if (state == PropertyLoadState.Loading)
            {
                return false;
            }

            if (state == PropertyLoadState.Queued)
            {
                PromotePropertyLoad(propertyId, PropertyLoadPriority.StaffRequest);
                EnsurePropertyLoadProcessor();
                return true;
            }

            _propertyLoadFailures.Remove(propertyId);
            SetPropertyLoadState(propertyId, PropertyLoadState.Unloaded);
            return QueuePropertyLoad(propertyId, PropertyLoadPriority.StaffRequest);
        }

        public bool AbortQueuedPropertyLoad(string propertyId)
        {
            var job = _propertyLoadJobs.FirstOrDefault(x => x.PropertyId == propertyId);
            if (job == null || GetPropertyLoadState(propertyId) == PropertyLoadState.Loading)
                return false;

            _propertyLoadJobs.Remove(job);
            SetPropertyLoadState(propertyId, PropertyLoadState.Failed);
            _propertyLoadFailures[propertyId] = "Load aborted by staff before it started.";
            TrackStartupPropertyLoadFailed(propertyId);
            NotifyPropertyLoadWaiters(propertyId, false);
            LogStartupPropertyLoadProgress();
            return true;
        }

        private void ResetStartupPropertyLoadProgress()
        {
            _startupPropertyLoadIds.Clear();
            _completedStartupPropertyLoadIds.Clear();
            _failedStartupPropertyLoadIds.Clear();
            _lastStartupPropertyLoadProgressLoaded = -1;
            _lastStartupPropertyLoadProgressRemaining = -1;
            _lastStartupPropertyLoadProgressFailed = -1;
        }

        private void TrackStartupPropertyLoadQueued(string propertyId, PropertyLoadPriority priority)
        {
            if (priority == PropertyLoadPriority.Startup)
                _startupPropertyLoadIds.Add(propertyId);
        }

        private void TrackStartupPropertyLoadCompleted(string propertyId)
        {
            if (!_startupPropertyLoadIds.Contains(propertyId))
                return;

            _completedStartupPropertyLoadIds.Add(propertyId);
            _failedStartupPropertyLoadIds.Remove(propertyId);
        }

        private void TrackStartupPropertyLoadFailed(string propertyId)
        {
            if (!_startupPropertyLoadIds.Contains(propertyId))
                return;

            _failedStartupPropertyLoadIds.Add(propertyId);
            _completedStartupPropertyLoadIds.Remove(propertyId);
        }

        private void LogStartupPropertyLoadProgress(bool force = false)
        {
            var total = _startupPropertyLoadIds.Count;
            if (total <= 0)
                return;

            var loaded = _completedStartupPropertyLoadIds.Count;
            var failed = _failedStartupPropertyLoadIds.Count;
            var remaining = Math.Max(total - loaded - failed, 0);
            var isMilestone = loaded == 0 ||
                              remaining == 0 ||
                              failed != _lastStartupPropertyLoadProgressFailed ||
                              loaded % PropertyLoadProgressReportInterval == 0;

            if (!force && !isMilestone)
                return;

            if (loaded == _lastStartupPropertyLoadProgressLoaded &&
                remaining == _lastStartupPropertyLoadProgressRemaining &&
                failed == _lastStartupPropertyLoadProgressFailed)
            {
                return;
            }

            _lastStartupPropertyLoadProgressLoaded = loaded;
            _lastStartupPropertyLoadProgressRemaining = remaining;
            _lastStartupPropertyLoadProgressFailed = failed;

            Log.Write(LogGroup.Property, $"Startup instance property load progress: {loaded} loaded, {remaining} remaining, {failed} failed ({total} total).", true);
        }

        private void EnsurePropertyLoadProcessor()
        {
            if (_propertyLoadProcessorScheduled)
                return;

            _propertyLoadProcessorScheduled = true;
            ScheduleLoadBatch(ProcessPropertyLoadQueue, PropertyLoadBatchDelay);
        }

        private void ProcessPropertyLoadQueue()
        {
            _propertyLoadProcessorScheduled = false;

            var budget = PropertyLoadBatchSize;
            while (budget > 0)
            {
                if (_propertyLoadJobs.Count > 0)
                {
                    var job = _propertyLoadJobs
                        .OrderByDescending(x => x.Priority)
                        .First();

                    var consumed = ProcessPropertyLoadJob(job, budget);
                    budget -= Math.Max(consumed, 1);
                    continue;
                }

                if (_pendingStartupWorldPropertyIds.Count > 0)
                {
                    var consumed = ProcessStartupWorldProperties(budget);
                    if (consumed <= 0)
                        break;

                    budget -= consumed;
                    continue;
                }

                break;
            }

            if (_propertyLoadJobs.Count > 0 ||
                _pendingStartupWorldPropertyIds.Count > 0)
            {
                EnsurePropertyLoadProcessor();
            }
        }

        private int ProcessPropertyLoadJob(PropertyLoadJob job, int budget)
        {
            try
            {
                job.Property ??= _db.Get<WorldProperty>(job.PropertyId);
                if (job.Property == null)
                {
                    FailPropertyLoad(job, "Property does not exist in the database.");
                    return 1;
                }

                SetPropertyLoadState(job.PropertyId, PropertyLoadState.Loading);

                if (job.Phase == PropertyLoadJobPhase.CreateArea)
                {
                    var layout = _catalog.GetLayoutByType(job.Property.Layout);
                    uint targetArea;
                    if (!_propertyInstances.TryGetValue(job.PropertyId, out var instance) ||
                        !GetIsObjectValid(instance.Area))
                    {
                        targetArea = _area.CreateInstance(layout.AreaInstanceResref);
                        if (!GetIsObjectValid(targetArea))
                            throw new InvalidOperationException($"Unable to create property area from resref '{layout.AreaInstanceResref}'.");

                        RegisterInstance(job.Property.Id, targetArea, job.Property.Layout);

                        SetName(targetArea, "{PC} " + job.Property.CustomName);
                    }
                    else
                    {
                        targetArea = instance.Area;
                    }

                    if (!_completedInstanceSpawnActions.Contains(job.PropertyId))
                    {
                        layout.OnSpawnAction?.Invoke(targetArea);
                        _completedInstanceSpawnActions.Add(job.PropertyId);
                    }

                    job.Area = _propertyInstances[job.PropertyId].Area;
                    job.Structures = GetPropertyStructures(job.PropertyId);
                    job.Phase = PropertyLoadJobPhase.SpawnStructures;
                    return Math.Max(budget, 1);
                }

                var consumed = SpawnPropertyStructures(job, budget);
                if (job.StructureIndex >= job.Structures.Count)
                {
                    CompletePropertyLoad(job);
                }

                return consumed;
            }
            catch (Exception ex)
            {
                FailPropertyLoad(job, ex.ToString());
                return 1;
            }
        }

        private List<WorldProperty> GetPropertyStructures(string parentPropertyId)
        {
            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.ParentPropertyId), parentPropertyId, false)
                .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Structure);
            var count = (int)_db.SearchCount(query);
            return _db.Search(query.AddPaging(count, 0)).ToList();
        }

        private int SpawnPropertyStructures(PropertyLoadJob job, int budget)
        {
            var consumed = 0;
            while (job.StructureIndex < job.Structures.Count && consumed < budget)
            {
                SpawnIntoWorld(job.Structures[job.StructureIndex], job.Area);
                job.StructureIndex++;
                consumed++;
            }

            return consumed;
        }

        private void CompletePropertyLoad(PropertyLoadJob job)
        {
            _propertyLoadJobs.Remove(job);
            _propertyLoadFailures.Remove(job.PropertyId);
            SetPropertyLoadState(job.PropertyId, PropertyLoadState.Loaded);
            TrackStartupPropertyLoadCompleted(job.PropertyId);
            QueueExteriorStructuresForInterior(job.PropertyId);
            NotifyPropertyLoadWaiters(job.PropertyId, true);
            LogStartupPropertyLoadProgress();

            Log.Write(LogGroup.Property, $"Property '{job.Property.CustomName}' ({job.PropertyId}) loaded on {job.Priority} queue.");
        }

        private bool QueueExteriorStructuresForInterior(string interiorPropertyId)
        {
            var interior = _db.Get<WorldProperty>(interiorPropertyId);
            if (interior == null ||
                string.IsNullOrWhiteSpace(interior.ParentPropertyId))
            {
                return false;
            }

            var structure = _db.Get<WorldProperty>(interior.ParentPropertyId);
            if (structure == null ||
                structure.PropertyType != PropertyType.Structure ||
                !structure.ChildPropertyIds.ContainsKey(PropertyChildType.Interior) ||
                !structure.ChildPropertyIds[PropertyChildType.Interior].Contains(interiorPropertyId))
            {
                return false;
            }

            var parent = _db.Get<WorldProperty>(structure.ParentPropertyId);
            if (parent == null)
                return false;

            var parentDetail = _catalog.GetPropertyDetail(parent.PropertyType);
            if (parentDetail.SpawnType == PropertySpawnType.Instance)
                return false;

            QueueStartupWorldProperty(structure);
            return true;
        }

        private void FailPropertyLoad(PropertyLoadJob job, string failure)
        {
            _propertyLoadJobs.Remove(job);
            SetPropertyLoadState(job.PropertyId, PropertyLoadState.Failed);
            var property = job.Property ?? _db.Get<WorldProperty>(job.PropertyId);
            var structureId = job.Phase == PropertyLoadJobPhase.SpawnStructures &&
                              job.Structures != null &&
                              job.StructureIndex >= 0 &&
                              job.StructureIndex < job.Structures.Count
                ? job.Structures[job.StructureIndex].Id
                : string.Empty;
            var failureDetail =
                $"PropertyId: {job.PropertyId}; " +
                $"Type: {property?.PropertyType.ToString() ?? "Unknown"}; " +
                $"Layout: {property?.Layout.ToString() ?? "Unknown"}; " +
                $"Phase: {job.Phase}; " +
                $"StructureId: {structureId}; " +
                $"Error: {failure}";
            _propertyLoadFailures[job.PropertyId] = failureDetail;
            TrackStartupPropertyLoadFailed(job.PropertyId);
            NotifyPropertyLoadWaiters(job.PropertyId, false);
            LogStartupPropertyLoadProgress();

            Log.Write(LogGroup.Error, $"Property failed to load. {failureDetail}");
        }

        private void QueueStartupWorldProperty(WorldProperty property)
        {
            if (property == null)
                return;

            if (!_pendingStartupWorldPropertyIds.Contains(property.Id))
                _pendingStartupWorldPropertyIds.Add(property.Id);

            EnsurePropertyLoadProcessor();
        }

        private int ProcessStartupWorldProperties(int budget)
        {
            var consumed = 0;
            var scanned = 0;

            while (_pendingStartupWorldPropertyIds.Count > 0 &&
                   consumed < budget &&
                   scanned < _pendingStartupWorldPropertyIds.Count)
            {
                var propertyId = _pendingStartupWorldPropertyIds[0];
                var property = _db.Get<WorldProperty>(propertyId);
                if (property == null)
                {
                    _pendingStartupWorldPropertyIds.RemoveAt(0);
                    consumed++;
                    continue;
                }

                bool resolved;
                bool dependencyFailed;
                uint area;
                try
                {
                    resolved = TryResolveWorldStructureArea(property, out area, out dependencyFailed);
                }
                catch (Exception ex)
                {
                    _pendingStartupWorldPropertyIds.RemoveAt(0);
                    Log.Write(LogGroup.Error, $"World structure '{property.CustomName}' ({property.Id}) failed while resolving dependencies: {ex}");
                    consumed++;
                    continue;
                }

                if (!resolved)
                {
                    if (dependencyFailed)
                    {
                        _pendingStartupWorldPropertyIds.RemoveAt(0);
                        Log.Write(LogGroup.Error, $"World structure '{property.CustomName}' ({property.Id}) was not spawned because one of its property dependencies failed to load.");
                        consumed++;
                        continue;
                    }

                    _pendingStartupWorldPropertyIds.RemoveAt(0);
                    _pendingStartupWorldPropertyIds.Add(propertyId);
                    scanned++;
                    continue;
                }

                try
                {
                    SpawnIntoWorld(property, area);
                }
                catch (Exception ex)
                {
                    Log.Write(LogGroup.Error, $"World structure '{property.CustomName}' ({property.Id}) failed to spawn: {ex}");
                }

                _pendingStartupWorldPropertyIds.RemoveAt(0);
                consumed++;
            }

            return consumed;
        }

        private bool TryResolveWorldStructureArea(WorldProperty property, out uint area, out bool dependencyFailed)
        {
            area = OBJECT_INVALID;
            dependencyFailed = false;

            var parent = _db.Get<WorldProperty>(property.ParentPropertyId);
            if (parent == null)
            {
                dependencyFailed = true;
                return false;
            }

            var parentDetail = _catalog.GetPropertyDetail(parent.PropertyType);
            if (parentDetail.SpawnType == PropertySpawnType.Instance)
            {
                var state = GetPropertyLoadState(parent.Id);
                if (state == PropertyLoadState.Failed)
                {
                    dependencyFailed = true;
                    return false;
                }

                if (state == PropertyLoadState.Unloaded)
                    QueuePropertyLoad(parent.Id, PropertyLoadPriority.Startup);

                if (!TryGetLoadedInstance(parent.Id, out var instance))
                    return false;

                area = instance.Area;
            }
            else
            {
                area = _area.GetAreaByResref(parent.ParentPropertyId);
                if (!GetIsObjectValid(area))
                {
                    dependencyFailed = true;
                    return false;
                }
            }

            if (!property.ChildPropertyIds.ContainsKey(PropertyChildType.Interior))
                return true;

            var interiorId = property.ChildPropertyIds[PropertyChildType.Interior].SingleOrDefault();
            if (string.IsNullOrWhiteSpace(interiorId))
                return true;

            var interior = _db.Get<WorldProperty>(interiorId);
            if (interior == null)
            {
                dependencyFailed = true;
                return false;
            }

            var interiorDetail = _catalog.GetPropertyDetail(interior.PropertyType);
            if (interiorDetail.SpawnType != PropertySpawnType.Instance)
                return true;

            var interiorState = GetPropertyLoadState(interiorId);
            if (interiorState == PropertyLoadState.Failed)
            {
                dependencyFailed = true;
                return false;
            }

            if (interiorState == PropertyLoadState.Unloaded && !IsPropertyOnDemand(interior))
                QueuePropertyLoad(interiorId, PropertyLoadPriority.Startup);

            return IsPropertyOnDemand(interior) ||
                   TryGetLoadedInstance(interiorId, out _);
        }

        /// <summary>
        /// When the module loads, load all properties.
        /// </summary>
        public void LoadProperties()
        {
            _propertyLoadJobs.Clear();
            _pendingStartupWorldPropertyIds.Clear();
            _propertyLoadStates.Clear();
            _propertyLoadFailures.Clear();
            _propertyLoadWaiters.Clear();
            _completedInstanceSpawnActions.Clear();
            ResetStartupPropertyLoadProgress();
            _propertyLoadProcessorScheduled = false;

            var instanceTypes = _catalog.PropertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.Instance)
                .Select(s => (int)s.Key)
                .ToList();
            var worldTypes = _catalog.PropertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.World)
                .Select(s => (int)s.Key)
                .ToList();
            var areaTypes = _catalog.PropertyTypes
                .Where(x => x.Value.SpawnType == PropertySpawnType.Area)
                .Select(s => (int)s.Key)
                .ToList();

            var instanceQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), instanceTypes);
            var instancePropertiesCount = _db.SearchCount(instanceQuery);
            var instanceProperties = _db.Search(instanceQuery
                .AddPaging((int)instancePropertiesCount, 0))
                .ToList();

            var worldQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), worldTypes);
            var worldPropertiesCount = _db.SearchCount(worldQuery);
            var worldProperties = _db.Search(worldQuery
                .AddPaging((int)worldPropertiesCount, 0))
                .ToList();

            var areaQuery = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.PropertyType), areaTypes);
            var areaPropertiesCount = _db.SearchCount(areaQuery);
            var areaProperties = _db.Search(areaQuery
                .AddPaging((int)areaPropertiesCount, 0))
                .ToList();

            foreach (var property in instanceProperties)
            {
                if (IsPropertyOnDemand(property))
                {
                    SetPropertyLoadState(property.Id, PropertyLoadState.Unloaded);
                    continue;
                }

                if (IsPropertyStartupLoaded(property))
                    QueuePropertyLoad(property.Id, PropertyLoadPriority.Startup);
            }

            foreach (var property in worldProperties)
            {
                var parent = _db.Get<WorldProperty>(property.ParentPropertyId);
                if (parent == null)
                {
                    Log.Write(LogGroup.Error, $"Error loading property '{property.Id}'. Its parent object '{property.ParentPropertyId}' does not exist in database.");
                    continue;
                }

                var parentDetail = _catalog.GetPropertyDetail(parent.PropertyType);
                if (parentDetail.SpawnType == PropertySpawnType.Instance)
                    continue;

                QueueStartupWorldProperty(property);
            }

            foreach (var property in areaProperties)
            {
                var area = _area.GetAreaByResref(property.ParentPropertyId);
                SpawnIntoWorld(property, area);
            }


            Log.Write(LogGroup.Property, $"Queued {instanceProperties.Count(x => !IsPropertyOnDemand(x))} startup instanced properties.", true);
            Log.Write(LogGroup.Property, $"Deferred {instanceProperties.Count(IsPropertyOnDemand)} on-demand instanced properties.", true);
            Log.Write(LogGroup.Property, $"Queued {_pendingStartupWorldPropertyIds.Count} startup world properties.", true);
            Log.Write(LogGroup.Property, $"Loaded {areaProperties.Count} area properties.", true);
            LogStartupPropertyLoadProgress(true);
        }

        /// <summary>
        /// Retrieves a placeable associated with a property Id.
        /// </summary>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A placeable or OBJECT_INVALID if not found.</returns>
        public uint GetPlaceableByPropertyId(string propertyId)
        {
            return !_structurePropertyIdToPlaceable.ContainsKey(propertyId)
                ? OBJECT_INVALID
                : _structurePropertyIdToPlaceable[propertyId];
        }

        /// <summary>
        /// When a player enters a property instance, add them to the list of players.
        /// </summary>
        public void EnterPropertyInstance()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (GetPropertyLoadState(propertyId) != PropertyLoadState.Loaded)
                return;

            if (!_propertyInstances[propertyId].Players.Contains(player))
                _propertyInstances[propertyId].Players.Add(player);
        }

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        public void ExitPropertyInstance()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (GetPropertyLoadState(propertyId) != PropertyLoadState.Loaded)
                return;

            if (_propertyInstances[propertyId].Players.Contains(player))
                _propertyInstances[propertyId].Players.Remove(player);
        }

        /// <summary>
        /// Spawns the property into the game world.
        /// For structures, this means spawning a placeable at the location.
        /// For cities, starships, apartments, and buildings this means spawning area instances.
        /// </summary>
        /// <param name="property">The property to spawn into the world.</param>
        /// <param name="area">The area to spawn the property into. Leave OBJECT_INVALID if spawning an instance.</param>
        public void SpawnIntoWorld(WorldProperty property, uint area)
        {
            var propertyDetail = _catalog.GetPropertyDetail(property.PropertyType);

            // World spawns represent placeables within the game world such as furniture and buildings
            if (propertyDetail.SpawnType == PropertySpawnType.World)
            {
                if (_structurePropertyIdToPlaceable.TryGetValue(property.Id, out var existingPlaceable) &&
                    GetIsObjectValid(existingPlaceable))
                {
                    _catalog.RunStructureChangedEvent(property.StructureType, StructureChangeType.PositionChanged, property, existingPlaceable);
                    return;
                }

                var furniture = _catalog.GetStructureByType(property.StructureType);

                var staticPosition = property.Positions[PropertyLocationType.StaticPosition];
                var position = Vector3(staticPosition.X, staticPosition.Y, staticPosition.Z);
                var location = Location(area, position, staticPosition.Orientation);

                var placeable = CreateObject(ObjectType.Placeable, furniture.Resref, location);
                SetPlotFlag(placeable, true);
                AssignPropertyId(placeable, property.Id);

                _structurePropertyIdToPlaceable[property.Id] = placeable;

                // Some structures have custom spawn-in actions which also need to be run
                // when brought into the world.
                _catalog.RunStructureChangedEvent(property.StructureType, StructureChangeType.PositionChanged, property, placeable);
            }
            // Instance spawns are instanced areas that are spawned dynamically into the game world.
            else if(propertyDetail.SpawnType == PropertySpawnType.Instance)
            {
                var layout = _catalog.GetLayoutByType(property.Layout);
                if (_propertyInstances.TryGetValue(property.Id, out var existingInstance) &&
                    GetIsObjectValid(existingInstance.Area))
                {
                    if (!_completedInstanceSpawnActions.Contains(property.Id))
                    {
                        layout.OnSpawnAction?.Invoke(existingInstance.Area);
                        _completedInstanceSpawnActions.Add(property.Id);
                    }

                    SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
                    return;
                }

                // If no interior layout is defined, the provided area will be used.
                var targetArea = _area.CreateInstance(layout.AreaInstanceResref);
                if (!GetIsObjectValid(targetArea))
                    throw new InvalidOperationException($"Unable to create property area from resref '{layout.AreaInstanceResref}'.");

                RegisterInstance(property.Id, targetArea, property.Layout);

                SetName(targetArea, "{PC} " + property.CustomName);

                if (layout.OnSpawnAction != null)
                {
                    layout.OnSpawnAction(targetArea);
                }

                _completedInstanceSpawnActions.Add(property.Id);
                SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
            }
            // Area spawns exist in a pre-built area.
            else if(propertyDetail.SpawnType == PropertySpawnType.Area)
            {
                AssignPropertyId(area, property.Id);
                SetPropertyLoadState(property.Id, PropertyLoadState.Loaded);
            }
        }

        /// <inheritdoc />
        public void RegisterStructurePlaceable(string propertyId, uint placeable)
        {
            _structurePropertyIdToPlaceable[propertyId] = placeable;
        }

        /// <inheritdoc />
        public void ForgetProperty(string propertyId)
        {
            _propertyInstances.Remove(propertyId);
            _propertyLoadStates.Remove(propertyId);
            _propertyLoadFailures.Remove(propertyId);
            _propertyLoadWaiters.Remove(propertyId);
            _completedInstanceSpawnActions.Remove(propertyId);
            _propertyLoadJobs.RemoveAll(x => x.PropertyId == propertyId);
            _pendingStartupWorldPropertyIds.Remove(propertyId);
            _structurePropertyIdToPlaceable.Remove(propertyId);
        }

        /// <summary>
        /// Schedules the next batch of the load queue. A test replaces it to run the batches without waiting for the clock.
        /// </summary>
        /// <param name="task">The batch to run.</param>
        /// <param name="delay">How long to wait before it runs.</param>
        protected virtual void ScheduleLoadBatch(Action task, TimeSpan delay)
        {
            Scheduler.Schedule(task, delay);
        }
    }
}
