using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Admin.Contracts;
using SWLOR.Shared.Domain.Admin.Enums;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Admin.Feature
{
    public class DMAuthorization
    {
        private readonly ILogger _logger;
        private readonly IAuthorizationService _authorization;

        public DMAuthorization(ILogger logger, IAuthorizationService authorization)
        {
            _logger = logger;
            _authorization = authorization;
        }
        /// <summary>
        /// Verifies that a logging in player is an authorized DM.
        /// The player will be booted if they are not authorized.
        /// </summary>
        public void VerifyDM()
        {
            var dm = GetEnteringObject();
            if (!GetIsDM(dm) && !GetIsDMPossessed(dm)) return;

            var authorizationLevel = _authorization.GetAuthorizationLevel(dm);

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
        private void LogDMAuthorization(bool success)
        {
            var player = GetEnteringObject();
            string ipAddress = GetPCIPAddress(player);
            string cdKey = GetPCPublicCDKey(player);
            string account = GetPCPlayerName(player);
            string pcName = GetName(player);

            if (success)
            {
                var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Authorization successful";
                _logger.Write<DMAuthorizationLogGroup>( log);
            }
            else
            {
                var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Authorization UNSUCCESSFUL";
                _logger.Write<DMAuthorizationLogGroup>( log);
            }
        }

    }
}
