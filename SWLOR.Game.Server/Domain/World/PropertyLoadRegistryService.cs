using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The registry of property instances: the area instance of each property and the players in it, the load state and failure
    /// of each property, the players waiting for a load, the instance spawn actions that have run and the placeable of each
    /// structure, with the handlers that keep the players of an instance. Split from <c>PropertyLoaderService</c>; the
    /// members moved unchanged. The queue that loads the instances is <see cref="IPropertyLoaderService"/>, the spawning of a
    /// property is <see cref="IPropertySpawnService"/>.
    /// </summary>
    public class PropertyLoadRegistryService : IPropertyLoadRegistryService
    {
        private readonly IDBService _db;
        private readonly IPropertyCatalogService _catalog;

        // A hot reload rebuilds the instances, placeables and spawn actions from the live world and takes the rest of the
        // bookkeeping from the old build (the loader's CarryLoadState and AdoptLiveProperties).
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, PropertyInstance> _propertyInstances = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, PropertyLoadState> _propertyLoadStates = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, string> _propertyLoadFailures = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, HashSet<string>> _propertyLoadWaiters = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly HashSet<string> _completedInstanceSpawnActions = new();
        [ReloadState(ReloadStateKind.Adopted)]
        private readonly Dictionary<string, uint> _structurePropertyIdToPlaceable = new();

        private const string LastDescribedPropertyVariable = "PROPERTY_LAST_DESCRIBED";

        public PropertyLoadRegistryService(IDBService db, IPropertyCatalogService catalog)
        {
            _db = db;
            _catalog = catalog;
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

        public void AddPropertyLoadWaiter(uint player, string propertyId)
        {
            if (!GetIsPC(player) || string.IsNullOrWhiteSpace(propertyId))
                return;

            if (!_propertyLoadWaiters.ContainsKey(propertyId))
                _propertyLoadWaiters[propertyId] = new HashSet<string>();

            _propertyLoadWaiters[propertyId].Add(GetObjectUUID(player));
        }

        public void NotifyPropertyLoadWaiters(string propertyId, bool loaded)
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

        public void SendPropertyLoadingMessage(uint player)
        {
            SendMessageToPC(player, "This property is still loading. Please try again shortly.");
        }

        public void SendPropertyLoadFailedMessage(uint player)
        {
            SendMessageToPC(player, "This property could not be loaded. Please notify staff.");
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
            var player = GetEnteringObject();
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
        /// Shows a property's description to a player arriving from outside it. Coming back to the property's area, such as after a
        /// reload, does not repeat it.
        /// </summary>
        public void ShowPropertyDescription()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var propertyId = GetPropertyId(OBJECT_SELF);
            if (!_propertyInstances.ContainsKey(propertyId))
            {
                DeleteLocalString(player, LastDescribedPropertyVariable);
                return;
            }

            if (GetLocalString(player, LastDescribedPropertyVariable) == propertyId)
                return;

            SetLocalString(player, LastDescribedPropertyVariable, propertyId);

            var property = _db.Get<WorldProperty>(propertyId);
            var message = PropertyDescriptionText.Format(property?.CustomName, property?.CustomDescription);
            if (!string.IsNullOrEmpty(message))
                SendMessageToPC(player, ColorToken.Cyan(message));
        }

        /// <summary>
        /// When a player exits a property instance, remove them from the list of players.
        /// </summary>
        public void ExitPropertyInstance()
        {
            // No GetIsPC check: it is false here for a player logging out, and the entry must still go, because
            // the player keeps the same object ID when they log back in.
            var player = GetExitingObject();

            var propertyId = GetPropertyId(OBJECT_SELF);

            if (!_propertyInstances.ContainsKey(propertyId))
                return;

            if (GetPropertyLoadState(propertyId) != PropertyLoadState.Loaded)
                return;

            _propertyInstances[propertyId].Players.Remove(player);
        }

        /// <inheritdoc />
        public void RegisterStructurePlaceable(string propertyId, uint placeable)
        {
            _structurePropertyIdToPlaceable[propertyId] = placeable;
        }

        /// <inheritdoc />
        public void SetPropertyLoadFailure(string propertyId, string failure)
        {
            _propertyLoadFailures[propertyId] = failure;
        }

        /// <inheritdoc />
        public void ClearPropertyLoadFailure(string propertyId)
        {
            _propertyLoadFailures.Remove(propertyId);
        }

        /// <inheritdoc />
        public bool TryGetRegisteredInstance(string propertyId, out PropertyInstance instance)
        {
            return _propertyInstances.TryGetValue(propertyId, out instance);
        }

        /// <inheritdoc />
        public void AdoptInstance(string propertyId, PropertyInstance instance)
        {
            _propertyInstances[propertyId] = instance;
            _completedInstanceSpawnActions.Add(propertyId);
        }

        /// <inheritdoc />
        public bool IsSpawnActionCompleted(string propertyId)
        {
            return _completedInstanceSpawnActions.Contains(propertyId);
        }

        /// <inheritdoc />
        public void MarkSpawnActionCompleted(string propertyId)
        {
            _completedInstanceSpawnActions.Add(propertyId);
        }

        /// <inheritdoc />
        public int GetWaiterCount(string propertyId)
        {
            return _propertyLoadWaiters.TryGetValue(propertyId, out var waiters) ? waiters.Count : 0;
        }

        /// <inheritdoc />
        public bool TryGetStructurePlaceable(string propertyId, out uint placeable)
        {
            return _structurePropertyIdToPlaceable.TryGetValue(propertyId, out placeable);
        }

        /// <inheritdoc />
        public void ForgetProperty(string propertyId)
        {
            _propertyInstances.Remove(propertyId);
            _propertyLoadStates.Remove(propertyId);
            _propertyLoadFailures.Remove(propertyId);
            _propertyLoadWaiters.Remove(propertyId);
            _completedInstanceSpawnActions.Remove(propertyId);
            _structurePropertyIdToPlaceable.Remove(propertyId);
        }

        /// <inheritdoc />
        public void ResetLoadBookkeeping()
        {
            _propertyLoadStates.Clear();
            _propertyLoadFailures.Clear();
            _propertyLoadWaiters.Clear();
            _completedInstanceSpawnActions.Clear();
        }

        /// <inheritdoc />
        public JObject CarryState()
        {
            return new JObject
            {
                ["states"] = new JObject(_propertyLoadStates.Select(pair => new JProperty(pair.Key, (int)pair.Value))),
                ["failures"] = new JObject(_propertyLoadFailures.Select(pair => new JProperty(pair.Key, pair.Value))),
                ["waiters"] = new JObject(_propertyLoadWaiters.Select(pair => new JProperty(pair.Key, JArray.FromObject(pair.Value)))),
            };
        }

        /// <inheritdoc />
        public void RestoreState(JObject carry)
        {
            foreach (var (propertyId, state) in (JObject)carry["states"])
                _propertyLoadStates[propertyId] = (PropertyLoadState)state.Value<int>();
            foreach (var (propertyId, failure) in (JObject)carry["failures"])
                _propertyLoadFailures[propertyId] = failure.Value<string>();
            foreach (var (propertyId, waiters) in (JObject)carry["waiters"])
                _propertyLoadWaiters[propertyId] = new HashSet<string>(waiters.ToObject<List<string>>());
        }
    }
}
