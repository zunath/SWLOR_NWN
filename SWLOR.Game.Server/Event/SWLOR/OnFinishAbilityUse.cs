using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnFinishAbilityUse
    {
        public NWCreature Activator { get; set; }
        public string SpellUUID { get; set; }
        public int PerkID { get; set; }
        public NWObject Target { get; set; }
        public int PCPerkLevel { get; set; }
        public int SpellTier { get; set; }
        public float ArmorPenalty { get; set; }

        public OnFinishAbilityUse(
            NWCreature activator,
            string spellUUID,
            int perkID,
            NWObject target,
            int pcPerkLevel,
            int spellTier,
            float armorPenalty)
        {
            Activator = activator;
            SpellUUID = spellUUID;
            PerkID = perkID;
            Target = target;
            PCPerkLevel = pcPerkLevel;
            SpellTier = spellTier;
            ArmorPenalty = armorPenalty;
        }
    }
}
