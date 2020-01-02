using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Throwing
{
    public class GrenadeProficiency: IPerk
    {
        public PerkType PerkType => PerkType.GrenadeProficiency;
        public string Name => "Grenade Proficiency";
        public bool IsActive => true;
        public string Description => "Grenades have a chance to inflict knockdown.";
        public PerkCategoryType Category => PerkCategoryType.Throwing;
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
				1, new PerkLevel(2, "10% chance, lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 10}, 
				})
			},
			{
				2, new PerkLevel(2, "20% chance, lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 20}, 
				})
			},
			{
				3, new PerkLevel(3, "30% chance, lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 30}, 
				})
			},
			{
				4, new PerkLevel(3, "40% chance, lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 40}, 
				})
			},
			{
				5, new PerkLevel(4, "50% chance, lasts 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 50}, 
				})
			},
			{
				6, new PerkLevel(4, "60% chance, lasts 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 60}, 
				})
			},
			{
				7, new PerkLevel(5, "70% chance, lasts 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 70}, 
				})
			},
			{
				8, new PerkLevel(5, "80% chance, lasts 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 80}, 
				})
			},
			{
				9, new PerkLevel(6, "90% chance, lasts 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 90}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
