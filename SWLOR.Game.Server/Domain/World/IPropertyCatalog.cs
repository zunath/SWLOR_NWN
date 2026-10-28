using System.Collections.Generic;
using System.Numerics;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The definitions of the property system: the property types, layouts, entrances, permissions, structures and
    /// instance templates the module cache builds, and the lookups over them.
    /// </summary>
    public interface IPropertyCatalog
    {
        /// <summary>
        /// When the module loads, cache all relevant data into memory.
        /// </summary>
        void CacheData();

        /// <summary>
        /// Every property type with its detail, in the order the types were cached.
        /// </summary>
        IReadOnlyDictionary<PropertyType, PropertyTypeAttribute> PropertyTypes { get; }

        /// <summary>
        /// The permissions available to each property type, in the order the types were cached.
        /// </summary>
        IReadOnlyDictionary<PropertyType, List<PropertyPermissionType>> PermissionsByPropertyType { get; }

        /// <summary>
        /// Retrieves a property layout by its type.
        /// </summary>
        /// <param name="type">The type of layout to retrieve.</param>
        /// <returns>A property layout</returns>
        PropertyLayout GetLayoutByType(PropertyLayoutType type);

        /// <summary>
        /// Retrieves a structure detail by its type.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        StructureAttribute GetStructureByType(StructureType structure);

        /// <summary>
        /// Retrieves all layouts for a given property type.
        /// </summary>
        /// <param name="type">The type of property to search for</param>
        /// <returns>A list of layouts for the given property type.</returns>
        List<PropertyLayoutType> GetAllLayoutsByPropertyType(PropertyType type);

        /// <summary>
        /// Retrieves the entrance point for a given layout.
        /// </summary>
        /// <param name="type">The layout type</param>
        /// <returns>The entrance position for the layout.</returns>
        Vector4 GetEntrancePosition(PropertyLayoutType type);

        /// <summary>
        /// Retrieves the template area by a given layout type.
        /// </summary>
        /// <param name="type">The type to search for.</param>
        /// <returns>The instance template area associated with this type.</returns>
        uint GetInstanceTemplate(PropertyLayoutType type);

        /// <summary>
        /// Retrieves the detail for a given permission type.
        /// </summary>
        /// <param name="permission">The type of permission to retrieve.</param>
        /// <returns>A permission detail</returns>
        PropertyPermissionAttribute GetPermissionByType(PropertyPermissionType permission);

        /// <summary>
        /// Retrieves the list of available permissions for a given property type.
        /// </summary>
        /// <param name="type">The type of property</param>
        /// <returns>A list of available permissions</returns>
        List<PropertyPermissionType> GetPermissionsByPropertyType(PropertyType type);

        /// <summary>
        /// Retrieves the property detail for a given type of property.
        /// </summary>
        /// <param name="type">The type of property to get.</param>
        /// <returns>A property detail for the given type.</returns>
        PropertyTypeAttribute GetPropertyDetail(PropertyType type);

        /// <summary>
        /// Retrieves a list of structures which have an interior with the specified property type.
        /// </summary>
        /// <param name="propertyType">The property type to search for.</param>
        /// <returns>A list of structure types.</returns>
        List<StructureType> GetStructuresByInteriorPropertyType(PropertyType propertyType);

        /// <summary>
        /// Retrieves all area resrefs used by property instance templates.
        /// </summary>
        /// <returns>A list of area resrefs.</returns>
        List<string> GetAllInstanceAreaResrefs();

        /// <summary>
        /// If a structure changed action is registered, this will perform the action on the specified
        /// property and placeable. If not registered, nothing will happen.
        /// </summary>
        /// <param name="structureType">The type of structure</param>
        /// <param name="changeType">The type of change.</param>
        /// <param name="property">The world property to target</param>
        /// <param name="placeable">The placeable to target</param>
        void RunStructureChangedEvent(StructureType structureType, StructureChangeType changeType, WorldProperty property, uint placeable);
    }
}
