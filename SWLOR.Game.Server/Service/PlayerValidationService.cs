using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Service
{
    public class PlayerValidationService : IPlayerValidationService
    {
        private readonly INWScript _;
        private readonly INWNXAdmin _nwnxAdmin;
        private readonly IDataContext _db;

        public PlayerValidationService(
            INWScript script,
            INWNXAdmin nwnxAdmin,
            IDataContext db)
        {
            _ = script;
            _nwnxAdmin = nwnxAdmin;
            _db = db;
        }

        public void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            string error = ValidateBackground(player);

            if (string.IsNullOrWhiteSpace(error))
            {
                error = ValidateName(player);
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                error = ValidateExistingCharacter(player);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                _.BootPC(player, error);
                _nwnxAdmin.DeletePlayerCharacter(player, true);
            }
        }

        private string ValidateName(NWPlayer player)
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

        private string ValidateBackground(NWPlayer player)
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

        private readonly string[] ReservedWords =
        {
            "darth", "malak", "revan", "jedi", "sith", "yoda", "luke", "skywalker", "starkiller", "vader", "han", "solo", "boba", "bobba", "fett",
            "admiral", "ackbar", "c-3p0", "c3p0", "c-3po", "r2d2", "r2-d2", "qui-gon", "jinn", "greedo", "hutt", "the", "jabba", "mace", "windu", 
            "padme", "padmé", "amidala", "poe", "dameron", "tarkin", "moff", "anakin", "lando", "calrissian", "leia", "finn", "maul", "emperor",
            "palpatine", "rey", "obi-wan", "kenobi", "kylo", "ren", "bb-8", "bb8", "chewbacca", "princess", "canderous", "ordo", "t3-m4", "hk-47",
            "carth", "onasi", "mission", "vao", "zaalbar", "bastila", "shan", "juhani", "jolee", "bindo", "atton", "rand", "bao", "dur", "bao-dur",
            "mical", "mira", "hanharr", "brianna", "visas", "marr", "g0-t0", "go-to", "goto", "zayne", "carrick", "marn", "hierogryph", "jarael", "gorman",
            "vandrayk", "elbee", "rohlan", "dyre", "slyssk", "sion", "nihilus", "general", "zunath", "xephnin", "taelon", "lestat", "dm", "gm"
        };

        private string ValidateExistingCharacter(NWPlayer player)
        {
            if (player.IsInitializedAsPlayer) return string.Empty;

            string error = string.Empty;
            PlayerCharacter dbPlayer = _db.PlayerCharacters.FirstOrDefault(x => x.CharacterName == player.Name && x.IsDeleted == false);

            if (dbPlayer != null)
            {
                error = "Another player character with the same name already exists. Please create a new character with a unique name.";
            }

            return error;
        }


    }
}
