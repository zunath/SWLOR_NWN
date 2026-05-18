using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CircleOfHarmony1StatusEffect : StatusEffectBase
    {
        public override string Name => "Circle of Harmony";
        public override EffectIconType Icon => EffectIconType.CircleOfHarmony1StatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 3f;
        public override bool PersistsOnLogout => false;

        protected override void Tick(uint creature)
        {
            Stat.RestoreFP(creature, 1);
            Stat.RestoreStamina(creature, 1);
        }
    }
}
