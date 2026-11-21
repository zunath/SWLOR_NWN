using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The property terminals and menus: the apartment, citizenship and city management terminals and the property menu feat,
    /// which open their windows. Split from <c>PropertyService</c>; the members moved unchanged.
    /// </summary>
    public class PropertyTerminalService : IPropertyTerminalService
    {
        private readonly IDBService _db;
        private readonly IGuiService _gui;
        private readonly IPropertyLoadRegistryService _registry;

        public PropertyTerminalService(
            IDBService db,
            IGuiService gui,
            IPropertyLoadRegistryService registry)
        {
            _db = db;
            _gui = gui;
            _registry = registry;
        }

        /// <summary>
        /// When an apartment terminal is used, open the Apartment NUI
        /// </summary>
        [NWNEventHandler(ScriptName.OnApartmentTerminal)]
        public void StartApartmentConversation()
        {
            var player = GetLastUsedBy();
            var terminal = OBJECT_SELF;

            TogglePlayerWindow(player, GuiWindowType.ManageApartment, null, terminal);
        }

        /// <summary>
        /// When the property menu feat is used, open the GUI window.
        /// </summary>
        [NWNEventHandler(ScriptName.OnFeatUseBefore)]
        public void PropertyMenu()
        {
            var feat = (FeatType)Convert.ToInt32(EventsPlugin.GetEventData("FEAT_ID"));

            if (feat != FeatType.PropertyMenu) return;

            var player = OBJECT_SELF;

            if (_gui.IsWindowOpen(player, GuiWindowType.ManageStructures))
            {
                TogglePlayerWindow(player, GuiWindowType.ManageStructures);
                return;
            }

            var area = GetArea(player);
            var propertyId = _registry.GetPropertyId(area);

            if (string.IsNullOrWhiteSpace(propertyId))
            {
                FloatingTextStringOnCreature($"This menu can only be accessed within player properties.", player, false);
                return;
            }

            var playerId = GetObjectUUID(player);
            var permissionQuery = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var permission = _db.Search(permissionQuery).FirstOrDefault();

            var categoryQuery = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var categoryIds = _db.Search(categoryQuery).Select(s => s.Id).ToList();
            long categoryPermissionCount = 0;

            if (categoryIds.Count > 0)
            {
                permissionQuery = new DBQuery<WorldPropertyPermission>()
                    .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false)
                    .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), categoryIds);
                categoryPermissionCount = _db.SearchCount(permissionQuery);
            }

            // Player can access this menu only if they have permission to manipulate structures,
            // retrieve structures, or access the property's item storage.
            if (permission == null ||
                !permission.Permissions[PropertyPermissionType.RetrieveStructures] &&
                !permission.Permissions[PropertyPermissionType.EditStructures] &&
                categoryPermissionCount <= 0)
            {
                FloatingTextStringOnCreature($"You do not have permission to access this property.", player, false);
                return;
            }

            TogglePlayerWindow(player, GuiWindowType.ManageStructures);
        }

        /// <summary>
        /// When the Citizenship terminal is used, open the Manage Citizenship UI.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenCitizenship)]
        public void OpenCitizenshipMenu()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, "Only players may use this terminal.");
                return;
            }

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = _registry.GetPropertyId(area);
            var dbProperty = _db.Get<WorldProperty>(propertyId);
            var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);

            // Player is a citizen of another city. Their citizenship needs to be revoked before
            // they're able to access this terminal.
            if (!string.IsNullOrWhiteSpace(dbPlayer.CitizenPropertyId) &&
                dbPlayer.CitizenPropertyId != Guid.Empty.ToString() &&
                dbBuilding.ParentPropertyId != dbPlayer.CitizenPropertyId)
            {
                SendMessageToPC(player, ColorToken.Red("You are a citizen of another city. You cannot access this terminal unless you revoke your citizenship first."));
            }
            else
            {
                TogglePlayerWindow(player, GuiWindowType.ManageCitizenship, null, terminal);
            }
        }

        /// <summary>
        /// When the City Management terminal is used, open the City Management UI.
        /// </summary>
        [NWNEventHandler(ScriptName.OnOpenCityManage)]
        public void OpenCityManagementMenu()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var terminal = OBJECT_SELF;
            var area = GetArea(terminal);
            var propertyId = _registry.GetPropertyId(area);
            var dbProperty = _db.Get<WorldProperty>(propertyId);
            var dbBuilding = _db.Get<WorldProperty>(dbProperty.ParentPropertyId);

            var permission = _db.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), dbBuilding.ParentPropertyId, false)
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId), playerId, false))
                .SingleOrDefault();

            // Player has at least one permission. Display the window.
            if (permission != null && (permission.Permissions[PropertyPermissionType.RenameProperty] ||
                                       permission.Permissions[PropertyPermissionType.EditTaxes] ||
                                       permission.Permissions[PropertyPermissionType.AccessTreasury] ||
                                       permission.Permissions[PropertyPermissionType.ManageUpgrades] ||
                                       permission.Permissions[PropertyPermissionType.ManageUpkeep]))
            {
                TogglePlayerWindow(player, GuiWindowType.ManageCity, null, terminal);
            }
            else
            {
                SendMessageToPC(player, ColorToken.Red("You do not have permission to access this terminal."));
            }
        }

        /// <summary>
        /// Shows or hides a window for a player. The window table of <c>Gui</c> is filled by the module load, so a unit
        /// test replaces this.
        /// </summary>
        /// <param name="player">The player to show the window to.</param>
        /// <param name="type">The type of window.</param>
        /// <param name="payload">An optional payload for the window's view model.</param>
        /// <param name="tetherObject">The object the window is tethered to.</param>
        protected virtual void TogglePlayerWindow(uint player, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = OBJECT_INVALID)
        {
            _gui.TogglePlayerWindow(player, type, payload, tetherObject);
        }
    }
}
