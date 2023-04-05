using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public static class ReservedNames
    {
        private static readonly HashSet<string> _names = new HashSet<string>
        {
            "squall",
            "leonhart",
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
            "martinus",
            "darth", 
            "malak", 
            "revan", 
            "jedi", 
            "sith", 
            "yoda", 
            "skywalker", 
            "starkiller", 
            "vader", 
            "han", 
            "solo", 
            "boba", 
            "bobba", 
            "fett",
            "admiral", 
            "ackbar", 
            "c-3p0", 
            "c3p0", 
            "c-3po", 
            "r2d2", 
            "r2-d2", 
            "qui-gon", 
            "jinn", 
            "greedo", 
            "hutt", 
            "the", 
            "jabba", 
            "mace", 
            "windu",
            "padme", 
            "padmé", 
            "amidala", 
            "poe", 
            "dameron", 
            "tarkin", 
            "moff", 
            "anakin", 
            "lando", 
            "calrissian", 
            "leia", 
            "finn", 
            "maul", 
            "emperor",
            "palpatine", 
            "rey", 
            "obi-wan", 
            "kenobi", 
            "kylo", 
            "ren", 
            "bb-8", 
            "bb8", 
            "chewbacca", 
            "princess", 
            "canderous", 
            "ordo", 
            "t3-m4", 
            "hk-47",
            "carth", 
            "onasi", 
            "mission", 
            "vao", 
            "zaalbar", 
            "bastila", 
            "shan", 
            "juhani", 
            "jolee", 
            "bindo", 
            "atton", 
            "rand", 
            "bao", 
            "dur", 
            "bao-dur",
            "mical", 
            "mira", 
            "hanharr", 
            "brianna", 
            "visas", 
            "marr", 
            "g0-t0", 
            "go-to", 
            "goto", 
            "carrick", 
            "marn", 
            "hierogryph", 
            "jarael", 
            "gorman",
            "vandrayk", 
            "elbee", 
            "rohlan", 
            "dyre", 
            "slyssk", 
            "nihilus", 
            "general", 
            "xephnin", 
            "taelon", 
            "lestat", 
            "dm", 
            "gm"
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
