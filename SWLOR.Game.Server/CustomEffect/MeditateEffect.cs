using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class MeditateEffect : ICustomEffect
    {
        private readonly INWScript _;
        private readonly IAbilityService _ability;
        private readonly IPerkService _perk;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _playerStat;

        public MeditateEffect(
            INWScript script,
            IAbilityService ability,
            IPerkService perk,
            ICustomEffectService customEffect,
            IPlayerStatService playerStat)
        {
            _ = script;
            _ability = ability;
            _perk = perk;
            _customEffect = customEffect;
            _playerStat = playerStat;
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

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            NWPlayer player = oTarget.Object;
            int meditateTick = oTarget.GetLocalInt("MEDITATE_TICK") + 1;

            // Pull original position from data
            string[] values = data.Split(',');
            Vector originalPosition = _.Vector
            (
                Convert.ToSingle(values[0]),
                Convert.ToSingle(values[1]),
                Convert.ToSingle(values[2])
            );

            // Check position
            Vector position = player.Position;

            if ((Math.Abs(position.m_X - originalPosition.m_X) > 0.01f ||
                 Math.Abs(position.m_Y - originalPosition.m_Y) > 0.01f ||
                 Math.Abs(position.m_Z - originalPosition.m_Z) > 0.01f) ||
                !CanMeditate(player) ||
                !player.IsValid)
            {
                player.IsBusy = false;
                _customEffect.RemovePCCustomEffect(player, CustomEffectType.Meditate);
                return;
            }

            player.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_MEDITATE, 1.0f, 6.1f);
            });
            
            if (meditateTick >= 6)
            {
                int amount = CalculateAmount(player);

                _ability.RestoreFP(player, amount);
                Effect vfx = _.EffectVisualEffect(VFX_IMP_HEAD_MIND);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, player);
                meditateTick = 0;
            }

            oTarget.SetLocalInt("MEDITATE_TICK", meditateTick);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            NWPlayer player = oTarget.Object;
            player.IsBusy = false;
            player.DeleteLocalInt("MEDITATE_TICK");
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
            amount += _playerStat.EffectiveMeditateBonus(player);

            return amount;
        }

        public static bool CanMeditate(NWPlayer oPC)
        {
            bool canMeditate = !oPC.IsInCombat;

            NWArea pcArea = oPC.Area;
            foreach (NWPlayer member in oPC.PartyMembers)
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
