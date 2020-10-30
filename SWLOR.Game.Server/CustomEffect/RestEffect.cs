using System;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

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
                NWScript.ActionPlayAnimation(Animation.LoopingSitCross, 1.0f, 6.1f);
            });

            player.IsBusy = true;

            var data = $"{player.Position.X},{player.Position.Y},{player.Position.Z}";

            return data;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            AbilityService.EndConcentrationEffect(oCaster);

            NWPlayer player = oTarget.Object;
            var restTick = oTarget.GetLocalInt("REST_TICK") + 1;


            // Pull original position from data
            var values = data.Split(',');
            var originalPosition = NWScript.Vector3
            (
                Convert.ToSingle(values[0]),
                Convert.ToSingle(values[1]),
                Convert.ToSingle(values[2])
            );

            // Check position
            var position = player.Position;

            if ((Math.Abs(position.X - originalPosition.X) > 0.01f ||
                 Math.Abs(position.Y - originalPosition.Y) > 0.01f ||
                 Math.Abs(position.Z - originalPosition.Z) > 0.01f) ||
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
                NWScript.ActionPlayAnimation(Animation.LoopingSitCross, 1.0f, 6.1f);
            });

            if (restTick >= 6)
            {
                var amount = CalculateAmount(player);

                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(amount), player);
                var vfx = NWScript.EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Holy);
                NWScript.ApplyEffectToObject(DurationType.Instant, vfx, player);
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
            var perkLevel = PerkService.GetCreaturePerkLevel(player, PerkType.Rest);
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
            var canRest = !creature.IsInCombat;

            var pcArea = creature.Area;
            foreach (var member in creature.PartyMembers)
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
