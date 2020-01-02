using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using static NWN._;

namespace SWLOR.Game.Server.Perk.General
{
    public class Dash: IPerk
    {
        public PerkType PerkType => PerkType.Dash;
        public string Name => "Dash";
        public bool IsActive => true;
        public string Description => "Increases your movement speed for a short period of time.";
        public PerkCategoryType Category => PerkCategoryType.General;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Dash;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.FireForget_Victory2;

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
            float duration = 60.0f;
            int speed;

            switch (perkLevel)
            {
                case 1:
                    speed = 25;
                    break;
                case 2:
                    speed = 30;
                    break;
                case 3:
                    speed = 35;
                    break;
                case 4:
                    speed = 40;
                    break;
                case 5:
                    speed = 45;
                    break;
                case 6:
                    speed = 50;
                    break;
                case 7:
                    speed = 50;
                    duration = 120.0f;
                    break;
                default: return;
            }

            if (creature.DexterityModifier > 0)
            {
                duration = duration + creature.DexterityModifier * 5;
            }

            Effect movement = _.EffectMovementSpeedIncrease(speed);
            movement = _.TagEffect(movement, "DASH");

            _.ApplyEffectToObject(DurationType.Temporary, movement, target, duration);
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
