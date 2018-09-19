using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.General
{
    public class Rest : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;

        public Rest(INWScript script,
            IPerkService perk)
        {
            _ = script;
            _perk = perk;
        }


        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return CanRest(oPC);
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You cannot rest while you or a party member are in combat.";
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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Rest);

            switch (perkLevel)
            {
                case 1: return 300.0f;
                case 2: return 270.0f;
                case 3:
                case 4:
                    return 240.0f;
                case 5:
                    return 210.0f;
                case 6:
                case 7:
                    return 180.0f;
                default: return 300.0f;
            }
        }

        public void OnImpact(NWPlayer oPC, NWObject oTarget)
        {
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Rest);
            Vector position = oPC.Position;
            int amount;

            switch (perkLevel)
            {
                default:
                    amount = 1;
                    break;
                case 4:
                case 5:
                case 6:
                    amount = 2;
                    break;
                case 7:
                    amount = 3;
                    break;
            }
            amount += oPC.EffectiveRestBonus;

            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0f, 6.1f);
            });

            oPC.DelayCommand(() =>
            {
                RunRest(oPC, position, amount);
            }, 6.0f);

            oPC.SendMessage("You begin to rest...");
            oPC.IsBusy = true;
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


        private void RunRest(NWPlayer oPC, Vector originalPosition, int amount)
        {
            Vector position = oPC.Position;

            if ((position.m_X != originalPosition.m_X ||
                 position.m_Y != originalPosition.m_Y ||
                 position.m_Z != originalPosition.m_Z) ||
                !CanRest(oPC) ||
                !oPC.IsValid)
            {
                oPC.SendMessage("You stop resting.");
                oPC.IsBusy = false;
                return;
            }

            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(amount), oPC);

            Effect vfx = _.EffectVisualEffect(VFX_IMP_HEAD_HOLY);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, oPC.Object);
            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0f, 6.1f);
            });
            oPC.DelayCommand(() =>
            {
                RunRest(oPC, originalPosition, amount);
            }, 6.0f);
        }

        private bool CanRest(NWPlayer oPC)
        {
            bool canRest = !oPC.IsInCombat;

            NWArea pcArea = oPC.Area;
            foreach (NWPlayer member in oPC.GetPartyMembers())
            {
                if (!member.Area.Equals(pcArea)) continue;

                if (member.IsInCombat)
                {
                    canRest = false;
                    break;
                }
            }

            return canRest;
        }

    }
}
