using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Piloting
{
    public class Scavenger: IPerk
    {
        public PerkType PerkType => PerkType.Scavenger;
        public string Name => "Scavenger";
        public bool IsActive => true;
        public string Description => "Increases the relative chance of finding salvage when determining space encounters.";
        public PerkCategoryType Category => PerkCategoryType.Piloting;
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
				1, new PerkLevel(2, "Increases relative chance of salvage encounters by 1 each.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 25}, 
				})
			},
			{
				2, new PerkLevel(2, "Increases relative chance of salvage encounters by 2 each.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 40}, 
				})
			},
			{
				3, new PerkLevel(3, "Increases relative chance of salvage encounters by 3 each.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 55}, 
				})
			},
			{
				4, new PerkLevel(3, "Increases relative chance of salvage encounters by 4 each.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 70}, 
				})
			},
			{
				5, new PerkLevel(4, "Increases relative chance of salvage encounters by 5 each.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 85}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
