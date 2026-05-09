using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class CombatEnhancement1StatusEffect : AbilityEnhancementStatusEffectBase
    {
        public override string Name => "Combat Enhancement I";
        public override EffectIconType Icon => EffectIconType.AbilityIncreaseSTR;

        public CombatEnhancement1StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Might] = 1;
            StatGroup.Abilities[AbilityType.Perception] = 1;
            StatGroup.Abilities[AbilityType.Vitality] = 1;
        }
    }
}
