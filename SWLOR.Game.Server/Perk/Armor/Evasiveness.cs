using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Evasiveness : IPerk
    {
        
        

        public Evasiveness(
            )
        {
            
            
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            NWItem armor = oPC.Chest;
            return armor.CustomItemType == CustomItemType.LightArmor;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You must be equipped with light armor to use that combat ability.";
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            int concealment;
            float length;

            switch (perkLevel)
            {
                case 1:
                    concealment = 10;
                    length = 12.0f;
                    break;
                case 2:
                    concealment = 15;
                    length = 12.0f;
                    break;
                case 3:
                    concealment = 20;
                    length = 12.0f;
                    break;
                case 4:
                    concealment = 25;
                    length = 12.0f;
                    break;
                case 5:
                    concealment = 30;
                    length = 18.0f;
                    break;
                default:
                    return;
            }

            Effect effect = _.EffectConcealment(concealment);
            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, player.Object, length);

            effect = _.EffectVisualEffect(_.VFX_DUR_AURA_CYAN);
            _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, effect, player.Object, length);

            effect = _.EffectVisualEffect(_.VFX_IMP_AC_BONUS);
            _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, effect, player.Object);
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
        }

        public void OnRemoved(NWPlayer oPC)
        {
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
