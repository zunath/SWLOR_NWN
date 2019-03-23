using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using System;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.CustomEffect
{
    public class MeditateEffect : ICustomEffect
    {

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
                CustomEffectService.RemovePCCustomEffect(player, CustomEffectType.Meditate);
                return;
            }

            player.IsBusy = true;

            player.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_MEDITATE, 1.0f, 6.1f);
            });
            
            if (meditateTick >= 6)
            {
                int amount = CalculateAmount(player);

                AbilityService.RestoreFP(player, amount);
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
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int perkLevel = PerkService.GetPCPerkLevel(player, PerkType.Meditate);
            int amount;
            switch (perkLevel)
            {
                default:
                    amount = 2;
                    break;
                case 4:
                case 5:
                case 6:
                    amount = 3;
                    break;
                case 7:
                    amount = 4;
                    break;
            }
            amount += effectiveStats.Meditate;

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
