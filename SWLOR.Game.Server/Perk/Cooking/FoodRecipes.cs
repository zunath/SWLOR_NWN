using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Cooking
{
    public class FoodRecipes : IPerk
    {
        public PerkType PerkType => PerkType.FoodRecipes;
        public string Name => "Food Recipes";
        public bool IsActive => false;
        public string Description => "Unlocks additional food recipes on every odd level (1, 3, 5, 7) and adds an enhancement slot for every even level (2, 4, 6, 8) for cooking.";
        public PerkCategoryType Category => PerkCategoryType.Cooking;
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
				1, new PerkLevel(2, "Unlocks tier 1 cooking recipes.",
				new Dictionary<SkillType, int>
				{

				})
			},
			{
				2, new PerkLevel(2, "Unlocks tier 2 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "Unlocks tier 3 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 20}, 
				})
			},
			{
				4, new PerkLevel(4, "Unlocks tier 4 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 30}, 
				})
			},
			{
				5, new PerkLevel(5, "Unlocks tier 5 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 40}, 
				})
			},
			{
				6, new PerkLevel(5, "Unlocks tier 6 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 50}, 
				})
			},
			{
				7, new PerkLevel(5, "Unlocks tier 7 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 60}, 
				})
			},
			{
				8, new PerkLevel(6, "Unlocks tier 8 cooking recipes.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Cooking, 70}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
