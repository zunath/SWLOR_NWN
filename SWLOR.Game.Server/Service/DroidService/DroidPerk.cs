using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service.DroidService
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
