using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Perk.ForceControl
{
    public class ForceBody: IPerk
    {
        public PerkType PerkType => PerkType.ForceBody;
        public string Name => "Force Body";
        public bool IsActive => true;
        public string Description => "Converts a percentage of the casters current HP into FP.";
        public PerkCategoryType Category => PerkCategoryType.ForceControl;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ForceBody;
        public PerkExecutionType ExecutionType => PerkExecutionType.ForceAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.CurrentHP <= 1)
                return "You do not have enough HP to use this ability.";

            if (oPC.IsPlayer)
            {
                var dbPlayer = DataService.Player.GetByID(oPC.GlobalID);
                if (dbPlayer.CurrentFP >= dbPlayer.MaxFP)
                    return "Your FP is already at maximum.";
            }

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
            switch (spellTier)
            {
                case 1: return 900f; // 15 minutes
                case 2: return 600f; // 10 minutes
                case 3: return 420f; // 7 minutes
                case 4: return 300f; // 5 minutes
            }

            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            float percent = 0.0f;

            switch (spellTier)
            {
                case 1:
                    percent = 0.10f;
                    break;
                case 2:
                    percent = 0.20f;
                    break;
                case 3:
                    percent = 0.35f;
                    break;
                case 4:
                    percent = 0.50f;
                    break;
            }

            int recovery = (int)(target.CurrentHP * percent);
            if (recovery < 1) recovery = 1;

            // Damage user.
            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(recovery), creature);
            
            // Check lucky chance.
            int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky);
            if (RandomService.D100(1) <= luck)
            {
                recovery *= 2;
                creature.SendMessage("Lucky Force Body!");
            }
            
            // Recover FP on target.
            AbilityService.RestorePlayerFP(target.Object, recovery);

            // Play VFX
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Head_Odd), target);

            // Grant XP, if player.
            if (creature.IsPlayer)
            {
                SkillService.GiveSkillXP(creature.Object, SkillType.ForceControl, recovery * 2);
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
				1, new PerkLevel(3, "Converts 10% of the casters current HP into FP.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceControl, 10}, 
				})
			},
			{
				2, new PerkLevel(4, "Converts 20% of the casters current HP into FP.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceControl, 25}, 
				})
			},
			{
				3, new PerkLevel(5, "Converts 35% of the casters current HP into FP.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceControl, 40}, 
				})
			},
			{
				4, new PerkLevel(6, "Converts 50% of the casters current HP into FP.",
				new Dictionary<SkillType, int>
				{
					{ SkillType.ForceControl, 55}, 
				})
			},
		};


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
