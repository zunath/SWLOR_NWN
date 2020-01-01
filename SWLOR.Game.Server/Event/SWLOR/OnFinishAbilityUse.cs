using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnFinishAbilityUse
    {
        public NWCreature Activator { get; set; }
        public string SpellUUID { get; set; }
        public PerkType PerkType { get; set; }
        public NWObject Target { get; set; }
        public int PCPerkLevel { get; set; }
        public int SpellTier { get; set; }
        public float ArmorPenalty { get; set; }

        public OnFinishAbilityUse(
            NWCreature activator,
            string spellUUID,
            PerkType perkType,
            NWObject target,
            int pcPerkLevel,
            int spellTier,
            float armorPenalty)
        {
            Activator = activator;
            SpellUUID = spellUUID;
            PerkType = perkType;
            Target = target;
            PCPerkLevel = pcPerkLevel;
            SpellTier = spellTier;
            ArmorPenalty = armorPenalty;
        }
    }
}
