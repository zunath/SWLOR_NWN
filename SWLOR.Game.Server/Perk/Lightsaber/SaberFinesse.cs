using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWNX;

using static NWN._;
namespace SWLOR.Game.Server.Perk.Lightsaber
{
    public class SaberFinesse : IPerkHandler
    {
        public PerkType PerkType => PerkType.SaberFinesse;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellTier)
        {
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
            ApplyFeatChanges(oPC, null);
        }

        public void OnRemoved(NWPlayer oPC)
        {
            NWNXCreature.RemoveFeat(oPC, _.FEAT_WEAPON_FINESSE);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalInt("LIGHTSABER") == FALSE) return;
            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.Lightsaber && oItem.CustomItemType != CustomItemType.Saberstaff && oItem.GetLocalInt("LIGHTSABER") == FALSE) return;
            if (oItem == oPC.LeftHand) return;
            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equipped = oItem ?? oPC.RightHand;
            if (Equals(equipped, oItem) || (equipped.CustomItemType != CustomItemType.Lightsaber && equipped.CustomItemType != CustomItemType.Saberstaff && equipped.GetLocalInt("LIGHTSABER") == FALSE))
            {
                NWNXCreature.RemoveFeat(oPC, _.FEAT_WEAPON_FINESSE);
                return;
            }
            NWNXCreature.AddFeat(oPC, _.FEAT_WEAPON_FINESSE);
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWPlayer player, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
