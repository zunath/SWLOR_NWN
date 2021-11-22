using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyPermissionType
    {
        [PropertyPermission("Invalid", "Invalid", false)]
        Invalid = 0,

        // Categories
        [PropertyPermission("Edit Storage Categories", "Toggles the ability to create or delete storage categories.", true)]
        EditStorageCategories = 1,

        // Apartments, Buildings, Starships
        [PropertyPermission("Edit Structures", "Toggles the ability to place structures and edit their details.", true)]
        EditStructures = 2,

        // Apartments, Buildings, Starships
        [PropertyPermission("Retrieve Structures", "Toggles the ability to pick up structures that have already been placed.", true)]
        RetrieveStructures = 3,

        // Apartments, Buildings, Starships
        [PropertyPermission("Rename Property", "Toggles the ability to rename the property.", true)]
        RenameProperty = 4,

        // Apartments, Buildings, Starships
        [PropertyPermission("Access Storage", "Toggles the ability to access item storage.", true)]
        AccessStorage = 5,

        // Apartments
        [PropertyPermission("Extend Lease", "Toggles the ability to pay for lease extensions.", true)]
        ExtendLease = 6,

        // Apartments
        [PropertyPermission("Cancel Lease", "Toggles the ability to cancel a lease.", true)]
        CancelLease = 7,

        // Apartments, Buildings, Starships
        [PropertyPermission("Enter Property", "Toggles the ability to enter the property.", true)]
        EnterProperty = 8,

        // Apartments, Buildings, Starships
        [PropertyPermission("Change Description", "Toggles the ability to change a property's description", true)]
        ChangeDescription = 10,
    }

    public class PropertyPermissionAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }

        public PropertyPermissionAttribute(string name, string description, bool isActive)
        {
            Name = name;
            Description = description;
            IsActive = isActive;
        }
    }
}
