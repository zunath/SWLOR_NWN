using System.Linq;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.CustomEffect.Contracts;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.CustomEffect
{
    public class ShieldBoostEffect: ICustomEffectHandler
    {
        public CustomEffectCategoryType CustomEffectCategoryType => CustomEffectCategoryType.NormalEffect;
        public CustomEffectType CustomEffectType => CustomEffectType.ShieldBoost;

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);
            var healAmount = (int)(CustomEffectService.CalculateEffectHPBonusPercent(oTarget.Object) * oTarget.MaxHP);

            if (healAmount > 0)
            {
                NWScript.ApplyEffectToObject(DurationType.Instant, NWScript.EffectHeal(healAmount), oTarget);
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

                var vfx = targetPlayer.Effects.SingleOrDefault(x => NWScript.GetEffectTag(x) == "SHIELD_BOOST_VFX");

                if (vfx != null)
                {
                    NWScript.RemoveEffect(targetPlayer, vfx);
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
