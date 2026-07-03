using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceWardingStatusEffect : StatusEffectBase
    {
        private readonly int _forceDefense;

        public override string Name => "Force Warding";
        public override EffectIconType Icon => EffectIconType.ForceWardingStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override bool PersistsOnLogout => false;

        public ForceWardingStatusEffect()
            : this(5)
        {
        }

        public ForceWardingStatusEffect(int forceDefense)
        {
            _forceDefense = forceDefense;
            StatGroup.Stats[StatType.ForceDefense] = forceDefense;
        }

        public override IStatusEffect Clone()
        {
            return new ForceWardingStatusEffect(_forceDefense);
        }
    }
}
