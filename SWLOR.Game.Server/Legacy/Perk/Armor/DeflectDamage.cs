using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using DamageType = SWLOR.Game.Server.Core.NWScript.Enum.DamageType;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;


namespace SWLOR.Game.Server.Legacy.Perk.Armor
{
    public class DeflectDamage: IPerkHandler
    {
        public PerkType PerkType => PerkType.DeflectDamage;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            var armor = oPC.Chest;
            if (armor.CustomItemType != CustomItemType.HeavyArmor)
                return "You must be equipped with heavy armor to use that combat ability.";

            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int damageBase;
            var length = 12.0f;
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

            var effect = NWScript.EffectDamageShield(damageBase, (DamageBonus)randomDamage, DamageType.Magical);
            NWScript.ApplyEffectToObject(DurationType.Temporary, effect, creature.Object, length);

            effect = NWScript.EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Orange);
            NWScript.ApplyEffectToObject(DurationType.Temporary, effect, creature.Object, length);

            effect = NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus);
            NWScript.ApplyEffectToObject(DurationType.Instant, effect, creature.Object);
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
