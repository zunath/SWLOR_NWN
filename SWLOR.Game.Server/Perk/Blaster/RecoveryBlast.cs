using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.Blaster
{
    public class RecoveryBlast: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;

        public RecoveryBlast(
            INWScript script,
            IPerkService perk,
            IRandomService random)
        {
            _ = script;
            _perk = perk;
            _random = random;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return oPC.RightHand.CustomItemType == CustomItemType.BlasterRifle;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Must be equipped with a blaster rifle to use that ability.";
        }

        public int FPCost(NWPlayer oPC, int baseFPCost)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
            // Mark the player as performing a recovery blast.
            // This is later picked up in the OnApplyDamage event to reduce all damage to 0.
            oPC.SetLocalInt("RECOVERY_BLAST_ACTIVE", 1);

            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.RecoveryBlast);
            var members = oPC.GetPartyMembers().Where(x => _.GetDistanceBetween(x, oTarget) <= 10.0f);
            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky);

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
                    amount = _random.D12(1);
                    break;
                case 2:
                    amount = _random.D8(2);
                    break;
                case 3:
                    amount = _random.D8(3);
                    break;
                case 4:
                    amount = _random.D8(4);
                    break;
                case 5:
                    amount = _random.D8(5);
                    break;
                case 6:
                    amount = _random.D8(6);
                    break;
                default: return;
            }

            if (_random.D100(1) <= luck)
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
