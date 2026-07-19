using System;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Display-only effect for the Evasion buff Lateral Footwork grants after a Spear ability.
    /// The Evasion itself lives in TemporaryStatModifier, so this must not carry StatGroup stats.
    /// </summary>
    public sealed class LateralFootworkStatusEffect : StatusEffectBase
    {
        private readonly int _evasionPercent;

        public override string Name => $"Lateral Footwork (+{_evasionPercent}% Evasion)";
        public override EffectIconType Icon => EffectIconType.LateralFootworkStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;

        public LateralFootworkStatusEffect()
            : this(1)
        {
        }

        public LateralFootworkStatusEffect(int evasionPercent)
        {
            _evasionPercent = Math.Max(1, evasionPercent);
        }

        public override IStatusEffect Clone()
        {
            return new LateralFootworkStatusEffect(_evasionPercent);
        }
    }
}
