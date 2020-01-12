using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;


namespace SWLOR.Game.Server.Perk.Shields
{
    public class ExpulsionManeuver : IPerk
    {
        public PerkType PerkType => PerkType.ExpulsionManeuver;
        public string Name => "Expulsion Maneuver";
        public bool IsActive => true;
        public string Description => "Occasionally receive increased Attack Bonus when blocking with a shield.";
        public PerkCategoryType Category => PerkCategoryType.Shields;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.ShieldOnHit;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 15;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
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
            float length;
            int ab;
            int chance;

            switch (perkLevel)
            {
                case 1:
                    length = 12.0f;
                    ab = 1;
                    chance = 10;
                    break;
                case 2:
                    length = 12.0f;
                    ab = 1;
                    chance = 20;
                    break;
                case 3:
                    length = 12.0f;
                    ab = 2;
                    chance = 20;
                    break;
                case 4:
                    length = 12.0f;
                    ab = 2;
                    chance = 30;
                    break;
                case 5:
                    length = 12.0f;
                    ab = 3;
                    chance = 30;
                    break;
                default:
                    return;
            }

            if (creature.IsPlayer)
            {
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(creature.Object);
                int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky) + effectiveStats.Luck;
                chance += luck;
            }

            if (RandomService.Random(100) + 1 <= chance)
            {
                _.ApplyEffectToObject(DurationType.Temporary, _.EffectAttackIncrease(ab), creature.Object, length);
                creature.SendMessage(ColorTokenService.Combat("You perform a defensive maneuver."));
            }
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
				1, new PerkLevel(2, "+1 AB for 12 seconds with a 10% of occurring",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "+1 AB for 12 seconds with a 20% of occurring",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "+2 AB for 12 seconds with a 20% of occurring",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 15}, 
				})
			},
			{
				4, new PerkLevel(3, "+2 AB for 12 seconds with a 30% of occurring",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 20}, 
				})
			},
			{
				5, new PerkLevel(4, "+3 AB for 12 seconds with a 30% of occurring",
				new Dictionary<Skill, int>
				{
					{ Skill.Shields, 25}, 
				})
			},
		};

                public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>();


                public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
