using SWLOR.Game.Server.Core;
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

            if (type == ObjectType.Creature)
            {
                for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                {
                    SetDroppableFlag(item, false);
                }
            }
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
    }
} 