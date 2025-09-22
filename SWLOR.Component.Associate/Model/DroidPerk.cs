using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Models
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
