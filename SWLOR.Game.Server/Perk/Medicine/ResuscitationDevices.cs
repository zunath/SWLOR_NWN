using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.Medicine
{
    public class ResuscitationDevices : IPerk
    {
        public PerkType PerkType => PerkType.ResuscitationDevices;
        public string Name => "Resuscitation Devices";
        public bool IsActive => true;
        public string Description => "Enables and improves the use of resuscitation devices.";
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
				1, new PerkLevel(3, "Enables the use of tech 1 resuscitation devices.",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 5}, 
				})
			},
			{
				2, new PerkLevel(3, "Can use tech 1 resuscitation devices. Resuscitation HP/FP recovery +1",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "Can use tech 1 resuscitation devices. Resuscitation HP/FP recovery +2",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 15}, 
				})
			},
			{
				4, new PerkLevel(3, "Can use tech 2 resuscitation devices. Resuscitation HP/FP recovery +2",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 20}, 
				})
			},
			{
				5, new PerkLevel(4, "Can use tech 2 resuscitation devices. Resuscitation HP/FP recovery +3",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 25}, 
				})
			},
			{
				6, new PerkLevel(4, "Can use tech 3 resuscitation devices. Resuscitation HP/FP recovery +3",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 30}, 
				})
			},
			{
				7, new PerkLevel(5, "Can use tech 3 resuscitation devices. Resuscitation HP/FP recovery +4",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 35}, 
				})
			},
			{
				8, new PerkLevel(5, "Can use tech 4 resuscitation devices. Resuscitation HP/FP recovery +4",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 40}, 
				})
			},
			{
				9, new PerkLevel(6, "Can use tech 4 resuscitation devices. Resuscitation HP/FP recovery +5",
				new Dictionary<Skill, int>
				{
					{ Skill.Medicine, 45}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
