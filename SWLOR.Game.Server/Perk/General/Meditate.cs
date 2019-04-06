using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.CustomEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.General
{
    public class Meditate: IPerkHandler
    {
        public PerkType PerkType => PerkType.Meditate;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellFeatID)
        {
            if (!MeditateEffect.CanMeditate(oPC))
                return "You cannot meditate while you or a party member are in combat.";
            
            return string.Empty;
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
            int perkLevel = PerkService.GetPCPerkLevel(oPC, PerkType.Meditate);

            switch (perkLevel)
            {
                case 1: return 300.0f;
                case 2: return 270.0f;
                case 3:
                case 4:
                    return 240.0f;
                case 5:
                    return 210.0f;
                case 6:
                case 7:
                    return 180.0f;
                default: return 300.0f;
            }
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            CustomEffectService.ApplyCustomEffect(player, player, CustomEffectType.Meditate, -1, 0, null);
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
        }

        public void OnRemoved(NWPlayer oPC)
        {
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
