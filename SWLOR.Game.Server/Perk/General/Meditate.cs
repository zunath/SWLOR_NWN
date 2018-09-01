using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.General
{
    public class Meditate: IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IAbilityService _ability;

        public Meditate(INWScript script, 
            IPerkService perk,
            IAbilityService ability)
        {
            _ = script;
            _perk = perk;
            _ability = ability;
        }


        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return CanMeditate(oPC);
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "You cannot meditate while you or a party member are in combat.";
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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Meditate);

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
            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.Meditate);
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
            amount += oPC.EffectiveMeditateBonus;

            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_MEDITATE, 1.0f, 6.1f);
            });

            oPC.DelayCommand(() =>
            {
                RunMeditate(oPC, position, amount);
            }, 6.0f);

            oPC.SendMessage("You begin to meditate...");
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


        private void RunMeditate(NWPlayer oPC, Vector originalPosition, int amount)
        {
            Vector position = oPC.Position;

            if ((position.m_X != originalPosition.m_X || 
                 position.m_Y != originalPosition.m_Y ||
                 position.m_Z != originalPosition.m_Z) ||
                !CanMeditate(oPC) ||
                !oPC.IsValid)
            {
                oPC.SendMessage("You stop meditating.");
                oPC.IsBusy = false;
                return;
            }

            _ability.RestoreFP(oPC, amount);
            Effect vfx = _.EffectVisualEffect(NWScript.VFX_IMP_HEAD_MIND);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, vfx, oPC.Object);
            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(NWScript.ANIMATION_LOOPING_MEDITATE, 1.0f, 6.1f);
            });
            oPC.DelayCommand(() =>
            {
                RunMeditate(oPC, originalPosition, amount);
            }, 6.0f);
        }

        private bool CanMeditate(NWPlayer oPC)
        {
            bool canMeditate = !oPC.IsInCombat;

            NWArea pcArea = oPC.Area;
            foreach (NWPlayer member in oPC.GetPartyMembers())
            {
                if (!member.Area.Equals(pcArea)) continue;

                if (member.IsInCombat)
                {
                    canMeditate = false;
                    break;
                }
            }

            return canMeditate;
        }

    }
}
