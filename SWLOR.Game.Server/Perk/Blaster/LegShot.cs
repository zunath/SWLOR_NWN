using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class LegShot: IPerkBehaviour
    {
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.BlasterPistol;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a blaster pistol to use that ability.";
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

        public void OnImpact(NWPlayer player, NWObject target, int level, int spellFeatID)
        {
            int damage;
            float duration;

            switch (level)
            {
                case 1:
                    damage = RandomService.D4(1);
                    duration = 6;
                    break;
                case 2:
                    damage = RandomService.D8(1);
                    duration = 6;
                    break;
                case 3:
                    damage = RandomService.D8(2);
                    duration = 6;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    duration = 12;
                    break;
                case 5:
                    damage = RandomService.D8(3);
                    duration = 12;
                    break;
                case 6:
                    damage = RandomService.D8(4);
                    duration = 12;
                    break;
                case 7:
                    damage = RandomService.D8(5);
                    duration = 12;
                    break;
                case 8:
                    damage = RandomService.D8(5);
                    duration = 18;
                    break;
                case 9:
                    damage = RandomService.D8(6);
                    duration = 24;
                    break;
                default: return;
            }


            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_PIERCING), target);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectCutsceneImmobilize(), target, duration);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_IMP_ACID_L), target, duration);
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
