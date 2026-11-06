using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static class SlicingSession
    {
        public const string BoardNumberVariable = Domain.Crafting.SlicingSessionService.BoardNumberVariable;
        // Retained only to map targets created before the checked-in board catalog.
        public const string SeedVariable = Domain.Crafting.SlicingSessionService.SeedVariable;
        public const string FailuresVariable = Domain.Crafting.SlicingSessionService.FailuresVariable;
        public const string IntegrityVariable = Domain.Crafting.SlicingSessionService.IntegrityVariable;
        public const string TierVariable = Domain.Crafting.SlicingSessionService.TierVariable;
        public const string OwnerVariable = Domain.Crafting.SlicingSessionService.OwnerVariable;
        public const string OwnerTimestampVariable = Domain.Crafting.SlicingSessionService.OwnerTimestampVariable;
        public const string CommittedVariable = Domain.Crafting.SlicingSessionService.CommittedVariable;
        public const string ToolTypeVariable = Domain.Crafting.SlicingSessionService.ToolTypeVariable;
        public const string ToolTierVariable = Domain.Crafting.SlicingSessionService.ToolTierVariable;

        private static ISlicingSessionService Instance => FacadeAccessor.Resolve<ISlicingSessionService>();

        /// <summary>The player's slicing attempt in progress, or null.</summary>
        public static ActiveSlicingSession Get(uint player)
        {
            return Instance.Get(player);
        }

        /// <summary>
        /// Claims the target and begins an attempt. Returns false with the reason in <paramref name="error"/> when the player cannot
        /// slice it, when they already have an attempt, or when the abandoned attempt of an earlier claim destroyed the target.
        /// </summary>
        public static bool TryStart(uint player, uint target, SlicingSourceType source, int tier, out string error)
        {
            return Instance.TryStart(player, target, source, tier, out error);
        }

        /// <summary>The reason a player cannot begin slicing the target, or an empty string.</summary>
        public static string ValidateStart(uint player, uint target, SlicingSourceType source, int tier)
        {
            return Instance.ValidateStart(player, target, source, tier);
        }

        public static bool SelectTile(uint player, int index, out string error)
        {
            return Instance.SelectTile(player, index, out error);
        }

        public static bool RotateSelected(uint player, out string message)
        {
            return Instance.RotateSelected(player, out message);
        }

        public static bool SwapSelectedWith(uint player, int secondIndex, out string message)
        {
            return Instance.SwapSelectedWith(player, secondIndex, out message);
        }

        public static bool ActivateTool(uint player, uint item, out string message)
        {
            return Instance.ActivateTool(player, item, out message);
        }

        /// <summary>The tools in the player's inventory that can still be used on their attempt.</summary>
        public static IReadOnlyList<EligibleSlicingTool> GetEligibleTools(uint player)
        {
            return Instance.GetEligibleTools(player);
        }

        /// <summary>Ends the player's attempt. A committed attempt counts as a failure.</summary>
        public static void Abort(uint player)
        {
            Instance.Abort(player);
        }

        public static int GetFailures(uint target)
        {
            return Instance.GetFailures(target);
        }

        public static int GetIntegrity(uint target)
        {
            return Instance.GetIntegrity(target);
        }

        /// <summary>A player who leaves the module or the area abandons their attempt.</summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnPlayerExit()
        {
            Instance.OnPlayerExit();
        }

        /// <summary>A player who dies abandons their attempt and the window is closed.</summary>
        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnPlayerDeath()
        {
            Instance.OnPlayerDeath();
        }
    }
}
