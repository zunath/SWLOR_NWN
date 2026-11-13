using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Starts, plays and ends slicing attempts on lockboxes and terminals. Static callers reach
    /// the container's instance through <c>GameServices.SlicingSession</c>; the event handlers are on <see cref="SlicingSessionService"/>.
    /// </summary>
    public interface ISlicingSessionService
    {
        /// <summary>The player's slicing attempt in progress, or null.</summary>
        ActiveSlicingSession Get(uint player);

        /// <summary>
        /// Claims the target and begins an attempt. Returns false with the reason in <paramref name="error"/> when the player cannot
        /// slice it, when they already have an attempt, or when the abandoned attempt of an earlier claim destroyed the target.
        /// </summary>
        bool TryStart(uint player, uint target, SlicingSourceType source, int tier, out string error);

        /// <summary>The reason a player cannot begin slicing the target, or an empty string.</summary>
        string ValidateStart(uint player, uint target, SlicingSourceType source, int tier);

        bool SelectTile(uint player, int index, out string error);

        bool RotateSelected(uint player, out string message);

        bool SwapSelectedWith(uint player, int secondIndex, out string message);

        bool ActivateTool(uint player, uint item, out string message);

        /// <summary>The tools in the player's inventory that can still be used on their attempt.</summary>
        IReadOnlyList<EligibleSlicingTool> GetEligibleTools(uint player);

        /// <summary>Ends the player's attempt. A committed attempt counts as a failure.</summary>
        void Abort(uint player);

        int GetFailures(uint target);

        int GetIntegrity(uint target);

        /// <summary>A player who leaves the module or the area abandons their attempt.</summary>
        void OnPlayerExit();

        /// <summary>The work of <see cref="OnPlayerExit"/> for one player (a hot reload's quiescence step).</summary>
        void AbandonFor(uint player);

        /// <summary>A player who dies abandons their attempt and the window is closed.</summary>
        void OnPlayerDeath();
    }
}
