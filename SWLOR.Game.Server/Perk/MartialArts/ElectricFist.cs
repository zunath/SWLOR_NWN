using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class ElectricFist: IPerkHandler
    {
        public PerkType PerkType => PerkType.ElectricFist;
        public string Name => "Electric Fist";
        public bool IsActive => true;
        public string Description => "Your next attack deals extra electrical damage and inflicts shock, which paralyzes your target for a short period of time. Must be equipped with a power glove.";
        public PerkCategoryType Category => PerkCategoryType.MartialArts;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.ElectricFist;
        public PerkExecutionType ExecutionType => PerkExecutionType.QueuedWeaponSkill;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (!oPC.RightHand.IsValid && !oPC.LeftHand.IsValid)
                return string.Empty;

            return "Must be equipped with a power glove in order to use that ability.";
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
            int damage;
            float duration;

            switch (perkLevel)
            {
                case 1:
                    damage = RandomService.D8(1);
                    duration = 3;
                    break;
                case 2:
                    damage = RandomService.D8(2);
                    duration = 3;
                    break;
                case 3:
                    damage = RandomService.D8(3);
                    duration = 3;
                    break;
                case 4:
                    damage = RandomService.D8(3);
                    duration = 6;
                    break;
                case 5:
                    damage = RandomService.D8(4);
                    duration = 6;
                    break;
                case 6:
                    damage = RandomService.D8(5);
                    duration = 6;
                    break;
                case 7:
                    damage = RandomService.D8(6);
                    duration = 6;
                    break;
                case 8:
                    damage = RandomService.D8(7);
                    duration = 6;
                    break;
                case 9:
                    damage = RandomService.D8(7);
                    duration = 9;
                    break;
                case 10:
                    damage = RandomService.D8(8);
                    duration = 9;
                    break;
                default: return;
            }
            
            _.ApplyEffectToObject(DurationType.Temporary, _.EffectStunned(), target, duration);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectDamage(damage, DamageType.Electrical), target);
            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(Vfx.Vfx_Imp_Sunstrike), target);
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

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
            
        }
    }
}
