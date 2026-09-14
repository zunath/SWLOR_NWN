using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    [StatConfiguredIcon]
    public sealed class IdleSkillAbilityReadyStatusEffect : StatusEffectBase
    {
        public override string Name => "Idle Skill Ability Ready";
        public override EffectIconType Icon => EffectIconType.SereneFocusStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;
    }
}
