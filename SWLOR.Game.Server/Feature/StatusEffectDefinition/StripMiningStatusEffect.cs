using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>The Gathering style Strip Mining. The Prospecting window applies it when the harvester picks the style.</summary>
    public sealed class StripMiningStatusEffect : StatusEffectBase
    {
        public override string Name => "Strip Mining";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.CraftingStyle;
        public override EffectIconType Icon => EffectIconType.StripMiningStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public StripMiningStatusEffect()
        {
            foreach (var (stat, value) in GatheringStyles.StatsOf(GatheringStyle.StripMining))
                StatGroup.Stats[stat] = value;
        }
    }
}
