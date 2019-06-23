using System;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.CustomEffect
{
    public class RestEffect : ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.Rest;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            NWPlayer player = oTarget.Object;

            player.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0f, 6.1f);
            });

            player.IsBusy = true;

            string data = $"{player.Position.m_X},{player.Position.m_Y},{player.Position.m_Z}";

            return data;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            AbilityService.EndConcentrationEffect(oCaster);

            NWPlayer player = oTarget.Object;
            int restTick = oTarget.GetLocalInt("REST_TICK") + 1;


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
                !CanRest(player) ||
                !player.IsValid)
            {
                player.IsBusy = false;
                CustomEffectService.RemovePCCustomEffect(player, CustomEffectType.Rest);
                return;
            }

            player.IsBusy = true;

            player.AssignCommand(() =>
            {
                _.ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0f, 6.1f);
            });

            if (restTick >= 6)
            {
                int amount = CalculateAmount(player);

                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(amount), player);
                Effect vfx = _.EffectVisualEffect(VFX_IMP_HEAD_HOLY);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, player);
                restTick = 0;
            }

            oTarget.SetLocalInt("REST_TICK", restTick);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            NWPlayer player = oTarget.Object;
            player.IsBusy = false;
        }

        private int CalculateAmount(NWPlayer player)
        {
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            int perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.Rest);
            int amount;
            switch (perkLevel)
            {
                default:
                    amount = 6;
                    break;
                case 4:
                case 5:
                case 6:
                    amount = 10;
                    break;
                case 7:
                    amount = 14;
                    break;
            }
            amount += effectiveStats.Rest;

            return amount;
        }
        
        public static bool CanRest(NWCreature creature)
        {
            bool canRest = !creature.IsInCombat;

            NWArea pcArea = creature.Area;
            foreach (NWCreature member in creature.PartyMembers)
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

        public string StartMessage => "You begin to rest...";
        public string ContinueMessage => "";
        public string WornOffMessage => "You stop resting.";
    }
}
