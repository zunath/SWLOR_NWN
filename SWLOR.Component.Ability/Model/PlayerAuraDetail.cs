using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Component.Ability.Model
{
    public class PlayerAuraDetail
    {
        public StatusEffectType Type { get; set; }
        public bool TargetsSelf { get; set; }
        public bool TargetsParty { get; set; }
        public bool TargetsEnemies { get; set; }

        public PlayerAuraDetail(StatusEffectType type, bool targetsSelf, bool targetsParty, bool targetsEnemies)
        {
            Type = type;
            TargetsSelf = targetsSelf;
            TargetsParty = targetsParty;
            TargetsEnemies = targetsEnemies;
        }
    }
}
