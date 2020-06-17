using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWNX;

namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class SaberFinesse : IPerkHandler
    {
        public PerkType PerkType => PerkType.SaberFinesse;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, Feat.WeaponFinesse);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalBool("LIGHTSABER") == false) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalBool("LIGHTSABER") == false) return;
            if (oItem == creature.LeftHand) return;
            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.RightHand;
            if (Equals(equipped, oItem) || (equipped.CustomItemType != CustomItemType.Lightsaber && equipped.CustomItemType != CustomItemType.Saberstaff && equipped.GetLocalBool("LIGHTSABER") == false))
            {
                NWNXCreature.RemoveFeat(creature, Feat.WeaponFinesse);
                return;
            }
            NWNXCreature.AddFeat(creature, Feat.WeaponFinesse);
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
