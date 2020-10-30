using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class LegShot: IPerkHandler
    {
        public PerkType PerkType => PerkType.LegShot;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.BlasterPistol)
                return "Must be equipped with a blaster pistol to use that ability.";

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

        public void OnImpact(NWCreature creature, NWObject target, int level, int spellTier)
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


            NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectDamage(damage, DamageType.Piercing), target);
            NWScript.ApplyEffectToObject(DurationType.Temporary, NWScript.EffectCutsceneImmobilize(), target, duration);
            NWScript.ApplyEffectToObject(DurationType.Temporary, NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Acid_L), target, duration);
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
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
