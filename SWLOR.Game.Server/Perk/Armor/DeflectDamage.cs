using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWScript.Enumerations;


namespace SWLOR.Game.Server.Perk.Armor
{
    public class DeflectDamage: IPerkHandler
    {
        public PerkType PerkType => PerkType.DeflectDamage;
        public string Name => "Deflect Damage";
        public bool IsActive => true;
        public string Description => "You are protected by a damage shield for a limited time. Must be wearing heavy armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Evasiveness;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 10;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            NWItem armor = oPC.Chest;
            if (armor.CustomItemType != CustomItemType.HeavyArmor)
                return "You must be equipped with heavy armor to use that combat ability.";

            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int damageBase;
            float length = 12.0f;
            int randomDamage;

            switch (perkLevel)
            {
                case 1:
                    damageBase = 1;
                    randomDamage = 6; // 6 = DAMAGE_BONUS_1d4 constant
                    break;
                case 2:
                    damageBase = 1;
                    randomDamage = 8; // 8 = DAMAGE_BONUS_1d8 constant
                    break;
                case 3:
                    damageBase = 2;
                    randomDamage = 10; // 10 = DAMAGE_BONUS_2d6 constant
                    break;
                case 4:
                    damageBase = 2;
                    randomDamage = 11; // 11 = DAMAGE_BONUS_2d8 constant
                    break;
                case 5:
                    damageBase = 3;
                    randomDamage = 15; // 15 = DAMAGE_BONUS_2d12 constant
                    break;
                default:
                    return;
            }

            Effect effect = _.EffectDamageShield(damageBase, (DamageBonus)randomDamage, DamageType.Magical);
            _.ApplyEffectToObject(DurationType.Temporary, effect, creature.Object, length);

            effect = _.EffectVisualEffect(Vfx.Vfx_Dur_Aura_Orange);
            _.ApplyEffectToObject(DurationType.Temporary, effect, creature.Object, length);

            effect = _.EffectVisualEffect(Vfx.Vfx_Imp_Ac_Bonus);
            _.ApplyEffectToObject(DurationType.Instant, effect, creature.Object);
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
