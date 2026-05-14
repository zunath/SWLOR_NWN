using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AdrenalStimStatusEffect : StatusEffectBase
    {
        private readonly int _staminaPerTick;

        public override string Name => "Adrenal Stim";
        public override EffectIconType Icon => EffectIconType.Regenerate;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        public AdrenalStimStatusEffect()
        {
            _staminaPerTick = 0;
        }

        public AdrenalStimStatusEffect(int staminaPerTick)
        {
            _staminaPerTick = staminaPerTick;
        }

        protected override void Tick(uint creature)
        {
            if (_staminaPerTick <= 0)
                return;

            Stat.RestoreStamina(creature, _staminaPerTick);
        }
    }
}
