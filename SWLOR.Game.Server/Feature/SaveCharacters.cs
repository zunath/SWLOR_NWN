using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public static class SaveCharacters
    {
        private const string SaveCharactersVariable = "SAVE_CHARACTERS_TICK";
        private const string IsBarteringVariable = "IS_BARTERING";

        /// <summary>
        /// Saves characters every minute unless they're currently preoccupied (barter)
        /// </summary>
        [NWNEventHandler("pc_heartbeat")]
        public static void HandleSaveCharacters()
        {
            var player = OBJECT_SELF;
            var tick = GetLocalInt(player, SaveCharactersVariable) + 1;

            if(tick >= 10)
            {
                if (!GetLocalBool(player, IsBarteringVariable))
                {
                    ExportSingleCharacter(player);
                    tick = 0;
                }
            }

            SetLocalInt(player, SaveCharactersVariable, tick);
        }

        /// <summary>
        /// Marks players as bartering. This is used to ensure the PCs are not exported during this process.
        /// </summary>
        [NWNEventHandler("bart_start_bef")]
        public static void SetBarteringFlag()
        {
            var player1 = OBJECT_SELF;
            var player2 = StringToObject(EventsPlugin.GetEventData("BARTER_TARGET"));

            SetLocalBool(player1, IsBarteringVariable, true);
            SetLocalBool(player2, IsBarteringVariable, true);
        }

        /// <summary>
        /// Removes the bartering flag from PCs involved in a trade. This will ensure their files are exported on the next save occurrence.
        /// </summary>
        [NWNEventHandler("bart_end_bef")]
        public static void RemoveBarteringFlag()
        {
            var player1 = OBJECT_SELF;
            var player2 = StringToObject(EventsPlugin.GetEventData("BARTER_TARGET"));

            DeleteLocalBool(player1, IsBarteringVariable);
            DeleteLocalBool(player2, IsBarteringVariable);
        }
    }
}
