using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Feature
{
    public static class ReservedNames
    {
        private static readonly HashSet<string> _names = new HashSet<string>
        {
            "squall",
            "leonhart",
            "zell",
            "seifer",
            "quistis",
            "selphie",
            "irvine",
            "edea",
            "rinoa",
            "cid",
            "cloud",
            "strife",
            "zunath",
            "taelon",
            "lestat",
            "martinus"
        };

        /// <summary>
        /// When a player enters, check their character name. If the name contains reserved words,
        /// boot them out with a message saying so.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void CheckName()
        {
            var player = GetEnteringObject();
            var authLevel = Authorization.GetAuthorizationLevel(player);

            // Admins/DMs can do whatever they want.
            if (authLevel == AuthorizationLevel.Admin || authLevel == AuthorizationLevel.DM) return;

            var fullName = GetName(player);
            var names = fullName.Split(' ');

            foreach (var name in names)
            {
                if (_names.Contains(name.ToLower()))
                {
                    BootPC(player, $"Your name contains the reserved word: '{name}'. Please create a new character or speak with a DM.");
                    return;
                }
            }

        }
    }
}
