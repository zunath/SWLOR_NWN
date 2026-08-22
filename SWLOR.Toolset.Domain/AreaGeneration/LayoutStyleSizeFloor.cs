#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Smallest square area size each layout style reliably generates at. Measured empirically
    /// (40 single-attempt seeds per size per shipped profile): below these floors styles fail
    /// structurally (PackedRooms' BSP can't split an 8x8; RoomsAndCorridors can't place two rooms
    /// plus gaps below 10-11; OrganicCave needs 12+ before caves stop collapsing during smoothing).
    /// At the floor, single-attempt success is >=95%, which the standard 6-attempt retry turns into
    /// effective certainty. Consumers that offer size choices (Content Builder sliders, review
    /// specs, /genarea) must clamp to this floor so users are never offered a failing option.
    /// </summary>
    public static class LayoutStyleSizeFloor
    {
        public static int For(DungeonLayoutStyle style)
        {
            return style switch
            {
                DungeonLayoutStyle.OrganicCave => 12,
                DungeonLayoutStyle.Warren => 8,
                DungeonLayoutStyle.PackedRooms => 9,
                DungeonLayoutStyle.RoomsAndCorridors => 11,
                DungeonLayoutStyle.Labyrinth => 8,
                _ => 12
            };
        }
    }
}
