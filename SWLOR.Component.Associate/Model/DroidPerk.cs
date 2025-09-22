namespace SWLOR.Component.Associate.Model
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
