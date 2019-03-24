using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;


namespace SWLOR.Game.Server.Perk.Blaster
{
    public class RapidShot : IPerkBehaviour
    {
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
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
    }
}
