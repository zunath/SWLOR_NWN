using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class LucidSpliceStatusEffect : StatusEffectBase
    {
        public override string Name => "Lucid Splice";
        public override EffectIconType Icon => EffectIconType.LucidSpliceStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public LucidSpliceStatusEffect()
        {
            StatGroup.Abilities[AbilityType.Willpower] = 2;
            StatGroup.Stats[StatType.MindResistance] = 50;
        }
    }
}
