using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Extra click-to-use hotbars shown beside the native quickbar: running a slot, and the abilities, combat modes and items a slot can hold.
    /// </summary>
    public interface IExtraHotbarsService
    {
        /// <summary>
        /// Runs whatever the slot holds.
        /// </summary>
        void UseSlot(uint player, ExtraHotbarSlot slot);

        /// <summary>
        /// The abilities a player can put on a bar: every registered active ability they know.
        /// </summary>
        List<FeatType> GetKnownAbilities(uint player);

        string GetAbilityIcon(FeatType feat);

        /// <summary>
        /// The combat modes the player can turn on. Detect is open to everyone and Stealth needs the Stealth perk;
        /// every other native mode needs a feat players cannot get, and Parry has no effect in SWLOR combat.
        /// </summary>
        List<ActionMode> GetAvailableActionModes(uint player);

        string GetActionModeName(ActionMode mode);

        string GetActionModeIcon(ActionMode mode);

        /// <summary>
        /// Usable items in the player's inventory, one per resref.
        /// </summary>
        List<uint> GetUsableItems(uint player);

        uint FindItem(uint player, string resref);

        int CountItems(uint player, string resref);
    }
}
