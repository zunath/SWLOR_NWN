using System.Linq;
using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ShieldBoostEffect: ICustomEffect
    {
        private readonly INWScript _;
        private readonly ISkillService _skill;
        private readonly ICustomEffectService _customEffect;

        public ShieldBoostEffect(
            INWScript script,
            ISkillService skill,
            ICustomEffectService customEffect)
        {
            _ = script;
            _skill = skill;
            _customEffect = customEffect;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            _skill.ApplyStatChanges(oTarget.Object, null);
            int healAmount = (int)(_customEffect.CalculateEffectHPBonusPercent(oTarget.Object) * oTarget.MaxHP);

            if (healAmount > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(healAmount), oTarget);
            }

            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {

            // TODO: This is currently disabled because it causes data context sync issues. Will address this when I figure out what to do with that piece.

            //NWPlayer targetPlayer = oTarget.Object;

            //if (targetPlayer.Chest.CustomItemType != CustomItemType.HeavyArmor)
            //{
            //    _customEffect.RemovePCCustomEffect(targetPlayer, CustomEffectType.ShieldBoost);
            //    _skill.ApplyStatChanges(targetPlayer, null);

            //    var vfx = targetPlayer.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "SHIELD_BOOST_VFX");

            //    if (vfx != null)
            //    {
            //        _.RemoveEffect(targetPlayer, vfx);
            //    }
            //}
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            _skill.ApplyStatChanges(oTarget.Object, null);
        }
    }
}
