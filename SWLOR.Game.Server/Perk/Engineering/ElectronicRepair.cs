using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Engineering
{
    public class ElectronicRepair: IPerk
    {
        public PerkType PerkType => PerkType.ElectronicRepair;
        public string Name => "Electronic Repair";
        public bool IsActive => true;
        public string Description => "Provides a bonus to repairs when using Electronic repair kits.";
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
				1, new PerkLevel(3, "Gains +2 to electronic repair when using an electronic repair kit",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 10}, 
				})
			},
			{
				2, new PerkLevel(3, "Gains +4 to electronic repair when using an electronic repair kit",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 20}, 
				})
			},
			{
				3, new PerkLevel(3, "Gains +6 to electronic repair when using an electronic repair kit",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 30}, 
				})
			},
			{
				4, new PerkLevel(3, "Gains +8 to electronic repair when using an electronic repair kit",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Engineering, 40}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
