using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Engineering
{
    public class ProcessingEfficiency : IPerk
    {
        public PerkType PerkType => PerkType.ProcessingEfficiency;
        public string Name => "Processing Efficiency";
        public bool IsActive => true;
        public string Description => "Occasionally receive additional items while processing raw materials.";
        public PerkCategoryType Category => PerkCategoryType.Engineering;
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
				1, new PerkLevel(2, "+10% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "+20% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 10}, 
				})
			},
			{
				3, new PerkLevel(2, "+30% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 15}, 
				})
			},
			{
				4, new PerkLevel(2, "+40% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 20}, 
				})
			},
			{
				5, new PerkLevel(2, "+50% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 35}, 
				})
			},
			{
				6, new PerkLevel(2, "+60% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 40}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
