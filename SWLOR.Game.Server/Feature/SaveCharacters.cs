using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public static class SaveCharacters
    {
        /// <summary>
        /// Saves characters every minute unless they're currently preoccupied (barter)
        /// </summary>
        [NWNEventHandler("mod_heartbeat")]
        public static void HandleSaveCharacters()
        {
            var module = GetModule();
            var tick = GetLocalInt(module, "SAVE_CHARACTERS_TICK") + 1;

            if(tick >= 10)
            {
                for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                {
                    if (GetLocalBool(player, "IS_BARTERING")) continue;

                    ExportSingleCharacter(player);
                }

                tick = 0;
            }

            SetLocalInt(module, "SAVE_CHARACTERS_TICK", tick);
        }

        /// <summary>
        /// Marks players as bartering. This is used to ensure the PCs are not exported during this process.
        /// </summary>
        [NWNEventHandler("bart_start_bef")]
        public static void SetBarteringFlag()
        {
            var player1 = OBJECT_SELF;
            var player2 = StringToObject(EventsPlugin.GetEventData("BARTER_TARGET"));

            SetLocalBool(player1, "IS_BARTERING", true);
            SetLocalBool(player2, "IS_BARTERING", true);
        }

        /// <summary>
        /// Removes the bartering flag from PCs involved in a trade. This will ensure their files are exported on the next save occurrence.
        /// </summary>
        [NWNEventHandler("bart_end_bef")]
        public static void RemoveBarteringFlag()
        {
            var player1 = OBJECT_SELF;
            var player2 = StringToObject(EventsPlugin.GetEventData("BARTER_TARGET"));

            DeleteLocalBool(player1, "IS_BARTERING");
            DeleteLocalBool(player2, "IS_BARTERING");
        }
    }
}
