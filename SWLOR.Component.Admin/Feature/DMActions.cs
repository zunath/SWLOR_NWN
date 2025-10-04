using Microsoft.Extensions.DependencyInjection;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Component.Admin.Feature
{
    public class DMActions
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventsPluginService _eventsPlugin;

        public DMActions(IDatabaseService db, IServiceProvider serviceProvider, IEventsPluginService eventsPlugin)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _eventsPlugin = eventsPlugin;
        }

        // Lazy-loaded service to break circular dependency
        private IGuiService GuiService => _serviceProvider.GetRequiredService<IGuiService>();
        
        public void OnDMSpawnObject()
        {
            OnDMSpawnObjectInstance();
        }

        public void OnDMSpawnObjectInstance()
        {
            var obj = StringToObject(_eventsPlugin.GetEventData("OBJECT"));
            var type = GetObjectType(obj);

            if (type == ObjectType.Creature)
            {
                for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                {
                    SetDroppableFlag(item, false);
                }
            }
        }

        public void GrantRPXPViaDMCommand()
        {
            GrantRPXPViaDMCommandInstance();
        }

        public void GrantRPXPViaDMCommandInstance()
        {
            var dm = OBJECT_SELF;
            var target = StringToObject(_eventsPlugin.GetEventData("OBJECT"));
            var amountStr = _eventsPlugin.GetEventData("AMOUNT");
            int.TryParse(amountStr, out var amount);

            // Skip the vanilla DM command
            _eventsPlugin.SkipEvent();

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
                GuiService.PublishRefreshEvent(target, new RPXPRefreshEvent());
            }
            else
            {
                SendMessageToPC(dm, "Only players may be targeted with this command.");
            }
        }
        public void DisableGiveLevel()
        {
            DisableGiveLevelInstance();
        }

        public void DisableGiveLevelInstance()
        {
            var dm = OBJECT_SELF;
            _eventsPlugin.SkipEvent();
            SendMessageToPC(dm, "The vanilla DM Give/Take Level command is disabled. Use /giverpxp <amount> instead to give RP XP.");
        }
    }
} 
