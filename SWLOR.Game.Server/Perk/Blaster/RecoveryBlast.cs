using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class RecoveryBlast: IPerkHandler
    {
        public PerkType PerkType => PerkType.RecoveryBlast;

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.BlasterRifle;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a blaster rifle to use that ability.";
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
            // Mark the player as performing a recovery blast.
            // This is later picked up in the OnApplyDamage event to reduce all damage to 0.
            player.SetLocalInt("RECOVERY_BLAST_ACTIVE", 1);

            var members = player.PartyMembers.Where(x => _.GetDistanceBetween(x, target) <= 10.0f);
            int luck = PerkService.GetPCPerkLevel(player, PerkType.Lucky);

            foreach (var member in members)
            {
                HealTarget(member, perkLevel, luck);
            }
        }

        private void HealTarget(NWPlayer member, int level, int luck)
        {
            int amount;
            
            switch (level)
            {
                case 1:
                    amount = RandomService.D12(1);
                    break;
                case 2:
                    amount = RandomService.D8(2);
                    break;
                case 3:
                    amount = RandomService.D8(3);
                    break;
                case 4:
                    amount = RandomService.D8(4);
                    break;
                case 5:
                    amount = RandomService.D8(5);
                    break;
                case 6:
                    amount = RandomService.D8(6);
                    break;
                default: return;
            }

            if (RandomService.D100(1) <= luck)
            {
                amount *= 2;
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(amount), member);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_IMP_HEALING_S), member);
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
