using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Gathering
{
    public class ScavengingExpert : IPerk
    {
        public PerkType PerkType => PerkType.ScavengingExpert;
        public string Name => "Scavenging Expert";
        public bool IsActive => true;
        public string Description => "You have a chance to search multiple times while scavenging.";
        public PerkCategoryType Category => PerkCategoryType.Gathering;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.None;
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
				1, new PerkLevel(2, "10% chance to search one more time",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "20% chance to search one more time",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "30% chance to search one more time",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 15}, 
				})
			},
			{
				4, new PerkLevel(3, "40% chance to search one more time",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 20}, 
				})
			},
			{
				5, new PerkLevel(4, "50% chance to search one more time",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 25}, 
				})
			},
			{
				6, new PerkLevel(4, "50% chance to search one more time. 10% chance to search a second time.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 30}, 
				})
			},
			{
				7, new PerkLevel(5, "50% chance to search one more time. 20% chance to search a second time.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 35}, 
				})
			},
			{
				8, new PerkLevel(5, "50% chance to search one more time. 30% chance to search a second time.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 40}, 
				})
			},
			{
				9, new PerkLevel(6, "50% chance to search one more time. 40% chance to search a second time.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 45}, 
				})
			},
			{
				10, new PerkLevel(7, "50% chance to search one more time. 50% chance to search a second time.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 50}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
