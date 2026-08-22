#nullable disable
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Layouts
{
    /// <summary>A rectangle in corner coordinates (inclusive on both ends), shared by rect-based room styles.</summary>
    internal readonly struct RoomRect
    {
        public RoomRect(int x0, int y0, int x1, int y1)
        {
            X0 = x0;
            Y0 = y0;
            X1 = x1;
            Y1 = y1;
        }

        public int X0 { get; }
        public int Y0 { get; }
        public int X1 { get; }
        public int Y1 { get; }

        public int CornerWidth => X1 - X0 + 1;
        public int CornerHeight => Y1 - Y0 + 1;
    }
}
