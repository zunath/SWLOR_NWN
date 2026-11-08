using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The DM battle grids of the areas (the facade <c>Service.BattleGrid</c> until P2-ZG).</summary>
        public static IBattleGridService BattleGrid => FacadeAccessor.Resolve<IBattleGridService>();

        /// <summary>The combat points players earn against NPCs (the facade <c>Service.CombatPoint</c> until P2-ZG).</summary>
        public static ICombatPointService CombatPoint => FacadeAccessor.Resolve<ICombatPointService>();

        /// <summary>
        /// The DM-toggled combat recorder (the facade <c>Service.CombatRecorder</c> until P2-ZG). The hooks read
        /// <c>CombatRecorder.IsRecording</c> first, through this property, which reads the same per-service slot the facade read.
        /// </summary>
        public static ICombatRecorderService CombatRecorder => FacadeAccessor.Resolve<ICombatRecorderService>();

        /// <summary>Player death, respawn and the respawn point (the facade <c>Service.Death</c> until P2-ZG).</summary>
        public static IDeathService Death => FacadeAccessor.Resolve<IDeathService>();

        /// <summary>The enmity tables and the rules that feed them (the facade <c>Service.Enmity</c> until P2-ZG).</summary>
        public static IEnmityService Enmity => FacadeAccessor.Resolve<IEnmityService>();

        /// <summary>Weapon poison coatings (the facade <c>Service.Poisons</c> until P2-ZG).</summary>
        public static IPoisonsService Poisons => FacadeAccessor.Resolve<IPoisonsService>();

        /// <summary>Player-vs-player combat (the facade <c>Service.PvpCombat</c> until P2-ZG).</summary>
        public static IPvpCombatService PvpCombat => FacadeAccessor.Resolve<IPvpCombatService>();

        /// <summary>Recast groups and cooldown deadlines (the facade <c>Service.Recast</c> until P2-ZG).</summary>
        public static IRecastService Recast => FacadeAccessor.Resolve<IRecastService>();

        /// <summary>Resistances and vulnerabilities (the facade <c>Service.Resistance</c> until P2-ZG).</summary>
        public static IResistanceService Resistance => FacadeAccessor.Resolve<IResistanceService>();

        /// <summary>Player targeting mode (the facade <c>Service.Targeting</c> until P2-ZG).</summary>
        public static ITargetingService Targeting => FacadeAccessor.Resolve<ITargetingService>();

        /// <summary>Timed stat modifiers a creature carries (the facade <c>Service.TemporaryStatModifier</c> until P2-ZG).</summary>
        public static ITemporaryStatModifierService TemporaryStatModifier => FacadeAccessor.Resolve<ITemporaryStatModifierService>();
    }
}
