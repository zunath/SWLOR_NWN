using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public class PlayerAuraDetail
    {
        public StatusEffectType Type { get; set; }
        public bool TargetsSelf { get; set; }
        public bool TargetsParty { get; set; }
        public bool TargetsNPCs { get; set; }

        public PlayerAuraDetail(StatusEffectType type, bool targetsSelf, bool targetsParty, bool targetsNPCs)
        {
            Type = type;
            TargetsSelf = targetsSelf;
            TargetsParty = targetsParty;
            TargetsNPCs = targetsNPCs;
        }
    }
}
