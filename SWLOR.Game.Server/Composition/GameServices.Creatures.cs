using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Creature AI profiles, flags and the creature event handlers (the static class <c>AI</c> until it was removed).</summary>
        public static IAIService AI => FacadeAccessor.Resolve<IAIService>();

        /// <summary>The mode, orders and abilities of a controlled companion (the static class <c>CompanionControl</c> until it was removed).</summary>
        public static ICompanionControlService CompanionControl => FacadeAccessor.Resolve<ICompanionControlService>();

        /// <summary>The registered companions of the companion control, their mode and whether their abilities are on.</summary>
        public static ICompanionControlStoreService CompanionControlStore => FacadeAccessor.Resolve<ICompanionControlStoreService>();

        /// <summary>The follow action and the mode position of a controlled companion.</summary>
        public static ICompanionPositioningService CompanionPositioning => FacadeAccessor.Resolve<ICompanionPositioningService>();

        /// <summary>The target a controlled companion may engage and the hostile abilities it may use on it.</summary>
        public static ICompanionTargetAuthorityService CompanionTargetAuthority => FacadeAccessor.Resolve<ICompanionTargetAuthorityService>();

        /// <summary>The NPC group catalog (the static class <c>NPCGroup</c> until it was removed).</summary>
        public static INPCGroupService NPCGroup => FacadeAccessor.Resolve<INPCGroupService>();

        /// <summary>Spawn tables, area spawning, despawning and respawn queues (the static class <c>Spawn</c> until it was removed).</summary>
        public static ISpawnService Spawn => FacadeAccessor.Resolve<ISpawnService>();

        /// <summary>The spawn tables, the registered spawns and the spawn reload carry.</summary>
        public static ISpawnRegistryService SpawnRegistry => FacadeAccessor.Resolve<ISpawnRegistryService>();

        /// <summary>Creating the object of a spawn and preparing DM-spawned creatures.</summary>
        public static ISpawnObjectService SpawnObject => FacadeAccessor.Resolve<ISpawnObjectService>();
    }
}
