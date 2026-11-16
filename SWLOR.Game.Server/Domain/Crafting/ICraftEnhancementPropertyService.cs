using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// The item properties an enhancement builds, and how a crafted property merges into an item or a list of properties. Static callers reach the container's instance through <c>GameServices.CraftEnhancementProperty</c>.
    /// </summary>
    public interface ICraftEnhancementPropertyService
    {
        /// <summary>
        /// Builds an item property for a given enhancement type.
        /// </summary>
        /// <param name="subTypeId">The sub type of the enhancement</param>
        /// <param name="amount">The amount to apply.</param>
        /// <returns></returns>
        IEnumerable<ItemProperty> BuildItemPropertiesForEnhancement(EnhancementSubType subTypeId, int amount, CombatDamageType damageType = CombatDamageType.Invalid);

        ItemProperty BuildItemPropertyForEnhancement(EnhancementSubType subTypeId, int amount);

        bool IsWeaponDamageEnhancement(EnhancementSubType subTypeId);

        bool TryGetWeaponDamageTypeForEnhancement(EnhancementSubType subTypeId, CombatDamageType explicitDamageType, out CombatDamageType damageType);

        /// <summary>
        /// Applies an enhancement-provided item property onto a crafted item,
        /// merging with any existing property of the same type. DMG amounts are
        /// summed together; a conflicting WeaponDamageType is resolved by randomly
        /// keeping one of the two rather than always favoring the latest one applied.
        /// </summary>
        void ApplyCraftedItemProperty(uint item, ItemProperty ip);

        /// <summary>
        /// Merges a crafted property into a list of properties by the rules <see cref="ApplyCraftedItemProperty"/> applies to an item:
        /// a property of the same type (and subtype, when the incoming one has one) is summed into one, a weapon keeps one damage type,
        /// and Force is never a weapon damage type. Working on a list lets a craft settle every property before any of them reaches
        /// the item, so the item never has a property removed and re-added: a removal is not committed to the item's saved property
        /// list until later, so a serialized form or copy taken in the same script would still carry the pre-merge property.
        /// </summary>
        void MergeCraftedItemProperty(List<ItemProperty> properties, ItemProperty ip);
    }
}
