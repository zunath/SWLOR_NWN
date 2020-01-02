using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class ForceInsight : IPerk
    {
        public PerkType PerkType => PerkType.ForceInsight;
        public string Name => "Force Insight";
        public bool IsActive => true;
        public string Description => "The caster boosts their AB and AC. Only affects themselves.";
        public PerkCategoryType Category => PerkCategoryType.ForceSense;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.BattleAndForceInsight;
        public PerkExecutionType ExecutionType => PerkExecutionType.ConcentrationAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 25;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
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

        		public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
		{
			{
				1, new PerkLevel(3, "Caster gets +3 AB.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 0}, 
				})
			},
			{
				2, new PerkLevel(4, "Caster gets +5 AB and +2 AC.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 15}, 
				})
			},
			{
				3, new PerkLevel(5, "Caster gets +5 AB and +4 AC.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 30}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            int abamount;
            int acamount;

            // Handle effects for differing spellTier values
            switch (perkLevel)
            {
                case 1:
                    abamount = 3;
                    acamount = 0;
                    break;
                case 2:
                    abamount = 5;
                    acamount = 2;
                    break;
                case 3:
                    abamount = 5;
                    acamount = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(perkLevel));
            }

            var effect = _.EffectACIncrease(acamount);
            effect = _.EffectLinkEffects(effect, _.EffectAttackIncrease(abamount));
            effect = _.TagEffect(effect, "EFFECT_FORCE_INSIGHT");

            // Remove any existing force insight effects.
            foreach(var existing in creature.Effects.Where(x => _.GetEffectTag(x) == "EFFECT_FORCE_INSIGHT"))
            {
                _.RemoveEffect(creature, existing);
            }
            
            // Apply the new effect.
            _.ApplyEffectToObject(DurationType.Temporary, effect, creature, 6.1f);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Dur_Magic_Resistance), target);

            // Register players to all combat targets for Force Sense.
            if (creature.IsPlayer)
            {
                SkillService.RegisterPCToAllCombatTargetsForSkill(creature.Object, SkillType.ForceSense, null);
            }

            EnmityService.AdjustEnmityOnAllTaggedCreatures(creature, 4);
        }
    }
}
