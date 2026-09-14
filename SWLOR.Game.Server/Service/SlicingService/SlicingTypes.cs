using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SlicingService
{
    [Flags]
    public enum SlicingConnection
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    public enum SlicingTileType
    {
        Straight = 0,
        Corner = 1,
        Junction = 2,
        Cross = 3,
        Entry = 4,
        Core = 5,
        Blocker = 6,
        Corrupt = 7
    }

    public enum SlicingSourceType
    {
        Lockbox = 1,
        Terminal = 2
    }

    public enum SlicingToolType
    {
        Invalid = 0,
        RatchetBypassPin = 1,
        ReversibleServoKey = 2,
        PhaseShuntFork = 3,
        MnemonicTraceSplice = 4,
        NullSignatureLattice = 5,
        ContinuitySampler = 6,
        JunctionSpectrograph = 7,
        ForwardEchoDecoder = 8,
        RouteOverlayPrism = 9,
        CorePatternOracle = 10,
        TraceFuse = 11
    }

    public enum SlicingRewardCategory
    {
        Common = 1,
        Tool = 2,
        NamedItem = 3,
        Schematic = 4,
        FieldNote = 5
    }

    public sealed class SlicingTile
    {
        public SlicingTileType Type { get; set; }
        public int Orientation { get; set; }
        public int SolutionIndex { get; set; } = -1;
        public int RouteOrder { get; set; } = -1;
        public int SolutionOrientation { get; set; }
        public bool IsRouteRevealed { get; set; }
        public bool IsOrientationRevealed { get; set; }

        public SlicingTile Clone()
        {
            return new SlicingTile
            {
                Type = Type,
                Orientation = Orientation,
                SolutionIndex = SolutionIndex,
                RouteOrder = RouteOrder,
                SolutionOrientation = SolutionOrientation,
                IsRouteRevealed = IsRouteRevealed,
                IsOrientationRevealed = IsOrientationRevealed
            };
        }
    }

    public sealed class SlicingSwap
    {
        public int FirstIndex { get; init; }
        public int SecondIndex { get; init; }
    }

    public sealed class SlicingBoard
    {
        public int Tier { get; init; }
        public int BoardNumber { get; init; }
        public string BoardId => $"T{Tier}-{BoardNumber:000}";
        public int Width { get; init; }
        public int Height { get; init; }
        public int BaseTrace { get; init; }
        public int SolutionActionCost { get; init; }
        public List<SlicingTile> Tiles { get; init; } = new();
        public List<SlicingSwap> SolutionSwaps { get; init; } = new();

        public SlicingBoard Clone()
        {
            var clone = new SlicingBoard
            {
                Tier = Tier,
                BoardNumber = BoardNumber,
                Width = Width,
                Height = Height,
                BaseTrace = BaseTrace,
                SolutionActionCost = SolutionActionCost
            };

            foreach (var tile in Tiles)
            {
                clone.Tiles.Add(tile.Clone());
            }

            foreach (var swap in SolutionSwaps)
            {
                clone.SolutionSwaps.Add(new SlicingSwap
                {
                    FirstIndex = swap.FirstIndex,
                    SecondIndex = swap.SecondIndex
                });
            }

            return clone;
        }
    }

    public sealed class SlicingRewardEntry
    {
        public string Resref { get; init; }
        public string Name { get; init; }
        public int Tier { get; init; }
        public SlicingSourceType Source { get; init; }
        public SlicingRewardCategory Category { get; init; }
        public bool IsExceptional { get; init; }
        public bool IsNewDirectReward { get; init; } = true;
        public int Quantity { get; init; } = 1;
    }
}
