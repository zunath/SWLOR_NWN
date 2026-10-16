using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public class DMActions
    {
        [NWNEventHandler(ScriptName.OnDMSpawnObjectAfter)]
        public static void OnDMSpawnObject()
        {
            var obj = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            var type = GetObjectType(obj);
            var objectType = InternalObjectType.Invalid;
            if (int.TryParse(EventsPlugin.GetEventData("OBJECT_TYPE"), out var rawObjectType))
                objectType = (InternalObjectType)rawObjectType;

            if (type == ObjectType.Creature)
            {
                MakeInventoryUndroppable(obj);
            }

            // Track DM-spawned placeables so the DM tools NUI can list non-native objects.
            if (type == ObjectType.Placeable && objectType == InternalObjectType.Placeable)
            {
                var dm = OBJECT_SELF;
                if (GetIsDMPossessed(dm))
                    dm = GetMaster(dm);

                MarkDMSpawnedPlaceable(dm, obj);
            }
        }

        /// <summary>
        /// Keeps a DM-spawned creature's inventory from dropping as loot.
        /// </summary>
        /// <param name="creature">The spawned creature.</param>
        public static void MakeInventoryUndroppable(uint creature)
        {
            for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            {
                SetDroppableFlag(item, false);
            }
        }

        /// <summary>
        /// Records which DM spawned a placeable so the DM tools window can list it.
        /// </summary>
        /// <param name="dm">The DM who spawned the placeable.</param>
        /// <param name="placeable">The spawned placeable.</param>
        public static void MarkDMSpawnedPlaceable(uint dm, uint placeable)
        {
            SetLocalInt(placeable, "DM_SPAWNED_PLACEABLE", 1);
            SetLocalString(placeable, "DM_SPAWNED_BY", GetObjectUUID(dm));
            SetLocalString(placeable, "DM_SPAWNED_BY_NAME", GetName(dm));
        }

        [NWNEventHandler(ScriptName.OnDMGiveXPBefore)]
        public static void GrantRPXPViaDMCommand()
        {
            var dm = OBJECT_SELF;
            var target = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            var amountStr = EventsPlugin.GetEventData("AMOUNT");
            int.TryParse(amountStr, out var amount);

            // Skip the vanilla DM command
            EventsPlugin.SkipEvent();

            if (amount < 0)
            {
                SendMessageToPC(dm, "The vanilla DM Take XP command is disabled. This command should not be used in SWLOR.");
                return;
            }

            // Give RP XP only to players
            if (GetIsPC(target) && !GetIsDM(target))
            {
                var playerId = GetObjectUUID(target);
                var dbPlayer = DB.Get<Player>(playerId);
                dbPlayer.UnallocatedXP += amount;
                DB.Set(dbPlayer);
                SendMessageToPC(target, $"A DM has awarded you with {amount} roleplay XP.");
                SendMessageToPC(dm, $"You award {GetName(target)} with {amount} roleplay XP.");
                Gui.PublishRefreshEvent(target, new RPXPRefreshEvent());
            }
            else
            {
                SendMessageToPC(dm, "Only players may be targeted with this command.");
            }
        }
        [NWNEventHandler(ScriptName.OnDMGiveLevelBefore)]
        public static void DisableGiveLevel()
        {
            var dm = OBJECT_SELF;
            EventsPlugin.SkipEvent();
            SendMessageToPC(dm, "The vanilla DM Give/Take Level command is disabled. Use /giverpxp <amount> instead to give RP XP.");
        }

        [NWNEventHandler(ScriptName.OnDMJumpAllPlayersToPointBefore)]
        public static void DisableTeleportAll()
        {
            var dm = OBJECT_SELF;
            EventsPlugin.SkipEvent();
            SendMessageToPC(dm, "The Teleport All command is permanently disabled and cannot be used.");
        }
    }
}
