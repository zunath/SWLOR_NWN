using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.TwinBlade
{
    public class CrossCut: IPerkHandler
    {
        public PerkType PerkType => PerkType.CrossCut;

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.TwinBlade;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a twin blade to use that ability.";
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
            int damage = 0;
            float duration = 0.0f;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D4(1);
                    duration = 6;   
                    break;
                case 2:
                    damage = RandomService.D4(2);
                    duration = 6;
                    break;
                case 3:
                    damage = RandomService.D4(2);
                    duration = 9;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    duration = 9;
                    break;
                case 5:
                    damage = RandomService.D8(2);
                    duration = 12;
                    break;
                case 6:
                    damage = RandomService.D6(3);
                    duration = 15;
                    break;
                case 7:
                    damage = RandomService.D8(3);
                    duration = 15;
                    break;
                case 8:
                    damage = RandomService.D8(3);
                    duration = 18;
                    break;
                case 9:
                    damage = RandomService.D8(4);
                    duration = 18;
                    break;
                case 10:
                    damage = RandomService.D8(4);
                    duration = 21;
                    break;
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_SLASHING), target);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectACDecrease(3), target, duration);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_HEAD_EVIL), target);

            player.SendMessage("Your target's armor has been breached.");
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
