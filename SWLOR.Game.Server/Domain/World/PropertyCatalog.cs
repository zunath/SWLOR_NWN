using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The definitions of the property system: the property types, layouts, entrances, permissions, structures and
    /// instance templates the module cache builds, and the lookups over them. The logic and state that used to be
    /// static in <c>Service.Property</c>, moved here unchanged. It depends on no other part of the property system.
    /// </summary>
    public class PropertyCatalog : IPropertyCatalog
    {
        private readonly IAreaService _area;

        private readonly Dictionary<StructureType, StructureAttribute> _activeStructures = new();
        private readonly Dictionary<PropertyType, PropertyTypeAttribute> _propertyTypes = new();
        private readonly Dictionary<PropertyLayoutType, PropertyLayout> _activeLayouts = new();
        private readonly Dictionary<PropertyType, List<PropertyLayoutType>> _layoutsByPropertyType = new();
        private readonly Dictionary<PropertyLayoutType, Vector4> _entrancesByLayout = new();
        private readonly Dictionary<PropertyPermissionType, PropertyPermissionAttribute> _activePermissions = new();
        private readonly Dictionary<string, uint> _instanceTemplates = new();
        private readonly Dictionary<PropertyType, List<PropertyPermissionType>> _permissionsByPropertyType = new();
        private readonly Dictionary<PropertyType, List<StructureType>> _structureTypesByPropertyType = new();
        private readonly Dictionary<StructureType, Dictionary<StructureChangeType, Action<WorldProperty, uint>>> _structureChangedActions = StructureChangedAction.BuildSpawnActions();

        public PropertyCatalog(IAreaService area)
        {
            _area = area;
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<PropertyType, PropertyTypeAttribute> PropertyTypes => _propertyTypes;

        /// <inheritdoc />
        public IReadOnlyDictionary<PropertyType, List<PropertyPermissionType>> PermissionsByPropertyType => _permissionsByPropertyType;

        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        public void CacheData()
        {
            CachePropertyTypes();
            CachePropertyLayoutTypes();
            CachePermissions();
            CacheStructures();
            CacheInstanceTemplates();
            CacheStructuresByPropertyType();
        }

        private void CachePropertyTypes()
        {
            var propertyTypes = Enum.GetValues(typeof(PropertyType)).Cast<PropertyType>();
            foreach (var type in propertyTypes)
            {
                var detail = type.GetAttribute<PropertyType, PropertyTypeAttribute>();
                _propertyTypes[type] = detail;
            }
        }

        private void CachePropertyLayoutTypes()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPropertyLayoutListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IPropertyLayoutListDefinition)Activator.CreateInstance(type);
                var layouts = instance.Build();

                foreach (var (layoutType, layout) in layouts)
                {
                    _activeLayouts[layoutType] = layout;

                    if (!_layoutsByPropertyType.ContainsKey(layout.PropertyType))
                        _layoutsByPropertyType[layout.PropertyType] = new List<PropertyLayoutType>();

                    _layoutsByPropertyType[layout.PropertyType].Add(layoutType);
                    _entrancesByLayout[layoutType] = GetEntrancePosition(layout.AreaInstanceResref);
                }
            }

            Console.WriteLine($"Loaded {_activeLayouts.Count} property layouts.");
        }

        private void CachePermissions()
        {
            var permissionTypes = Enum.GetValues(typeof(PropertyPermissionType)).Cast<PropertyPermissionType>();
            foreach (var type in permissionTypes)
            {
                var permission = type.GetAttribute<PropertyPermissionType, PropertyPermissionAttribute>();

                if (permission.IsActive)
                {
                    _activePermissions[type] = permission;
                }
            }

            // Assign the list of permissions associated with each property type.
            _permissionsByPropertyType[PropertyType.Apartment] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ExtendLease,
                PropertyPermissionType.CancelLease,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories
            };

            _permissionsByPropertyType[PropertyType.CityHall] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription
            };

            _permissionsByPropertyType[PropertyType.Starship] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories,
                PropertyPermissionType.PilotShip,
                PropertyPermissionType.RefitShip
            };

            _permissionsByPropertyType[PropertyType.City] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditTaxes,
                PropertyPermissionType.AccessTreasury,
                PropertyPermissionType.ManageUpgrades,
                PropertyPermissionType.ManageUpkeep
            };

            _permissionsByPropertyType[PropertyType.Category] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.AccessStorage
            };

            _permissionsByPropertyType[PropertyType.Bank] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EditCategories,
            };

            _permissionsByPropertyType[PropertyType.MedicalCenter] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.Starport] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.Cantina] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
            };

            _permissionsByPropertyType[PropertyType.House] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.EditCategories,
            };

            _permissionsByPropertyType[PropertyType.Lab] = new List<PropertyPermissionType>
            {
                PropertyPermissionType.EditStructures,
                PropertyPermissionType.RetrieveStructures,
                PropertyPermissionType.RenameProperty,
                PropertyPermissionType.ChangeDescription,
                PropertyPermissionType.EnterProperty,
                PropertyPermissionType.ManageIncubators
            };
        }

        /// <summary>
        /// When the module loads, read all structure types and store them into the cache.
        /// </summary>
        private void CacheStructures()
        {
            var structureTypes = Enum.GetValues(typeof(StructureType)).Cast<StructureType>();
            foreach (var structure in structureTypes)
            {
                var detail = structure.GetAttribute<StructureType, StructureAttribute>();

                if (detail.IsActive)
                {
                    _activeStructures[structure] = detail;
                }
            }
        }

        /// <summary>
        /// When the module loads, iterate over all areas and cache any that are instance templates.
        /// </summary>
        private void CacheInstanceTemplates()
        {
            var templateResrefs = _activeLayouts
                .Select(x => x.Value.AreaInstanceResref)
                .ToList();

            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                var resref = GetResRef(area);
                if (templateResrefs.Contains(resref))
                    _instanceTemplates[resref] = area;
            }
        }

        /// <summary>
        /// When the module loads, link structures back to their property types.
        /// </summary>
        private void CacheStructuresByPropertyType()
        {
            foreach (var (structureType, detail) in _activeStructures)
            {
                if (detail.LayoutType == PropertyLayoutType.Invalid)
                    continue;

                var layout = GetLayoutByType(detail.LayoutType);
                if (!_structureTypesByPropertyType.ContainsKey(layout.PropertyType))
                    _structureTypesByPropertyType[layout.PropertyType] = new List<StructureType>();

                _structureTypesByPropertyType[layout.PropertyType].Add(structureType);
            }
        }

        /// <summary>
        /// Iterates over all areas to find the matching instance assigned to the specified resref.
        /// Then, the entrance waypoint is located and its coordinates are stored into cache.
        /// </summary>
        /// <param name="areaResref">The resref of the area to look for</param>
        /// <returns>X, Y, and Z coordinates of the entrance location</returns>
        private Vector4 GetEntrancePosition(string areaResref)
        {
            var area = _area.GetAreaByResref(areaResref);

            for (var obj = GetFirstObjectInArea(area); GetIsObjectValid(obj); obj = GetNextObjectInArea(area))
            {
                if (GetTag(obj) != "PROPERTY_ENTRANCE") continue;

                var position = GetPosition(obj);
                return new Vector4(position, GetFacing(obj));
            }

            return new Vector4();
        }

        /// <summary>
        /// Retrieves all area resrefs used by property instance templates.
        /// </summary>
        /// <returns>A list of area resrefs.</returns>
        public List<string> GetAllInstanceAreaResrefs()
        {
            var resrefs = new List<string>();
            foreach (var (type, detail) in _propertyTypes)
            {
                if (detail.SpawnType != PropertySpawnType.Instance)
                    continue;

                var layouts = _layoutsByPropertyType.ContainsKey(type)
                    ? _layoutsByPropertyType[type]
                    : new List<PropertyLayoutType>();

                foreach (var layoutType in layouts)
                {
                    var layout = _activeLayouts[layoutType];
                    if (!string.IsNullOrWhiteSpace(layout.AreaInstanceResref) &&
                        !resrefs.Contains(layout.AreaInstanceResref))
                    {
                        resrefs.Add(layout.AreaInstanceResref);
                    }
                }
            }

            return resrefs;
        }

        /// <summary>
        /// Retrieves a property layout by its type.
        /// </summary>
        /// <param name="type">The type of layout to retrieve.</param>
        /// <returns>A property layout</returns>
        public PropertyLayout GetLayoutByType(PropertyLayoutType type)
        {
            return _activeLayouts[type];
        }

        /// <summary>
        /// Retrieves a structure detail by its type.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public StructureAttribute GetStructureByType(StructureType structure)
        {
            return _activeStructures[structure];
        }

        /// <summary>
        /// Retrieves all layouts for a given property type.
        /// </summary>
        /// <param name="type">The type of property to search for</param>
        /// <returns>A list of layouts for the given property type.</returns>
        public List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type)
        {
            return _layoutsByPropertyType[type].ToList();
        }

        /// <summary>
        /// Retrieves the entrance point for a given layout.
        /// </summary>
        /// <param name="type">The layout type</param>
        /// <returns>The entrance position for the layout.</returns>
        public Vector4 GetEntrancePosition(PropertyLayoutType type)
        {
            return _entrancesByLayout[type];
        }

        /// <summary>
        /// Retrieves the template area by a given layout type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>The instance template area associated with this type.</returns>
        public uint GetInstanceTemplate(PropertyLayoutType type)
        {
            var layout = _activeLayouts[type];
            return _instanceTemplates[layout.AreaInstanceResref];
        }

        /// <summary>
        /// Retrieves the detail for a given permission type.
        /// </summary>
        /// <param name="permission">The type of permission to retrieve.</param>
        /// <returns>A permission detail</returns>
        public PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission)
        {
            return _activePermissions[permission];
        }

        /// <summary>
        /// Retrieves the list of available permissions for a given property type.
        /// </summary>
        /// <param name="type">The type of property</param>
        /// <returns>A list of available permissions</returns>
        public List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type)
        {
            return _permissionsByPropertyType[type];
        }

        /// <summary>
        /// Retrieves the property detail for a given type of property.
        /// </summary>
        /// <param name="type">The type of property to get.</param>
        /// <returns>A property detail for the given type.</returns>
        public PropertyTypeAttribute GetPropertyDetail(PropertyType type)
        {
            return _propertyTypes[type];
        }

        /// <summary>
        /// Retrieves a list of structures which have an interior with the specified property type.
        /// </summary>
        /// <param name="propertyType">The property type to search for.</param>
        /// <returns>A list of structure types.</returns>
        public List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType)
        {
            if (!_structureTypesByPropertyType.ContainsKey(propertyType))
                return new List<StructureType>();

            return _structureTypesByPropertyType[propertyType].ToList();
        }

        /// <summary>
        /// If a structure changed action is registered, this will perform the action on the specified
        /// property and placeable. If not registered, nothing will happen.
        /// </summary>
        /// <param name="structureType">The type of structure</param>
        /// <param name="changeType">The type of change.</param>
        /// <param name="property">The world property to target</param>
        /// <param name="placeable">The placeable to target</param>
        public void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable)
        {
            if (!_structureChangedActions.ContainsKey(structureType))
                return;

            if (!_structureChangedActions[structureType].ContainsKey(changeType))
                return;

            _structureChangedActions[structureType][changeType](property, placeable);
        }
    }
}
