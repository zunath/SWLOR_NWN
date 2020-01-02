using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Medicine
{
    public class RangedHealing: IPerk
    {
        public PerkType PerkType => PerkType.RangedHealing;
        public string Name => "Ranged Healing";
        public bool IsActive => true;
        public string Description => "Increases the range you can use medical devices and kits.";
        public PerkCategoryType Category => PerkCategoryType.Medicine;
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
				1, new PerkLevel(2, "+1 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "+2 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "+3 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 15}, 
				})
			},
			{
				4, new PerkLevel(3, "+4 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 20}, 
				})
			},
			{
				5, new PerkLevel(3, "+5 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 25}, 
				})
			},
			{
				6, new PerkLevel(3, "+6 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 30}, 
				})
			},
			{
				7, new PerkLevel(4, "+7 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 35}, 
				})
			},
			{
				8, new PerkLevel(4, "+8 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 40}, 
				})
			},
			{
				9, new PerkLevel(4, "+9 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 45}, 
				})
			},
			{
				10, new PerkLevel(4, "+10 meters",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Medicine, 50}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
