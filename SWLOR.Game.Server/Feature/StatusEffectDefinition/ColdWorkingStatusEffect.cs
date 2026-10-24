using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>The Smithery Plasma Forge style Cold Working. The Plasma Forge window applies it when the crafter picks the style.</summary>
    public sealed class ColdWorkingStatusEffect : StatusEffectBase
    {
        public override string Name => "Cold Working";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override StatusEffectSourceType SourceType => StatusEffectSourceType.CraftingStyle;
        public override EffectIconType Icon => EffectIconType.ColdWorkingStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public ColdWorkingStatusEffect()
        {
            foreach (var (stat, value) in ForgeStyles.StatsOf(ForgeStyle.ColdWorking))
                StatGroup.Stats[stat] = value;
        }
    }
}
