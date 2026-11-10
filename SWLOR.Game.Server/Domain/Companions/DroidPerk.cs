using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Companions
{
    public class DroidPerk
    {
        public PerkType Perk { get; set; }
        public int Level { get; set; }

        public DroidPerk()
        {
            Perk = PerkType.Invalid;
            Level = 0;
        }

        public DroidPerk(PerkType perk, int level)
        {
            Perk = perk;
            Level = level;
        }
    }
}
