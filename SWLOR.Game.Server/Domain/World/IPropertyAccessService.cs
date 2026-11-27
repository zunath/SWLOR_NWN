using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// Who may use a property and getting into it: the permission checks, entering a property or a building, previewing a layout,
    /// and remembering where a player came from. Split from <c>PropertyService</c>; the members moved unchanged.
    /// </summary>
    public interface IPropertyAccessService
    {
        /// <summary>
        /// Retrieves a list of permissions associated with the item storage of a property for a given player.
        /// </summary>
        /// <param name="playerId">The player Id to search for</param>
        /// <param name="propertyId">The property Id to search for</param>
        /// <returns>A list of permissions</returns>
        List<WorldPropertyPermission> GetCategoryPermissions(string playerId, string propertyId);

        /// <summary>
        /// Determines whether a player has a specific permission.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player has permission, false otherwise</returns>
        bool HasPropertyPermission(uint player, string propertyId, PropertyPermissionType permission);

        /// <summary>
        /// Determines whether a player can GRANT a specific permission to another player.
        /// This will always return true for DMs.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="propertyId">The property Id to check.</param>
        /// <param name="permission">The type of permission to check.</param>
        /// <returns>true if player can grant the permission, false otherwise</returns>
        bool CanGrantPermission(uint player, string propertyId, PropertyPermissionType permission);

        /// <summary>
        /// Sends a player to the template area of a particular layout.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="layout">The layout type to send them to.</param>
        void PreviewProperty(uint player, PropertyLayoutType layout);

        /// <summary>
        /// Sends a player to a specific property's instance.
        /// </summary>
        /// <param name="player">The player to send.</param>
        /// <param name="propertyId">The property Id</param>
        void EnterProperty(uint player, string propertyId);

        /// <summary>
        /// Stores the original location of a player, before being ported into a property instance.
        /// </summary>
        /// <param name="player">The player whose location will be stored.</param>
        void StoreOriginalLocation(uint player);

        /// <summary>
        /// Jumps player to their original location, which is where they were before entering a property instance.
        /// This will also clear the temporary data related to the original location.
        /// </summary>
        /// <param name="player">The player who will jump.</param>
        void JumpToOriginalLocation(uint player);

        /// <summary>
        /// When a building entrance is used, port the player inside the instance if they have permission
        /// or display an error message saying they don't have permission to enter.
        /// </summary>
        void EnterBuilding();
    }
}
