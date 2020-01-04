using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.Harvesting
{
    public class SpeedyRefining : IPerk
    {
        public PerkType PerkType => PerkType.SpeedyRefining;
        public string Name => "Speedy Refining";
        public bool IsActive => true;
        public string Description => "Reduces the amount of time it takes to refine raw materials.";
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
				1, new PerkLevel(2, "-10% Delay",
				new Dictionary<Skill, int>
				{
					{ Skill.Harvesting, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "-20% Delay",
				new Dictionary<Skill, int>
				{
					{ Skill.Harvesting, 10}, 
				})
			},
			{
				3, new PerkLevel(2, "-30% Delay",
				new Dictionary<Skill, int>
				{
					{ Skill.Harvesting, 15}, 
				})
			},
			{
				4, new PerkLevel(2, "-40% Delay",
				new Dictionary<Skill, int>
				{
					{ Skill.Harvesting, 20}, 
				})
			},
			{
				5, new PerkLevel(2, "-50% Delay",
				new Dictionary<Skill, int>
				{
					{ Skill.Harvesting, 35}, 
				})
			},
			{
				6, new PerkLevel(2, "-60% Delay",
				new Dictionary<Skill, int>
				{
					{ Skill.Harvesting, 40}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
