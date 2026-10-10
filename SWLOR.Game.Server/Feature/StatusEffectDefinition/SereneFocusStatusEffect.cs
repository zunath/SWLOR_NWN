using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SereneFocusStatusEffect : StatusEffectBase
    {
        private readonly bool _restoresStamina;

        public override string Name => "Serene Focus";
        public override EffectIconType Icon => EffectIconType.SereneFocusStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        public SereneFocusStatusEffect() : this(true)
        {
        }

        /// <summary>
        /// A caster focusing on themselves recovers FP only. Supporting someone else stays the
        /// stronger play without the caster being locked out of their own trait when solo.
        /// </summary>
        public SereneFocusStatusEffect(bool restoresStamina)
        {
            _restoresStamina = restoresStamina;
        }

        public override IStatusEffect Clone()
        {
            return new SereneFocusStatusEffect(_restoresStamina);
        }

        protected override void Tick(uint creature)
        {
            Stat.RestoreFP(creature, 1);

            if (_restoresStamina)
                Stat.RestoreStamina(creature, 1);
        }
    }
}
