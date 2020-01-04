using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Harvesting
{
    public class RefineryManagement : IPerk
    {
        public PerkType PerkType => PerkType.RefineryManagement;
        public string Name => "Refinery Management";
        public bool IsActive => true;
        public string Description => "Increases the time power sources last in a refinery.";
        public PerkCategoryType Category => PerkCategoryType.Harvesting;
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
				1, new PerkLevel(2, "+12 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Harvesting, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "+18 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Harvesting, 10}, 
				})
			},
			{
				3, new PerkLevel(2, "+24 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Harvesting, 15}, 
				})
			},
			{
				4, new PerkLevel(2, "+30 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Harvesting, 20}, 
				})
			},
			{
				5, new PerkLevel(3, "+48 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Harvesting, 35}, 
				})
			},
			{
				6, new PerkLevel(3, "+60 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Harvesting, 40}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
