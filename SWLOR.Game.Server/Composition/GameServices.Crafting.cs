using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Which minigame a craft runs (the facade <c>Service.CraftService.Minigame.CraftMinigameRouting</c> until P2-ZK).</summary>
        public static ICraftMinigameRoutingService CraftMinigameRouting => FacadeAccessor.Resolve<ICraftMinigameRoutingService>();

        /// <summary>The quick craft batches (the facade <c>Service.CraftService.Minigame.CraftQuickCraft</c> until P2-ZK).</summary>
        public static ICraftQuickCraftService CraftQuickCraft => FacadeAccessor.Resolve<ICraftQuickCraftService>();

        /// <summary>The research experiment minigame (the facade <c>Service.CraftService.Research.CraftResearchExperiment</c> until P2-ZK).</summary>
        public static ICraftResearchExperimentService CraftResearchExperiment => FacadeAccessor.Resolve<ICraftResearchExperimentService>();

        /// <summary>Starting, finishing and settling a craft (the facade <c>Service.CraftService.Minigame.CraftSessions</c> until P2-ZK).</summary>
        public static ICraftSessionsService CraftSessions => FacadeAccessor.Resolve<ICraftSessionsService>();

        /// <summary>Applying the selected crafting styles (the facade <c>Service.CraftService.CraftStyleEffects</c> until P2-ZK).</summary>
        public static ICraftStyleEffectsService CraftStyleEffects => FacadeAccessor.Resolve<ICraftStyleEffectsService>();

        /// <summary>Recipes, blueprints, enhancements and the crafting devices (the facade <c>Service.Craft</c> until P2-ZK).</summary>
        public static ICraftService Craft => FacadeAccessor.Resolve<ICraftService>();

        /// <summary>The harvest nodes engine world (the facade <c>Service.Prospecting</c> until P2-ZK).</summary>
        public static IProspectingService Prospecting => FacadeAccessor.Resolve<IProspectingService>();

        /// <summary>The Prospecting sessions (the facade <c>Service.ProspectingService.ProspectingSessions</c> until P2-ZK).</summary>
        public static IProspectingSessionsService ProspectingSessions => FacadeAccessor.Resolve<IProspectingSessionsService>();

        /// <summary>The scavenge sites engine world (the facade <c>Service.Scavenge</c> until P2-ZK).</summary>
        public static IScavengeService Scavenge => FacadeAccessor.Resolve<IScavengeService>();

        /// <summary>The scavenge visits (the facade <c>Service.ScavengeService.ScavengeSessions</c> until P2-ZK).</summary>
        public static IScavengeSessionsService ScavengeSessions => FacadeAccessor.Resolve<IScavengeSessionsService>();

        /// <summary>The register of the slicing attempts in progress (the facade <c>Service.Slicing</c> until P2-ZK).</summary>
        public static ISlicingService Slicing => FacadeAccessor.Resolve<ISlicingService>();

        /// <summary>Starting, playing and ending slicing attempts (the facade <c>Service.SlicingService.SlicingSession</c> until P2-ZK).</summary>
        public static ISlicingSessionService SlicingSession => FacadeAccessor.Resolve<ISlicingSessionService>();
    }
}
