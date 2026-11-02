using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>The Gathering style Careful Hands. The Prospecting and Search windows apply it when the gatherer picks the style.</summary>
    public sealed class CarefulHandsStatusEffect : StatusEffectBase
    {
        public override string Name => "Careful Hands";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.CraftingStyle;
        public override EffectIconType Icon => EffectIconType.CarefulHandsStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public CarefulHandsStatusEffect()
        {
            foreach (var (stat, value) in GatheringStyles.StatsOf(GatheringStyle.CarefulHands))
                StatGroup.Stats[stat] = value;
        }
    }
}
