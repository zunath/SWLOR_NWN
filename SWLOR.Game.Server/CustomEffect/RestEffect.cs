using System;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class RestEffect : ICustomEffect
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly ICustomEffectService _customEffect;

        public RestEffect(
            INWScript script,
            IPerkService perk,
            ICustomEffectService customEffect)
        {
            _ = script;
            _customEffect = customEffect;
            _perk = perk;
        }

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

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            NWPlayer player = oTarget.Object;
            int restTick = oTarget.GetLocalInt("REST_TICK") + 1;

            if (restTick >= 6)
            {
                restTick = 0;

                int amount = CalculateAmount(player);
                string[] values = data.Split(',');
                Vector originalPosition = _.Vector
                (
                    Convert.ToSingle(values[0]),
                    Convert.ToSingle(values[1]),
                    Convert.ToSingle(values[2])
                );

                RunRest(player, originalPosition, amount);

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
            int perkLevel = _perk.GetPCPerkLevel(player, PerkType.Rest);
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
            amount += player.EffectiveRestBonus;

            return amount;
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
                _customEffect.RemovePCCustomEffect(oPC, CustomEffectType.Rest);
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
        }

        public static bool CanRest(NWPlayer oPC)
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
