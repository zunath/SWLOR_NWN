using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class ForceDetection: IPerk
    {
        public PerkType PerkType => PerkType.ForceDetection;
        public string Name => "Force Detection";
        public bool IsActive => false;
        public string Description => "The caster senses nearby hidden creatures.";
        public PerkCategoryType Category => PerkCategoryType.ForceSense;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceDetection;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
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
				1, new PerkLevel(2, "The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 0}, 
				})
			},
			{
				2, new PerkLevel(2, "The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 5}, 
				})
			},
			{
				3, new PerkLevel(3, "The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).", SpecializationType.Sentinel,
                new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 20}, 
				})
			},
			{
				4, new PerkLevel(3, "The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).", SpecializationType.Sentinel,
                new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 35}, 
				})
			},
			{
				5, new PerkLevel(4, "The caster gets improved detection of hidden creatures while they concentrate.  ((Will do something when the stealth system is introduced)).", SpecializationType.Sentinel,
                new Dictionary<SkillType, int>
				{
					{ SkillType.ForceSense, 50}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
