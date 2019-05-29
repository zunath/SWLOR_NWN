using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.Perk.Blaster
{
    public class RapidShot : IPerkHandler
    {
        public PerkType PerkType => PerkType.RapidShot;

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
            NWNXCreature.RemoveFeat(oPC, _.FEAT_RAPID_SHOT);
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.LightArmor &&
                oItem.CustomItemType != CustomItemType.BlasterPistol) return;

            ApplyFeatChanges(oPC, null);
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.LightArmor &&
                oItem.CustomItemType != CustomItemType.BlasterPistol) return;

            ApplyFeatChanges(oPC, oItem);
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        private void ApplyFeatChanges(NWPlayer oPC, NWItem oItem)
        {
            NWItem equippedArmor = oItem ?? oPC.Chest;
            NWItem equippedWeapon = oItem ?? oPC.RightHand;

            if (equippedArmor.Equals(oItem) || equippedWeapon.Equals(oItem) || 
                equippedArmor.CustomItemType != CustomItemType.LightArmor ||
                equippedWeapon.CustomItemType != CustomItemType.BlasterPistol)
            {
                NWNXCreature.RemoveFeat(oPC, _.FEAT_RAPID_SHOT);
                return;
            }

            NWNXCreature.AddFeat(oPC, _.FEAT_RAPID_SHOT);
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWPlayer player, int perkLevel, int tick)
        {
            
        }
    }
}
