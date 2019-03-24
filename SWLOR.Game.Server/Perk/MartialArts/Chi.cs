using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.MartialArts
{
    public class Chi: IPerkBehaviour
    {
        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return true;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
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
            int wisdom = player.WisdomModifier;
            int constitution = player.ConstitutionModifier;
            int min = 1 + wisdom / 2 + constitution / 3;

            // Rank 7 and up: AOE heal party members
            if (perkLevel >= 7)
            {
                var members = player.PartyMembers.Where(x => Equals(x, player) || 
                                                             _.GetDistanceBetween(player, x) <= 10.0f && x.CurrentHP < x.MaxHP);
                foreach (var member in members)
                {
                    DoHeal(member, perkLevel, min);
                }
            }
            else
            {
                DoHeal(target, perkLevel, min);
            }
        }

        private void DoHeal(NWObject target, int perkLevel, int minimum)
        {
            float percentage = perkLevel * 0.10f;
            int heal = (int)(target.MaxHP * percentage);

            heal = RandomService.Random(minimum, heal);

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(heal), target);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_HEALING_G), target);
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
