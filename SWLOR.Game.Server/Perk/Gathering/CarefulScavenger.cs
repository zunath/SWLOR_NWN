using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Gathering
{
    public class CarefulScavenger : IPerk
    {
        public PerkType PerkType => PerkType.CarefulScavenger;
        public string Name => "Careful Scavenger";
        public bool IsActive => true;
        public string Description => "Reduces the chance of exhausting a resource while scavenging.";
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
				1, new PerkLevel(2, "-5% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "-10% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "-15% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 15}, 
				})
			},
			{
				4, new PerkLevel(4, "-20% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 20}, 
				})
			},
			{
				5, new PerkLevel(5, "-25% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 25}, 
				})
			},
			{
				6, new PerkLevel(5, "-30% Chance",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Scavenging, 30}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
