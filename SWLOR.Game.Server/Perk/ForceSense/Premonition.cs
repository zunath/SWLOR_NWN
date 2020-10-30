using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.Legacy;


namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class Premonition: IPerkHandler
    {
        public PerkType PerkType => PerkType.Premonition;
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
            Effect effect;
            var duration = 6.1f;
            int concealment;
            int hitpoints;

            switch (perkLevel)
            {

                case 1:
                    hitpoints = 3;
                    effect = NWScript.EffectTemporaryHitpoints(hitpoints);
                    break;
                case 2:
                    hitpoints = 3;
                    concealment = 5;
                    effect = NWScript.EffectConcealment(concealment);
                    effect = NWScript.EffectLinkEffects(effect, NWScript.EffectTemporaryHitpoints(hitpoints));
                    break;
                case 3:
                    concealment = 10;
                    hitpoints = 5;
                    effect = NWScript.EffectConcealment(concealment);
                    effect = NWScript.EffectLinkEffects(effect, NWScript.EffectTemporaryHitpoints(hitpoints));
                    break;
                default:
                    throw new ArgumentException(nameof(perkLevel) + " invalid. Value " + perkLevel + " is unhandled.");


            }

            NWScript.ApplyEffectToObject(DurationType.Temporary, effect, creature, duration);
            NWScript.ApplyEffectToObject(DurationType.Temporary, NWScript.EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Purple), creature, duration);

            if (NWScript.GetIsInCombat(creature))
            {
                if (creature.IsPlayer)
                {
                    SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (perkLevel * 50));
                }
            }
        }
    }
}
