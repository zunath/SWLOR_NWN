using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.OneHanded
{
    public class SneakAttack: IPerk
    {
        private readonly IPerkService _perk;

        public SneakAttack(IPerkService perk)
        {
            _perk = perk;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            NWItem weapon = oPC.RightHand;
            NWItem armor = oPC.Chest;

            return weapon.CustomItemType == CustomItemType.FinesseVibroblade &&
                   armor.CustomItemType == CustomItemType.LightArmor;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You must be equipped with a finesse blade and light armor to use that ability.";
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
            int perkRank = _perk.GetPCPerkLevel(oPC, PerkType.SneakAttack);
            float cooldown = baseCooldownTime;

            if (perkRank == 2)
            {
                cooldown -= 30f;
            }
            else if (perkRank > 2)
            {
                cooldown -= 60f;
            }

            return cooldown;
        }

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
            float minimum = oPC.Facing - 20;
            float maximum = oPC.Facing + 20;

            if (oTarget.Facing >= minimum &&
                oTarget.Facing <= maximum)
            {
                // Mark the player as committing a sneak attack.
                // This is later picked up in the OnApplyDamage event.
                oPC.SetLocalInt("SNEAK_ATTACK_ACTIVE", 1);
            }
            else
            {
                oPC.SetLocalInt("SNEAK_ATTACK_ACTIVE", 2);
            }
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
