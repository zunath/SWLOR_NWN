using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Shared.Core.Event;
using SWLOR.Shared.Core.Log;

namespace SWLOR.Game.Server.Feature
{
    public class DMAuthorization
    {
        /// <summary>
        /// Verifies that a logging in player is an authorized DM.
        /// The player will be booted if they are not authorized.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleEnter)]
        public static void VerifyDM()
        {
            var dm = GetEnteringObject();
            if (!GetIsDM(dm) && !GetIsDMPossessed(dm)) return;

            var authorizationLevel = Authorization.GetAuthorizationLevel(dm);

            if (authorizationLevel != AuthorizationLevel.DM &&
                authorizationLevel != AuthorizationLevel.Admin)
            {
                LogDMAuthorization(false);
                BootPC(dm, "You are not authorized to log in as a DM. Please contact the server administrator if this is incorrect.");
                return;
            }

            LogDMAuthorization(true);
            ExecuteScript(ScriptName.OnDMFIClientEnter, OBJECT_SELF);
        }

        /// <summary>
        /// Logs whether an authorization attempt was successful.
        /// </summary>
        /// <param name="success">if true, will be logged as a successful attempt. if false, will be logged as unsuccessful.</param>
        private static void LogDMAuthorization(bool success)
        {
            var player = GetEnteringObject();
            string ipAddress = GetPCIPAddress(player);
            string cdKey = GetPCPublicCDKey(player);
            string account = GetPCPlayerName(player);
            string pcName = GetName(player);

            if (success)
            {
                var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Authorization successful";
                Log.Write(LogGroup.DMAuthorization, log);
            }
            else
            {
                var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Authorization UNSUCCESSFUL";
                Log.Write(LogGroup.DMAuthorization, log);
            }
        }

    }
}
