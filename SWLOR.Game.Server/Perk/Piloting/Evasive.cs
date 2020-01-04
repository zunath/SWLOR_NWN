using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Piloting
{
    public class Evasive: IPerk
    {
        public PerkType PerkType => PerkType.Evasive;
        public string Name => "Evasive";
        public bool IsActive => true;
        public string Description => "Increases effective piloting skill by 3 per rank when dodging enemy attacks.";
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
				1, new PerkLevel(2, "Increases piloting by 3 when evading.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 10}, 
				})
			},
			{
				2, new PerkLevel(2, "Increases piloting by 6 when evading.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 20}, 
				})
			},
			{
				3, new PerkLevel(3, "Increases piloting by 9 when evading.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 30}, 
				})
			},
			{
				4, new PerkLevel(3, "Increases piloting by 12 when evading.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 40}, 
				})
			},
			{
				5, new PerkLevel(4, "Increases piloting by 15 when evading.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Piloting, 50}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
