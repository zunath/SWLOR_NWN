using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ShieldBoostEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ShieldBoost;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);
            int healAmount = (int)(CustomEffectService.CalculateEffectHPBonusPercent(oTarget.Object) * oTarget.MaxHP);

            if (healAmount > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(healAmount), oTarget);
            }

            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            NWPlayer targetPlayer = oTarget.Object;

            if (targetPlayer.Chest.CustomItemType != CustomItemType.HeavyArmor)
            {
                CustomEffectService.RemovePCCustomEffect(targetPlayer, CustomEffectType.ShieldBoost);
                PlayerStatService.ApplyStatChanges(targetPlayer, null);

                var vfx = targetPlayer.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "SHIELD_BOOST_VFX");

                if (vfx != null)
                {
                    _.RemoveEffect(targetPlayer, vfx);
                }
            }
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);
        }

        public string StartMessage => "Your shield power increases.";
        public string ContinueMessage => "";
        public string WornOffMessage => "Your shield power returns to normal.";
    }
}
