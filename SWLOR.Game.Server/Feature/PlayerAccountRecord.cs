using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public static class PlayerAccountRecord
    {
        /// <summary>
        /// Records the character's CD key, account name, and login time so the DM player search
        /// can show them while the character is offline.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCharacterInitAfter)]
        public static void RecordAccountDetails()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return;

            dbPlayer.CDKey = GetPCPublicCDKey(player);
            dbPlayer.AccountName = GetPCPlayerName(player);
            dbPlayer.LastLoginDate = DateTime.UtcNow;
            DB.Set(dbPlayer);
        }
    }
}
