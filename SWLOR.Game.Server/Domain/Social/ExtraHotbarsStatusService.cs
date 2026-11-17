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
    /// Tells the open bars to redraw what their slots show (item counts, active modes, equipped sets) when the player's items change.
    /// A part of the ExtraHotbars service.
    /// </summary>
    public class ExtraHotbarsStatusService : IExtraHotbarsStatusService
    {
        private readonly IGuiService _gui;

        public ExtraHotbarsStatusService(
            IGuiService gui)
        {
            _gui = gui;
        }

        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public void RefreshOnAcquire()
        {
            RefreshStatus(GetModuleItemAcquiredBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public void RefreshOnUnacquire()
        {
            RefreshStatus(GetModuleItemLostBy());
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public void RefreshOnEquip()
        {
            RefreshStatus(GetPCItemLastEquippedBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public void RefreshOnUnequip()
        {
            RefreshStatus(GetPCItemLastUnequippedBy());
        }

        // Scripted item uses lower stack sizes and attacks drop stealth without an event, so slots also catch up each heartbeat.
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public void RefreshOnHeartbeat()
        {
            RefreshStatus(OBJECT_SELF);
        }

        public void RefreshStatusSoon(uint player)
        {
            DelayCommand(1f, () => RefreshStatus(player));
        }

        private void RefreshStatus(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            _gui.PublishRefreshEvent(player, new ExtraHotbarStatusRefreshEvent());
        }
    }
}
