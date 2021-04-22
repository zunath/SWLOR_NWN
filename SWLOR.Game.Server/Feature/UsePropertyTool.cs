using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using Dialog = SWLOR.Game.Server.Service.Dialog;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class UsePropertyTool
    {
        /// <summary>
        /// When the property tool feat is used, ensure the player is in a property they own or have permission to access.
        /// If they do, open the menu.
        /// </summary>
        [NWNEventHandler("feat_use_bef")]
        public static void PropertyTool()
        {
            var feat = (FeatType)Convert.ToInt32(Events.GetEventData("FEAT_ID"));

            if (feat != FeatType.StructureTool) return;

            var player = OBJECT_SELF;
            var playerId = GetObjectUUID(player);
            var area = GetArea(player);
            var ownerPlayerUUID = GetLocalString(area, "HOUSING_OWNER_PLAYER_UUID");

            // Not in a property.
            if (string.IsNullOrWhiteSpace(ownerPlayerUUID))
            {
                SendMessageToPC(player, "The property tool may only be used when you are inside a player-owned property.");
                return;
            }

            // Check if the player has any menu-based permissions.
            var dbHouse = DB.Get<PlayerHouse>(ownerPlayerUUID);
            var permission = dbHouse.PlayerPermissions.ContainsKey(playerId)
                ? dbHouse.PlayerPermissions[playerId]
                : new PlayerHousePermission();

            if (!permission.CanPlaceFurniture &&
                !permission.CanAdjustPermissions)
            {
                SendMessageToPC(player, "You do not have permission to access this property's options.");
                return;
            }

            // We have access. Set the target object and location, which will be picked up by the menu.
            var target = StringToObject(Events.GetEventData("TARGET_OBJECT_ID"));
            var targetPositionX = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_X"));
            var targetPositionY = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Y"));
            var targetPositionZ = (float)Convert.ToDouble(Events.GetEventData("TARGET_POSITION_Z"));

            var location = Location(area, Vector3(targetPositionX, targetPositionY, targetPositionZ), 0.0f);

            SetLocalObject(player, "TEMP_PROPERTY_TOOL_OBJECT", target);
            SetLocalLocation(player, "TEMP_PROPERTY_TOOL_LOCATION", location);

            // Open the menu.
            Dialog.StartConversation(player, player, nameof(PlayerHousePropertyDialog));
        }
    }
}
