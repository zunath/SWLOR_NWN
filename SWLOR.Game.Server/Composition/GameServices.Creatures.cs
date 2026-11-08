using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Creature AI profiles, flags and the creature event handlers (the static class <c>AI</c> until P2-ZO).</summary>
        public static IAIService AI => FacadeAccessor.Resolve<IAIService>();

        /// <summary>The mode, orders and abilities of a controlled companion (the static class <c>CompanionControl</c> until P2-ZO).</summary>
        public static ICompanionControlService CompanionControl => FacadeAccessor.Resolve<ICompanionControlService>();

        /// <summary>The NPC group catalog (the static class <c>NPCGroup</c> until P2-ZO).</summary>
        public static INPCGroupService NPCGroup => FacadeAccessor.Resolve<INPCGroupService>();

        /// <summary>Spawn tables, area spawning, despawning and respawn queues (the static class <c>Spawn</c> until P2-ZO).</summary>
        public static ISpawnService Spawn => FacadeAccessor.Resolve<ISpawnService>();
    }
}
