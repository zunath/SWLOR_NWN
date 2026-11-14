using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A pilot used a hostile space ability.
    /// </summary>
    public class SpaceAbilityEvent
    {
        public uint Activator { get; set; }
        public uint Target { get; set; }
        public AbilityDetail Ability { get; set; }
        public AbilityImpactSummary Impact { get; set; }
    }
}
