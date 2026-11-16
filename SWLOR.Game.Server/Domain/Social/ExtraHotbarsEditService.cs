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
    /// What a player puts on a bar: the bar settings and the contents of each slot. A part of the ExtraHotbars service.
    /// </summary>
    public class ExtraHotbarsEditService : IExtraHotbarsEditService
    {
        private readonly IDBService _db;
        private readonly IGuiService _gui;
        private readonly IExtraHotbarsWindowService _window;
        private readonly IQuickCastService _quickCast;

        public ExtraHotbarsEditService(
            IDBService db,
            IGuiService gui,
            IExtraHotbarsWindowService window,
            IQuickCastService quickCast)
        {
            _db = db;
            _gui = gui;
            _window = window;
            _quickCast = quickCast;
        }

        /// <summary>
        /// Changes one bar's layout settings and redraws it.
        /// </summary>
        public void UpdateBar(uint player, int barIndex, System.Action<ExtraHotbar> change)
        {
            var dbPlayer = _window.GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            var bar = _window.GetBars(dbPlayer)[barIndex];
            change(bar);
            bar.SlotCount = System.Math.Clamp(bar.SlotCount, 1, ExtraHotbarsWindowService.MaxSlots);
            _db.Set(dbPlayer);

            _window.ApplyVisibility(player);
            _gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
        }

        public void AssignAbility(uint player, int barIndex, int slotIndex, FeatType feat)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Ability,
                Feat = _quickCast.GetOriginalFeat(feat)
            });
        }

        public void AssignItem(uint player, int barIndex, int slotIndex, uint item)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Item,
                ItemResref = GetResRef(item),
                ItemName = GetName(item)
            });
        }

        /// <summary>
        /// Puts a set of hand items on a slot. Either hand may be left out, which leaves that hand alone when used.
        /// </summary>
        public void AssignEquipment(uint player, int barIndex, int slotIndex, uint mainHand, uint offHand)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Equipment,
                MainHand = ToEquipItem(mainHand),
                OffHand = ToEquipItem(offHand)
            });
        }

        private ExtraHotbarEquipItem ToEquipItem(uint item)
        {
            if (!GetIsObjectValid(item))
                return null;

            return new ExtraHotbarEquipItem
            {
                ItemId = GetObjectUUID(item),
                ItemResref = GetResRef(item),
                ItemName = GetName(item)
            };
        }

        public void AssignChatCommand(uint player, int barIndex, int slotIndex, string command)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Command,
                Command = command.Trim()
            });
        }

        public void AssignActionMode(uint player, int barIndex, int slotIndex, ActionMode mode)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.ActionMode,
                ActionMode = mode
            });
        }

        public void ClearSlot(uint player, int barIndex, int slotIndex)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot());
        }

        private void SetSlot(uint player, int barIndex, int slotIndex, ExtraHotbarSlot slot)
        {
            var dbPlayer = _window.GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            _window.GetBars(dbPlayer)[barIndex].Slots[slotIndex] = slot;
            _db.Set(dbPlayer);
            _gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
        }
    }
}
