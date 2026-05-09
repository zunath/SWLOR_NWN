using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceInspiration3StatusEffect : AbilityEnhancementStatusEffectBase
    {
        public override string Name => "Force Inspiration III";
        public override EffectIconType Icon => EffectIconType.AbilityIncreaseWIS;

        public ForceInspiration3StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Willpower] = 3;
            StatGroup.Abilities[AbilityType.Agility] = 3;
            StatGroup.Abilities[AbilityType.Might] = 3;
        }
    }
}
