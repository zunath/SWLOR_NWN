using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Throwing
{
    public class PreciseToss: IPerk
    {
        public PerkType PerkType => PerkType.PreciseToss;
        public string Name => "Precise Toss";
        public bool IsActive => true;
        public string Description => "Your next attack deals extra piercing damage and inflicts bleeding on your target, which deals damage over time for a short time. Must be equipped with a throwing weapon.";
        public PerkCategoryType Category => PerkCategoryType.Throwing;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.PreciseToss;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.Throwing)
                return "Must be equipped with a throwing weapon to use that ability.";

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

        public void OnImpact(NWCreature creature, NWObject target, int level, int spellTier)
        {
            int damage;
            int seconds;
            int dotDamage;

            switch (level)
            {
                case 1:
                    damage = RandomService.D4(1);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 2:
                    damage = RandomService.D8(1);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 3:
                    damage = RandomService.D8(2);
                    seconds = 6;
                    dotDamage = 1;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 5:
                    damage = RandomService.D8(3);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 6:
                    damage = RandomService.D8(4);
                    seconds = 12;
                    dotDamage = 2;
                    break;
                case 7:
                    damage = RandomService.D8(5);
                    seconds = 12;
                    dotDamage = 3;
                    break;
                case 8:
                    damage = RandomService.D8(5);
                    seconds = 18;
                    dotDamage = 3;
                    break;
                case 9:
                    damage = RandomService.D8(6);
                    seconds = 24;
                    dotDamage = 3;
                    break;
                default: return;
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage, DamageType.Piercing), target);
            CustomEffectService.ApplyCustomEffect(creature, target.Object, CustomEffectType.Bleeding, seconds, level, Convert.ToString(dotDamage));


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
				1, new PerkLevel(2, "1d4 damage, bleeding lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 10}, 
				})
			},
			{
				2, new PerkLevel(2, "1d8 damage, bleeding lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 20}, 
				})
			},
			{
				3, new PerkLevel(3, "2d8 damage, bleeding lasts 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 30}, 
				})
			},
			{
				4, new PerkLevel(3, "2d8 damage, bleeding lasts 12 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 40}, 
				})
			},
			{
				5, new PerkLevel(4, "3d8 damage, bleeding lasts 12 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 50}, 
				})
			},
			{
				6, new PerkLevel(4, "4d8 damage, bleeding lasts 12 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 60}, 
				})
			},
			{
				7, new PerkLevel(5, "5d8 damage, bleeding lasts 12 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 70}, 
				})
			},
			{
				8, new PerkLevel(5, "5d8 damage, bleeding lasts 18 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 80}, 
				})
			},
			{
				9, new PerkLevel(6, "6d8 damage, bleeding lasts 24 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.Throwing, 90}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            throw new NotImplementedException();
        }
    }
}
