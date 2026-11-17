using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Sets, resets and carries the colours of a creature's own layers (skin, hair, tattoos) and keeps a
    /// player's colours in the player record. Static callers reach the container's instance through
    /// <c>GameServices.TintMapCreatureColor</c>.
    /// </summary>
    public interface ITintMapCreatureColorService
    {
        /// <summary>Reapplies the creature-wide colours stored on the creature to every material it shows.</summary>
        void CarryStoredCreatureCustomColors(uint creature);

        /// <summary>Removes the custom colour of one creature layer.</summary>
        void ResetCreatureCustomColor(uint creature, TintMapLayerType layer);

        /// <summary>Stores a custom colour for one creature layer and refreshes the creature.</summary>
        void SetCreatureCustomColor(uint creature, IReadOnlyList<TintMapMaterialSelection> selections,
            TintMapLayerType layer, TintMapColor color);

        /// <summary>
        /// Restores the creature colours saved in a player's record onto the entering player, replacing stale
        /// locals from an older character export.
        /// </summary>
        void RestorePlayerOverrides(uint creature);
    }
}
