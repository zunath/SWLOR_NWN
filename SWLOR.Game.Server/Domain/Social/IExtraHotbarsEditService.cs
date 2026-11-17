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
    /// What a player puts on a bar. A part of the ExtraHotbars service.
    /// </summary>
    public interface IExtraHotbarsEditService
    {
        /// <summary>
        /// Changes one bar's layout settings and redraws it.
        /// </summary>
        void UpdateBar(uint player, int barIndex, System.Action<ExtraHotbar> change);

        void AssignAbility(uint player, int barIndex, int slotIndex, FeatType feat);

        void AssignItem(uint player, int barIndex, int slotIndex, uint item);

        /// <summary>
        /// Puts a set of hand items on a slot. Either hand may be left out, which leaves that hand alone when used.
        /// </summary>
        void AssignEquipment(uint player, int barIndex, int slotIndex, uint mainHand, uint offHand);

        void AssignChatCommand(uint player, int barIndex, int slotIndex, string command);

        void AssignActionMode(uint player, int barIndex, int slotIndex, ActionMode mode);

        void ClearSlot(uint player, int barIndex, int slotIndex);
    }
}
