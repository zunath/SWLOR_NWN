using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Sets and resets the colours of an item's materials. Static callers reach the container's instance through
    /// <c>GameServices.TintMapItemColor</c>.
    /// </summary>
    public interface ITintMapItemColorService
    {
        /// <summary>Stores a custom colour for one material of one layer and applies it.</summary>
        void SetColor(uint creature, TintMapMaterialSelection selection, TintMapLayerType layer, TintMapColor color);

        /// <summary>
        /// Stores a saved colour value for one material of one layer and applies it. A queued colour carry
        /// calls it without invalidating the pending carries.
        /// </summary>
        /// <param name="invalidatePendingCarry">True when the edit is newer intent than a pending carry.</param>
        void SetStoredColor(uint creature, TintMapMaterialSelection selection, TintMapLayerType layer, int savedColor,
            bool invalidatePendingCarry);

        /// <summary>Resets one material to its palette colour, or to the given palette colour.</summary>
        void ResetColor(uint creature, TintMapMaterialSelection selection, TintMapLayerType layer,
            int? paletteColor = null);

        /// <summary>
        /// Removes a part-specific material override so the material can inherit an active item-wide custom
        /// color. This is distinct from selecting a preset, which deliberately stores a compatibility palette
        /// value beneath an active global tint.
        /// </summary>
        void ResetColorToInheritance(uint creature, TintMapMaterialSelection selection, TintMapLayerType layer);

        /// <summary>Stores one custom colour for a layer across every material of an item.</summary>
        void SetGlobalItemCustomColor(uint creature, IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer, TintMapColor color, uint item = OBJECT_INVALID);

        /// <summary>Removes an item-wide custom colour from a layer.</summary>
        void ResetGlobalItemCustomColor(uint creature, IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer);

        /// <summary>
        /// Clears persisted tint state when the selected armor part no longer exposes the chosen channel. There
        /// is no material selection for the normal reset path in that case, but the user's preset click still
        /// replaces the old custom color.
        /// </summary>
        void ResetInactiveItemCustomColor(uint creature, uint item, TintMapLayerType layer, AppearanceArmor armorPart);
    }
}
