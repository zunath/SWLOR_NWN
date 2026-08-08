using SWLOR.Toolset.Domain.Render.Icons;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Workspace
{
    /// <summary>
    /// Everything <see cref="ThumbnailService"/> asks of a renderer: turn a thing into pixels, and
    /// say how old the game data behind those pixels is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interface exists so the cache can be tested. <see cref="ThumbnailService"/> is the layer
    /// that coalesces concurrent requests for one key, decides when a cached image has been
    /// invalidated out from under an in-flight render, and falls through memory to disk to a real
    /// render — which is where its bugs live, and none of it could be exercised without a resolved
    /// NWN install, a hak stack, and several seconds per image.
    /// </para>
    /// <para>
    /// <see cref="BlueprintPreviewRenderer"/> is the only production implementation. A renderer that
    /// reports itself unavailable makes the whole cache a no-op, which is exactly what happens when
    /// the repository layout does not resolve.
    /// </para>
    /// </remarks>
    public interface IPreviewImageSource
    {
        /// <summary>True when game data is loaded well enough to produce any image at all.</summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Coarse version of every game-data dependency, so a cached image on disk can be told from
        /// one rendered against a since-changed hak stack.
        /// </summary>
        DateTime ContentVersionUtc { get; }

        /// <summary>A blueprint's preview. Null means "no artwork", not "failed".</summary>
        IconImage? Render(ResourceType type, string resRef, bool useIndexedBlueprint = false);

        /// <summary>A model by resref, with no blueprint involved — how a tile gets its picture.</summary>
        IconImage? RenderModel(string modelResRef, bool renderDoorTransitionFallback = false);

        /// <summary>
        /// A multi-tile palette group laid out on the grid, so its thumbnail shows the footprint it
        /// stamps rather than only its first tile. One model resref per slot, row-major.
        /// </summary>
        IconImage? RenderTileGroup(IReadOnlyList<string> slotModelResRefs, int columns, int rows);

        /// <summary>One <c>appearance.2da</c> row, for the creature appearance grid.</summary>
        IconImage? RenderCreatureAppearance(int appearanceId);
    }
}
