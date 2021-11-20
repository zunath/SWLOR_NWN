using System;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public enum PropertyPermissionType
    {
        [PropertyPermission("Invalid", "Invalid", false)]
        Invalid = 0,
        [PropertyPermission("Adjust Permissions", "This player can adjust the permissions of other players for this property.", true)]
        AdjustPermissions = 1,
        [PropertyPermission("Edit Structures", "Toggles the ability to place structures and edit their details.", true)]
        EditStructures = 2,
        [PropertyPermission("Retrieve Structures", "Toggles the ability to pick up structures that have already been placed.", true)]
        RetrieveStructures = 3,
        [PropertyPermission("Rename Property", "Toggles the ability to rename the property.", true)]
        RenameProperty = 4,
        [PropertyPermission("Access Storage", "Toggles the ability to access item storage.", true)]
        AccessStorage = 5,
        [PropertyPermission("Extend Lease", "Toggles the ability to pay for lease extensions.", true)]
        ExtendLease = 6,
        [PropertyPermission("Cancel Lease", "Toggles the ability to cancel a lease.", true)]
        CancelLease = 7,
        [PropertyPermission("Enter Property", "Toggles the ability to enter the property.", true)]
        EnterProperty = 8,

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
