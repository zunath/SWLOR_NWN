using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class LeadershipAuraStatusEffectBase : AuraStatusEffectBase
    {
        private readonly StatType _stat;
        private readonly PerkType _perk;
        private readonly float _level1Scale;
        private readonly float _level2Scale;
        private readonly float _level3Scale;

        protected LeadershipAuraStatusEffectBase(
            StatType stat,
            PerkType perk,
            float level1Scale,
            float level2Scale,
            float level3Scale)
        {
            _stat = stat;
            _perk = perk;
            _level1Scale = level1Scale;
            _level2Scale = level2Scale;
            _level3Scale = level3Scale;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            if (!GetIsObjectValid(Source))
                return;

            var social = GetAbilityScore(Source, AbilityType.Social);
            StatGroup.Stats[_stat] = Perk.GetPerkLevel(Source, _perk) switch
            {
                1 => (int)(social * _level1Scale),
                2 => (int)(social * _level2Scale),
                3 => (int)(social * _level3Scale),
                _ => 0
            };
        }
    }
}
