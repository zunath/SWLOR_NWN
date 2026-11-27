using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.World;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Instance loading: the queue that loads property area instances a few at a time, the startup load of the world
    /// properties and the load diagnostics. The logic and state that used to be part of the static property class, moved
    /// here unchanged. The registry of the instances, load states, failures and waiting players is
    /// <see cref="IPropertyLoadRegistryService"/>, spawning a property into the world is <see cref="IPropertySpawnService"/> and the
    /// hot reload of the loader is <see cref="IPropertyLoadReloadService"/> (split out of the loader).
    /// </summary>
    public class PropertyLoaderService : IPropertyLoaderService
    {
        private readonly IDBService _db;
        private readonly IAreaService _area;
        private readonly IPropertyCatalogService _catalog;
        private readonly IPropertyLoadRegistryService _registry;
        private readonly IPropertySpawnService _spawn;

        // A hot reload rebuilds the instances, placeables and spawn actions from the live world and takes the rest of the
        // bookkeeping from the old build (CarryLoadState, AdoptLiveProperties). The queued jobs are carried by property id and
        // read their property again, and the progress figures only shorten the log, so their digests are not compared.
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly List<PropertyLoadJob> _propertyLoadJobs = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly List<string> _pendingStartupWorldPropertyIds = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly HashSet<string> _startupPropertyLoadIds = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly HashSet<string> _completedStartupPropertyLoadIds = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly HashSet<string> _failedStartupPropertyLoadIds = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private bool _propertyLoadProcessorScheduled;
        [ReloadState(ReloadStateKind.Runtime)]
        private int _lastStartupPropertyLoadProgressLoaded = -1;
        [ReloadState(ReloadStateKind.Runtime)]
        private int _lastStartupPropertyLoadProgressRemaining = -1;
        [ReloadState(ReloadStateKind.Runtime)]
        private int _lastStartupPropertyLoadProgressFailed = -1;

        private const int PropertyLoadBatchSize = 5;
        private const int PropertyLoadProgressReportInterval = 25;
        private TimeSpan PropertyLoadBatchDelay => TimeSpan.FromSeconds(0.2d);

        public PropertyLoaderService(
            IDBService db,
            IAreaService area,
            IPropertyCatalogService catalog,
            IPropertyLoadRegistryService registry,
            IPropertySpawnService spawn)
        {
            _db = db;
            _area = area;
            _catalog = catalog;
            _registry = registry;
            _spawn = spawn;
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
                var state = _registry.GetPropertyLoadState(property.Id);
                var expectedChildCount = job?.Structures.Count > 0
                    ? job.Structures.Count
                    : GetPropertyStructures(property.Id).Count;
                var spawnedChildCount = job != null
                    ? job.StructureIndex
                    : state == PropertyLoadState.Loaded
                        ? expectedChildCount
                        : 0;
                var isLoadedAreaValid = _registry.TryGetRegisteredInstance(property.Id, out var instance) &&
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
                    Failure = _registry.GetPropertyLoadFailure(property.Id),
                    WaiterCount = _registry.GetWaiterCount(property.Id),
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

            if (_registry.TryGetLoadedInstance(propertyId, out instance))
                return true;

            var state = _registry.GetPropertyLoadState(propertyId);
            if (state == PropertyLoadState.Failed)
            {
                _registry.SendPropertyLoadFailedMessage(player);
                return false;
            }

            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
            {
                _registry.SendPropertyLoadFailedMessage(player);
                return false;
            }

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            if (detail.SpawnType != PropertySpawnType.Instance)
            {
                _registry.SendPropertyLoadFailedMessage(player);
                return false;
            }

            _registry.AddPropertyLoadWaiter(player, propertyId);

            if (state == PropertyLoadState.Unloaded)
            {
                QueuePropertyLoad(propertyId, PropertyLoadPriority.PlayerRequest);
            }
            else if (state == PropertyLoadState.Queued)
            {
                PromotePropertyLoad(propertyId, PropertyLoadPriority.PlayerRequest);
            }

            _registry.SendPropertyLoadingMessage(player);
            return false;
        }

        private bool QueuePropertyLoad(string propertyId, PropertyLoadPriority priority)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return false;

            var property = _db.Get<WorldProperty>(propertyId);
            if (property == null)
            {
                _registry.SetPropertyLoadState(propertyId, PropertyLoadState.Failed);
                _registry.SetPropertyLoadFailure(propertyId, "Property does not exist in the database.");
                return false;
            }

            var detail = _catalog.GetPropertyDetail(property.PropertyType);
            if (detail.SpawnType != PropertySpawnType.Instance)
                return false;

            var state = _registry.GetPropertyLoadState(propertyId);
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

            _registry.ClearPropertyLoadFailure(propertyId);
            _failedStartupPropertyLoadIds.Remove(propertyId);
            _registry.SetPropertyLoadState(propertyId, PropertyLoadState.Queued);
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

            var state = _registry.GetPropertyLoadState(propertyId);
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

            _registry.ClearPropertyLoadFailure(propertyId);
            _registry.SetPropertyLoadState(propertyId, PropertyLoadState.Unloaded);
            return QueuePropertyLoad(propertyId, PropertyLoadPriority.StaffRequest);
        }

        public bool AbortQueuedPropertyLoad(string propertyId)
        {
            var job = _propertyLoadJobs.FirstOrDefault(x => x.PropertyId == propertyId);
            if (job == null || _registry.GetPropertyLoadState(propertyId) == PropertyLoadState.Loading)
                return false;

            _propertyLoadJobs.Remove(job);
            _registry.SetPropertyLoadState(propertyId, PropertyLoadState.Failed);
            _registry.SetPropertyLoadFailure(propertyId, "Load aborted by staff before it started.");
            TrackStartupPropertyLoadFailed(propertyId);
            _registry.NotifyPropertyLoadWaiters(propertyId, false);
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

                _registry.SetPropertyLoadState(job.PropertyId, PropertyLoadState.Loading);

                if (job.Phase == PropertyLoadJobPhase.CreateArea)
                {
                    var layout = _catalog.GetLayoutByType(job.Property.Layout);
                    uint targetArea;
                    if (!_registry.TryGetRegisteredInstance(job.PropertyId, out var instance) ||
                        !GetIsObjectValid(instance.Area))
                    {
                        targetArea = _area.CreateInstance(layout.AreaInstanceResref);
                        if (!GetIsObjectValid(targetArea))
                            throw new InvalidOperationException($"Unable to create property area from resref '{layout.AreaInstanceResref}'.");

                        _registry.RegisterInstance(job.Property.Id, targetArea, job.Property.Layout);

                        SetName(targetArea, "{PC} " + job.Property.CustomName);
                    }
                    else
                    {
                        targetArea = instance.Area;
                    }

                    if (!_registry.IsSpawnActionCompleted(job.PropertyId))
                    {
                        layout.OnSpawnAction?.Invoke(targetArea);
                        _registry.MarkSpawnActionCompleted(job.PropertyId);
                    }

                    _registry.TryGetRegisteredInstance(job.PropertyId, out var registeredInstance);
                    job.Area = registeredInstance.Area;
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
                _spawn.SpawnIntoWorld(job.Structures[job.StructureIndex], job.Area);
                job.StructureIndex++;
                consumed++;
            }

            return consumed;
        }

        private void CompletePropertyLoad(PropertyLoadJob job)
        {
            _propertyLoadJobs.Remove(job);
            _registry.ClearPropertyLoadFailure(job.PropertyId);
            _registry.SetPropertyLoadState(job.PropertyId, PropertyLoadState.Loaded);
            TrackStartupPropertyLoadCompleted(job.PropertyId);
            QueueExteriorStructuresForInterior(job.PropertyId);
            _registry.NotifyPropertyLoadWaiters(job.PropertyId, true);
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
            _registry.SetPropertyLoadState(job.PropertyId, PropertyLoadState.Failed);
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
            _registry.SetPropertyLoadFailure(job.PropertyId, failureDetail);
            TrackStartupPropertyLoadFailed(job.PropertyId);
            _registry.NotifyPropertyLoadWaiters(job.PropertyId, false);
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
                    _spawn.SpawnIntoWorld(property, area);
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
                var state = _registry.GetPropertyLoadState(parent.Id);
                if (state == PropertyLoadState.Failed)
                {
                    dependencyFailed = true;
                    return false;
                }

                if (state == PropertyLoadState.Unloaded)
                    QueuePropertyLoad(parent.Id, PropertyLoadPriority.Startup);

                if (!_registry.TryGetLoadedInstance(parent.Id, out var instance))
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

            var interiorState = _registry.GetPropertyLoadState(interiorId);
            if (interiorState == PropertyLoadState.Failed)
            {
                dependencyFailed = true;
                return false;
            }

            if (interiorState == PropertyLoadState.Unloaded && !IsPropertyOnDemand(interior))
                QueuePropertyLoad(interiorId, PropertyLoadPriority.Startup);

            return IsPropertyOnDemand(interior) ||
                   _registry.TryGetLoadedInstance(interiorId, out _);
        }

        /// <summary>
        /// When the module loads, load all properties.
        /// </summary>
        public void LoadProperties()
        {
            _propertyLoadJobs.Clear();
            _pendingStartupWorldPropertyIds.Clear();
            _registry.ResetLoadBookkeeping();
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
                    _registry.SetPropertyLoadState(property.Id, PropertyLoadState.Unloaded);
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
                _spawn.SpawnIntoWorld(property, area);
            }


            Log.Write(LogGroup.Property, $"Queued {instanceProperties.Count(x => !IsPropertyOnDemand(x))} startup instanced properties.", true);
            Log.Write(LogGroup.Property, $"Deferred {instanceProperties.Count(IsPropertyOnDemand)} on-demand instanced properties.", true);
            Log.Write(LogGroup.Property, $"Queued {_pendingStartupWorldPropertyIds.Count} startup world properties.", true);
            Log.Write(LogGroup.Property, $"Loaded {areaProperties.Count} area properties.", true);
            LogStartupPropertyLoadProgress(true);
        }

        /// <summary>
        /// When a hot reload quiesces this build: the queued loads and world structures and the startup progress, which the world
        /// cannot tell, for <see cref="RestoreQueueState"/> in the next build.
        /// </summary>
        /// <returns>The carried bookkeeping of the queue.</returns>
        public JObject CarryQueueState()
        {
            return new JObject
            {
                ["jobs"] = new JArray(_propertyLoadJobs.Select(job => new JObject
                {
                    ["id"] = job.PropertyId,
                    ["priority"] = (int)job.Priority,
                    ["phase"] = (int)job.Phase,
                })),
                ["pendingWorld"] = JArray.FromObject(_pendingStartupWorldPropertyIds),
                ["startup"] = JArray.FromObject(_startupPropertyLoadIds),
                ["startupLoaded"] = JArray.FromObject(_completedStartupPropertyLoadIds),
                ["startupFailed"] = JArray.FromObject(_failedStartupPropertyLoadIds),
            };
        }

        /// <summary>
        /// Takes the bookkeeping the old build carried and resumes its queued loads: a load that had created its area spawns only
        /// the structures that are not in the world yet, so nothing is spawned twice.
        /// </summary>
        public void RestoreQueueState(JObject carry)
        {
            _pendingStartupWorldPropertyIds.AddRange(carry["pendingWorld"].ToObject<List<string>>());
            _startupPropertyLoadIds.UnionWith(carry["startup"].ToObject<List<string>>());
            _completedStartupPropertyLoadIds.UnionWith(carry["startupLoaded"].ToObject<List<string>>());
            _failedStartupPropertyLoadIds.UnionWith(carry["startupFailed"].ToObject<List<string>>());

            foreach (var carriedJob in carry["jobs"])
            {
                var job = new PropertyLoadJob
                {
                    PropertyId = carriedJob["id"].Value<string>(),
                    Priority = (PropertyLoadPriority)carriedJob["priority"].Value<int>(),
                    Phase = (PropertyLoadJobPhase)carriedJob["phase"].Value<int>(),
                };

                if (job.Phase == PropertyLoadJobPhase.SpawnStructures)
                {
                    if (_registry.TryGetRegisteredInstance(job.PropertyId, out var instance))
                    {
                        job.Area = instance.Area;
                        job.Structures = GetPropertyStructures(job.PropertyId)
                            .Where(structure => !(_registry.TryGetStructurePlaceable(structure.Id, out var placeable) && GetIsObjectValid(placeable)))
                            .ToList();
                    }
                    else
                    {
                        job.Phase = PropertyLoadJobPhase.CreateArea;
                    }
                }

                _propertyLoadJobs.Add(job);
            }
        }

        /// <summary>
        /// Starts the load processor when queued loads or world structures remain after an adoption.
        /// </summary>
        /// <returns>The number of queued instance loads and of queued world structures.</returns>
        public (int Jobs, int WorldStructures) ResumeQueuedLoads()
        {
            if (_propertyLoadJobs.Count > 0 || _pendingStartupWorldPropertyIds.Count > 0)
                EnsurePropertyLoadProcessor();

            return (_propertyLoadJobs.Count, _pendingStartupWorldPropertyIds.Count);
        }

        /// <summary>
        /// Forgets everything the loader knows about a property that is being deleted: its instance, load state,
        /// failure, waiting players, queued load and placeable.
        /// </summary>
        /// <param name="propertyId">The property Id.</param>
        public void ForgetProperty(string propertyId)
        {
            _registry.ForgetProperty(propertyId);
            _propertyLoadJobs.RemoveAll(x => x.PropertyId == propertyId);
            _pendingStartupWorldPropertyIds.Remove(propertyId);
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
