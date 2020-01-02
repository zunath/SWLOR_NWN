using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class EvadeBlasterFire : IPerk
    {
        public PerkType PerkType => PerkType.EvadeBlasterFire;
        public string Name => "Evade Blaster Fire";
        public bool IsActive => true;
        public string Description => "Enables you to evade a blaster shot if you meet the difficulty check. DEX modifier increases chance of evasion. Must be equipped with martial arts weapon.";
        public PerkCategoryType Category => PerkCategoryType.MartialArts;
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
				1, new PerkLevel(2, "18 second delay between evasion attempts.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.MartialArts, 10}, 
				})
			},
			{
				2, new PerkLevel(4, "12 second delay between evasion attempts.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.MartialArts, 25}, 
				})
			},
			{
				3, new PerkLevel(5, "6 second delay between evasion attempts.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.MartialArts, 50}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
