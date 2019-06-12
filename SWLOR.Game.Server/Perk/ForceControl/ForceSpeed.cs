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
        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellTier)
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
            int luck = PerkService.GetPCPerkLevel(player, PerkType.Lucky);
            if (RandomService.D100(1) <= luck)
            {
                duration *= 2;
                player.SendMessage("Lucky Force Speed!");
            }

            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, target, duration);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, _.EffectVisualEffect(_.VFX_IMP_AC_BONUS), target);
            
            int skillLevel = SkillService.GetPCSkillRank(player, SkillType.ForceControl);
            int xp = skillLevel * 10 + 10;
            SkillService.GiveSkillXP(player, SkillType.ForceControl, xp);
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

        public void OnConcentrationTick(NWPlayer player, NWObject target, int perkLevel, int tick)
        {
        }
    }
}
