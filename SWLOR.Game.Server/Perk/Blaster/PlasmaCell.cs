using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class PlasmaCell: IPerk
    {
        public PerkType PerkType => PerkType.PlasmaCell;
        public string Name => "Plasma Cell";
        public bool IsActive => true;
        public string Description => "Your attacks have a chance to inflict additional elemental damage over time on each hit. Must be equipped with a Blaster Pistol or Blaster Rifle.";
        public PerkCategoryType Category => PerkCategoryType.FirearmsGeneral;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.PlasmaCell;
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
				1, new PerkLevel(2, "10% chance. Damage types: Fire",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 10}, 
				})
			},
			{
				2, new PerkLevel(2, "10% chance. Damage types: Fire, Electric",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 20}, 
				})
			},
			{
				3, new PerkLevel(3, "20% chance. Damage types: Fire, Electric",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 30}, 
				})
			},
			{
				4, new PerkLevel(3, "20% chance. Damage types: Fire, Electric, Sonic",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 40}, 
				})
			},
			{
				5, new PerkLevel(4, "30% chance. Damage types: Fire, Electric, Sonic",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 50}, 
				})
			},
			{
				6, new PerkLevel(4, "30% chance. Damage types: Fire, Electric, Sonic, Acid",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 60}, 
				})
			},
			{
				7, new PerkLevel(5, "40% chance. Damage types: Fire, Electric, Sonic, Acid",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 70}, 
				})
			},
			{
				8, new PerkLevel(5, "40% chance. Damage types: Fire, Electric, Sonic, Acid, Cold",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 80}, 
				})
			},
			{
				9, new PerkLevel(6, "50% chance. Damage types: Fire, Electric, Sonic, Acid, Cold",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 90}, 
				})
			},
			{
				10, new PerkLevel(7, "50% chance. Damage types: Fire, Electric, Sonic, Acid, Cold, Divine",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Firearms, 100}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
