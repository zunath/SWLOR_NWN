using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SereneFocusStatusEffect : StatusEffectBase
    {
        public override string Name => "Serene Focus";
        public override EffectIconType Icon => EffectIconType.SereneFocusStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        protected override void Tick(uint creature)
        {
            Stat.RestoreFP(creature, 1);
            Stat.RestoreStamina(creature, 1);
        }
    }
}
