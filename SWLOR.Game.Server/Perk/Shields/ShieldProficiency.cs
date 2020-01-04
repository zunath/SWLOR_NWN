using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.Shields
{
    public class ShieldProficiency: IPerk
    {
        public PerkType PerkType => PerkType.ShieldProficiency;
        public string Name => "Shield Proficiency";
        public bool IsActive => true;
        public string Description => "Increases your damage reduction by 2% while equipped with a shield.";
        public PerkCategoryType Category => PerkCategoryType.Shields;
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
				1, new PerkLevel(3, "2% damage reduction",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 10}, 
				})
			},
			{
				2, new PerkLevel(3, "4% damage reduction",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 20}, 
				})
			},
			{
				3, new PerkLevel(3, "6% damage reduction",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 30}, 
				})
			},
			{
				4, new PerkLevel(3, "8% damage reduction",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 40}, 
				})
			},
			{
				5, new PerkLevel(3, "10% damage reduction",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 50}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
