using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyPermissionType
    {
        [PropertyPermission("Invalid", "Invalid", false)]
        Invalid = 0,

        // Categories
        [PropertyPermission("Edit Categories", "Can create or delete item categories.", true)]
        EditCategories = 1,

        // Apartments, Buildings, Starships
        [PropertyPermission("Edit Structures", "Can place structures and edit their details.", true)]
        EditStructures = 2,

        // Apartments, Buildings, Starships
        [PropertyPermission("Retrieve Structures", "Can pick up structures that have already been placed.", true)]
        RetrieveStructures = 3,

        // Apartments, Buildings, Starships
        [PropertyPermission("Rename Property", "Can rename the property.", true)]
        RenameProperty = 4,

        // Apartments, Buildings, Starships
        [PropertyPermission("Access Storage", "Can access item storage.", true)]
        AccessStorage = 5,

        // Apartments
        [PropertyPermission("Extend Lease", "Can pay for lease extensions.", true)]
        ExtendLease = 6,

        // Apartments
        [PropertyPermission("Cancel Lease", "Can cancel a lease.", true)]
        CancelLease = 7,

        // Apartments, Buildings, Starships
        [PropertyPermission("Enter Property", "Can enter the property.", true)]
        EnterProperty = 8, 

        // Starships
        [PropertyPermission("Pilot Ship", "Can pilot the starship.", true)]
        PilotShip = 9,

        // Apartments, Buildings, Starships
        [PropertyPermission("Change Description", "Can change a property's description", true)]
        ChangeDescription = 10,

        // Starships
        [PropertyPermission("Refit Ship", "Can refit the ship.", true)]
        RefitShip = 11,

        // City
        [PropertyPermission("Edit Taxes", "Can edit taxes for the entire city.", true)]
        EditTaxes = 12,

        // City
        [PropertyPermission("Access Treasury", "Can withdraw/deposit credits into the city's treasury.", true)]
        AccessTreasury = 13,

        // City
        [PropertyPermission("Manage Upgrades", "Can purchase city upgrades with treasury money.", true)]
        ManageUpgrades = 14,

        // City
        [PropertyPermission("Manage Upkeep", "Can pay maintenance fees.", true)]
        ManageUpkeep = 15,

        // Labs
        [PropertyPermission("Manage Incubators", "Can manage incubators.", true)]
        ManageIncubators = 16,
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
