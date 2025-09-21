using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.NWNX;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Game.Server.Feature
{
    public class DMActions
    {
        private readonly IDatabaseService _db;
        private readonly IGuiService _guiService;

        public DMActions(IDatabaseService db, IGuiService guiService)
        {
            _db = db;
            _guiService = guiService;
        }
        
        [ScriptHandler<OnDMSpawnObjectAfter>]
        public void OnDMSpawnObject()
        {
            OnDMSpawnObjectInstance();
        }

        public void OnDMSpawnObjectInstance()
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

        [ScriptHandler<OnDMGiveXPBefore>]
        public void GrantRPXPViaDMCommand()
        {
            GrantRPXPViaDMCommandInstance();
        }

        public void GrantRPXPViaDMCommandInstance()
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
                var dbPlayer = _db.Get<Player>(playerId);
                dbPlayer.UnallocatedXP += amount;
                _db.Set(dbPlayer);
                SendMessageToPC(target, $"A DM has awarded you with {amount} roleplay XP.");
                SendMessageToPC(dm, $"You award {GetName(target)} with {amount} roleplay XP.");
                _guiService.PublishRefreshEvent(target, new RPXPRefreshEvent());
            }
            else
            {
                SendMessageToPC(dm, "Only players may be targeted with this command.");
            }
        }
        [ScriptHandler<OnDMGiveLevelBefore>]
        public void DisableGiveLevel()
        {
            DisableGiveLevelInstance();
        }

        public void DisableGiveLevelInstance()
        {
            var dm = OBJECT_SELF;
            EventsPlugin.SkipEvent();
            SendMessageToPC(dm, "The vanilla DM Give/Take Level command is disabled. Use /giverpxp <amount> instead to give RP XP.");
        }
    }
} 
