using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.TwinBlade
{
    public class CrossCut: IPerk
    {
        public PerkType PerkType => PerkType.CrossCut;
        public string Name => "Cross Cut";
        public bool IsActive => true;
        public string Description => "Your next attack deals additional slashing damage and inflicts Breach, which reduces target's AC for a short period of time. Must be equipped with a twin blade.";
        public PerkCategoryType Category => PerkCategoryType.TwinBladesTwinVibroblades;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.CrossCut;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.RightHand.CustomItemType != CustomItemType.TwinBlade)
                return "Must be equipped with a twin blade to use that ability.";

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
            int damage = 0;
            float duration = 0.0f;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D4(1);
                    duration = 6;   
                    break;
                case 2:
                    damage = RandomService.D4(2);
                    duration = 6;
                    break;
                case 3:
                    damage = RandomService.D4(2);
                    duration = 9;
                    break;
                case 4:
                    damage = RandomService.D8(2);
                    duration = 9;
                    break;
                case 5:
                    damage = RandomService.D8(2);
                    duration = 12;
                    break;
                case 6:
                    damage = RandomService.D6(3);
                    duration = 15;
                    break;
                case 7:
                    damage = RandomService.D8(3);
                    duration = 15;
                    break;
                case 8:
                    damage = RandomService.D8(3);
                    duration = 18;
                    break;
                case 9:
                    damage = RandomService.D8(4);
                    duration = 18;
                    break;
                case 10:
                    damage = RandomService.D8(4);
                    duration = 21;
                    break;
            }

            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage, DamageType.Slashing), target);
            _.ApplyEffectToObject(DurationType.Temporary, _.EffectACDecrease(3), target, duration);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Head_Evil), target);

            creature.SendMessage("Your target's armor has been breached.");
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
				1, new PerkLevel(2, "1d4 slashing damage. Breach lasts for 6 seconds.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 5}, 
				})
			},
			{
				2, new PerkLevel(2, "2d4 slashing damage. Breach lasts for 6 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 10}, 
				})
			},
			{
				3, new PerkLevel(3, "2d4 slashing damage. Breach lasts for 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 15}, 
				})
			},
			{
				4, new PerkLevel(3, "2d8 slashing damage. Breach lasts for 9 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 20}, 
				})
			},
			{
				5, new PerkLevel(4, "2d8 slashing damage. Breach lasts for 12 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 25}, 
				})
			},
			{
				6, new PerkLevel(4, "3d6 slashing damage. Breach lasts for 15 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 30}, 
				})
			},
			{
				7, new PerkLevel(5, "3d8 slashing damage. Breach lasts for 15 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 35}, 
				})
			},
			{
				8, new PerkLevel(5, "3d8 slashing damage. Breach lasts for 18 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 40}, 
				})
			},
			{
				9, new PerkLevel(6, "4d8 slashing damage. Breach lasts for 18 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 45}, 
				})
			},
			{
				10, new PerkLevel(6, "4d8 slashing damage. Breach lasts for 21 seconds",
				new Dictionary<SkillType, int>
				{
					{ SkillType.TwinBlades, 50}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
