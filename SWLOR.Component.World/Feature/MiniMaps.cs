using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Events.Events.Area;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Component.World.Feature
{
    public class MiniMaps
    {
        private const string AreaMiniMapVariable = "MINI_MAP_DISABLED";
        private readonly IServiceProvider _serviceProvider;
        
        // Lazy-loaded services to break circular dependencies
        private IKeyItemService KeyItemService => _serviceProvider.GetRequiredService<IKeyItemService>();

        public MiniMaps(
            IServiceProvider serviceProvider,
            IEventAggregator eventAggregator)
        {
            _serviceProvider = serviceProvider;

            // Subscribe to events
            eventAggregator.Subscribe<OnAreaEnter>(e => DisableMiniMap());
            eventAggregator.Subscribe<OnAreaExit>(e => EnableMiniMap());
            eventAggregator.Subscribe<OnModuleGuiEvent>(e => MiniMapGui());
        }

        /// <summary>
        /// If a player enters an area with a disabled mini-map and they do not have the map key item, disable the window.
        /// If a player enters an area with the associated map key item, fully explore it for them.
        /// </summary>
        public void DisableMiniMap()
        {
            var area = OBJECT_SELF;
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var isMiniMapDisabled = GetLocalInt(area, AreaMiniMapVariable);
            if (isMiniMapDisabled == 1)
            {
                SetGuiPanelDisabled(player, GuiPanelType.Minimap, true);
            }

            var keyItemId = GetLocalInt(area, "MAP_KEY_ITEM_ID");
            if (keyItemId > 0)
            {
                var keyItemType = (KeyItemType)keyItemId;
                var hasKeyItem = KeyItemService.HasKeyItem(player, keyItemType);

                if (hasKeyItem)
                {
                    SetGuiPanelDisabled(player, GuiPanelType.Minimap, false);
                    ExploreAreaForPlayer(area, player);
                }
            }
        }

        /// <summary>
        /// Ensures the mini-map is always re-enabled when leaving an area.
        /// </summary>
        public void EnableMiniMap()
        {
            var player = GetExitingObject();
            if (!GetIsPC(player)) return;

            SetGuiPanelDisabled(player, GuiPanelType.Minimap, false);
        }

        /// <summary>
        /// Skips the character sheet panel open event and shows the SWLOR character sheet instead.
        /// </summary>
        public void MiniMapGui()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            var panelType = (GuiPanelType)GetLastGuiEventInteger();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (type != GuiEventType.DisabledPanelAttemptOpen) return;
            if (panelType != GuiPanelType.Minimap) return;

            var area = GetArea(player);
            if (GetLocalInt(area, AreaMiniMapVariable) != 1) return;

            SendMessageToPC(player, ColorToken.Red("You do not have a map of this area."));
        }
    }
}
