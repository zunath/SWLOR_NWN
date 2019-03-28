using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerValidationService
    {
        public static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            string error = ValidateBackground(player);

            if (string.IsNullOrWhiteSpace(error))
            {
                error = ValidateName(player);
            }
            
            if (!string.IsNullOrWhiteSpace(error))
            {
                _.BootPC(player, error);
                NWNXAdmin.DeletePlayerCharacter(player, true);
            }
        }

        private static string ValidateName(NWPlayer player)
        {
            string error = string.Empty;
            string name = player.Name.ToLower();
            string[] words = name.Split(null);

            foreach (var word in words)
            {
                if(ReservedWords.Contains(word))
                {
                    error = "Your character has a reserved word in his or her name. Please remake your character and use a different name. Note that famous Star Wars names are not allowed to be used for your character. Offending word: " + word;
                    break;
                }
            }

            return error;
        }

        private static string ValidateBackground(NWPlayer player)
        {
            int classID = _.GetClassByPosition(1, player);
            bool isPlayerClass = Convert.ToInt32(_.Get2DAString("classes", "PlayerClass", classID)) == 1;
            bool isValid = isPlayerClass;
            string error = string.Empty;

            if (!isValid)
            {
                error = "You have selected an invalid player background. Please ensure your hak files are up to date and then create a new character.";
            }

            return error;
        }

        private static readonly string[] ReservedWords =
        {
            "darth", "malak", "revan", "jedi", "sith", "yoda", "luke", "skywalker", "starkiller", "vader", "han", "solo", "boba", "bobba", "fett",
            "admiral", "ackbar", "c-3p0", "c3p0", "c-3po", "r2d2", "r2-d2", "qui-gon", "jinn", "greedo", "hutt", "the", "jabba", "mace", "windu", 
            "padme", "padmé", "amidala", "poe", "dameron", "tarkin", "moff", "anakin", "lando", "calrissian", "leia", "finn", "maul", "emperor",
            "palpatine", "rey", "obi-wan", "kenobi", "kylo", "ren", "bb-8", "bb8", "chewbacca", "princess", "canderous", "ordo", "t3-m4", "hk-47",
            "carth", "onasi", "mission", "vao", "zaalbar", "bastila", "shan", "juhani", "jolee", "bindo", "atton", "rand", "bao", "dur", "bao-dur",
            "mical", "mira", "hanharr", "brianna", "visas", "marr", "g0-t0", "go-to", "goto", "zayne", "carrick", "marn", "hierogryph", "jarael", "gorman",
            "vandrayk", "elbee", "rohlan", "dyre", "slyssk", "sion", "nihilus", "general", "zunath", "xephnin", "taelon", "lestat", "dm", "gm"
        };
    }
}
