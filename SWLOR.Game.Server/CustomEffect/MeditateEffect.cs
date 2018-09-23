using System;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class MeditateEffect: ICustomEffect
    {
        private readonly INWScript _;
        private readonly IAbilityService _ability;
        private readonly IPerkService _perk;
        private readonly ICustomEffectService _customEffect;

        public MeditateEffect(
            INWScript script,
            IAbilityService ability,
            IPerkService perk,
            ICustomEffectService customEffect)
        {
            _ = script;
            _ability = ability;
            _perk = perk;
            _customEffect = customEffect;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            NWPlayer player = oTarget.Object;

            player.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_MEDITATE, 1.0f, 6.1f);
            });

            player.IsBusy = true;

            string data = $"{player.Position.m_X},{player.Position.m_Y},{player.Position.m_Z}";

            return data;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            NWPlayer player = oTarget.Object;
            int meditateTick = oTarget.GetLocalInt("MEDITATE_TICK") + 1;

            if (meditateTick >= 6)
            {
                meditateTick = 0;

                int amount = CalculateAmount(player);
                string[] values = data.Split(',');
                Vector originalPosition = _.Vector
                (
                    Convert.ToSingle(values[0]),
                    Convert.ToSingle(values[1]),
                    Convert.ToSingle(values[2])
                );

                RunMeditate(player, originalPosition, amount);

            }

            oTarget.SetLocalInt("MEDITATE_TICK", meditateTick);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            NWPlayer player = oTarget.Object;
            player.IsBusy = false;
        }

        private int CalculateAmount(NWPlayer player)
        {
            int perkLevel = _perk.GetPCPerkLevel(player, PerkType.Meditate);
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
            amount += player.EffectiveMeditateBonus;

            return amount;
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
                oPC.IsBusy = false;
                _customEffect.RemovePCCustomEffect(oPC, CustomEffectType.Meditate);
                return;
            }

            _ability.RestoreFP(oPC, amount);
            Effect vfx = _.EffectVisualEffect(VFX_IMP_HEAD_MIND);
            _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, oPC.Object);
            oPC.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_MEDITATE, 1.0f, 6.1f);
            });
        }

        public static bool CanMeditate(NWPlayer oPC)
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
