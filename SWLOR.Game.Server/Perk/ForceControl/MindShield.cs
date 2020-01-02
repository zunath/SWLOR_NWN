using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;

using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class MindShield: IPerk
    {
        public PerkType PerkType => PerkType.MindShield;
        public string Name => "Mind Shield";
        public bool IsActive => true;
        public string Description => "Protects the target from mind affecting powers and abilities.";
        public PerkCategoryType Category => PerkCategoryType.ForceControl;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.MindShield;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 20;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Light;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int spellTier, int tick)
        {
            ApplyEffect(creature, target, spellTier);
        }

        private void ApplyEffect(NWCreature creature, NWObject target, int spellTier)
        {
            Effect effectMindShield;

            // Handle effects for differing spellTier values
            switch (spellTier)
            {
                case 1:
                    effectMindShield =_.EffectImmunity(ImmunityType.Dazed);

                    creature.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Temporary, effectMindShield, target, 6.1f);
                    });
                    break;
                case 2:
                    effectMindShield = _.EffectImmunity(ImmunityType.Dazed);
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(ImmunityType.Confused));
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(ImmunityType.Dominate)); // Force Pursuade is DOMINATION effect

                    creature.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Temporary, effectMindShield, target, 6.1f);
                    });
                    break;
                case 3:
                    effectMindShield = _.EffectImmunity(ImmunityType.Dazed);
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(ImmunityType.Confused));
                    effectMindShield = _.EffectLinkEffects(effectMindShield, _.EffectImmunity(ImmunityType.Dominate)); // Force Pursuade is DOMINATION effect

                    if (target.GetLocalInt("FORCE_DRAIN_IMMUNITY") == 1)
                
                    creature.SetLocalInt("FORCE_DRAIN_IMMUNITY",0);                   
                    creature.DelayAssignCommand(() =>
                    {
                        creature.DeleteLocalInt("FORCE_DRAIN_IMMUNITY");
                    },6.1f);

                    creature.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DurationType.Temporary, effectMindShield, target, 6.1f);
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            // Play VFX
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Dur_Mind_Affecting_Positive), target);

        }
    }
}
