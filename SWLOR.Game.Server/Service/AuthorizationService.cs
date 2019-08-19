using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.ValueObject;
using static NWN._;


namespace SWLOR.Game.Server.Service
{
    public static class AuthorizationService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(OnModuleEnter);
        }

        private static void OnModuleEnter(OnModuleEnter @event)
        {
            NWPlayer dm = GetEnteringObject();
            if (!dm.IsDM) return;

            var cdKey = GetPCPublicCDKey(dm);
            var entity = DataService.AuthorizedDM.GetByCDKeyAndActiveOrDefault(cdKey);
            if (entity == null || !entity.IsActive)
            {
                LogDMAuthorization(dm, false);
                BootPC(dm, "You are not authorized to log in as a DM. Please contact the server administrator if this is incorrect.");
                return;
            }

            LogDMAuthorization(dm, true);
        }

        public static bool IsPCRegisteredAsDM(NWPlayer player)
        {
            if (player.IsDM) return true;
            if (!player.IsPlayer) return false;

            string cdKey = GetPCPublicCDKey(player.Object);

            AuthorizedDM entity = DataService.AuthorizedDM.GetByCDKeyAndActiveOrDefault(cdKey);
            return entity != null;
        }

        private static void LogDMAuthorization(NWPlayer dm, bool isAuthorizationSuccessful)
        {
            var account = GetPCPlayerName(dm);
            var cdKey = GetPCPublicCDKey(dm);
            var now = DateTime.UtcNow;
            var eventType = isAuthorizationSuccessful ? 13 : 14;

            ModuleEvent entity = new ModuleEvent
            {
                AccountName = account,
                CDKey = cdKey,
                ModuleEventTypeID = eventType,
                PlayerID = null,
                DateOfEvent = now
            };

            // Bypass the caching logic.
            DataService.DataQueue.Enqueue(new DatabaseAction(entity, DatabaseActionType.Insert));
        }
    }
}
