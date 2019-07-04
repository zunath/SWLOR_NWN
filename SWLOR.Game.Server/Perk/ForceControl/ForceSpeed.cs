using System;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceSpeed: IPerkHandler
    {
        public PerkType PerkType => PerkType.ForceSpeed;
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            switch (spellTier)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 6;
                case 4: return 8;
                case 5: return 20;
            }

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
            Effect effect;
            float duration;
            switch (spellTier)
            {
                case 1:
                    effect = _.EffectMovementSpeedIncrease(10);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 2));
                    duration = 60f;
                    break;
                case 2:
                    effect = _.EffectMovementSpeedIncrease(20);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 4));
                    duration = 90f;
                    break;
                case 3:
                    effect = _.EffectMovementSpeedIncrease(30);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 6));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 120f;
                    break;
                case 4:
                    effect = _.EffectMovementSpeedIncrease(40);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 8));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 150f;
                    break;
                case 5:
                    effect = _.EffectMovementSpeedIncrease(50);
                    effect = _.EffectLinkEffects(effect, _.EffectAbilityIncrease(_.ABILITY_DEXTERITY, 10));
                    effect = _.EffectLinkEffects(effect, _.EffectModifyAttacks(1));
                    duration = 180f;
                    break;
                default:
                    throw new ArgumentException(nameof(perkLevel) + " invalid. Value " + perkLevel + " is unhandled.");
            }
            
            // Check lucky chance.
            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);
            if (RandomService.D100(1) <= luck)
            {
                duration *= 2;
                creature.SendMessage("Lucky Force Speed!");
            }

            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, target, duration);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_IMP_AC_BONUS), target);

            if (creature.IsPlayer)
            {
                NWPlayer player = creature.Object;
                int skillLevel = SkillService.GetPCSkillRank(player, SkillType.ForceControl);
                int xp = skillLevel * 10 + 50;
                SkillService.GiveSkillXP(player, SkillType.ForceControl, xp);
            }
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
