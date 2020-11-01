using System;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;


namespace SWLOR.Game.Server.Legacy.Service
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

        public static DMAuthorizationType GetDMAuthorizationType(NWPlayer player)
        {
            if (!player.IsPlayer && !player.IsDM) return DMAuthorizationType.None;

            var cdKey = GetPCPublicCDKey(player);

            var entity = DataService.AuthorizedDM.GetByCDKeyAndActiveOrDefault(cdKey);
            if (entity == null) return DMAuthorizationType.None;

            if (entity.DMRole == 1)
                return DMAuthorizationType.DM;
            else if (entity.DMRole == 2)
                return DMAuthorizationType.Admin;

            return DMAuthorizationType.None;
        }

        private static void LogDMAuthorization(NWPlayer dm, bool isAuthorizationSuccessful)
        {
            var account = GetPCPlayerName(dm);
            var cdKey = GetPCPublicCDKey(dm);
            var now = DateTime.UtcNow;
            var eventType = isAuthorizationSuccessful ? 13 : 14;

            var entity = new ModuleEvent
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
