using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SlicingService
{
    public static partial class SlicingBoardCatalog
    {
        public static int GetBoardCount(int tier)
        {
            if (!_encodedBoards.TryGetValue(tier, out var boards))
                throw new ArgumentOutOfRangeException(nameof(tier), "Slicing tier must be between 1 and 5.");
            return boards.Length;
        }

        public static SlicingBoard GetBoard(int tier, int boardNumber)
        {
            if (!_encodedBoards.TryGetValue(tier, out var boards))
                throw new ArgumentOutOfRangeException(nameof(tier), "Slicing tier must be between 1 and 5.");
            if (boardNumber < 1 || boardNumber > boards.Length)
                throw new ArgumentOutOfRangeException(nameof(boardNumber),
                    $"Tier {tier} slicing board number must be between 1 and {boards.Length}.");

            return Decode(tier, boardNumber, boards[boardNumber - 1]);
        }

        private static SlicingBoard Decode(int tier, int boardNumber, string encoded)
        {
            var sections = encoded.Split('|');
            if (sections.Length != 4)
                throw InvalidCatalogEntry(tier, boardNumber, "entry must contain four sections");

            var dimensions = ParsePair(sections[0], tier, boardNumber, "dimensions");
            var costs = ParsePair(sections[1], tier, boardNumber, "costs");
            var board = new SlicingBoard
            {
                Tier = tier,
                BoardNumber = boardNumber,
                Width = dimensions.First,
                Height = dimensions.Second,
                BaseTrace = costs.First,
                SolutionActionCost = costs.Second
            };

            foreach (var encodedTile in sections[2].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var fields = encodedTile.Split(',');
                if (fields.Length != 5 ||
                    !int.TryParse(fields[0], out var type) ||
                    !int.TryParse(fields[1], out var orientation) ||
                    !int.TryParse(fields[2], out var solutionIndex) ||
                    !int.TryParse(fields[3], out var routeOrder) ||
                    !int.TryParse(fields[4], out var solutionOrientation))
                {
                    throw InvalidCatalogEntry(tier, boardNumber, $"invalid tile '{encodedTile}'");
                }

                board.Tiles.Add(new SlicingTile
                {
                    Type = (SlicingTileType)type,
                    Orientation = orientation,
                    SolutionIndex = solutionIndex,
                    RouteOrder = routeOrder,
                    SolutionOrientation = solutionOrientation
                });
            }

            if (!string.IsNullOrWhiteSpace(sections[3]))
            {
                foreach (var encodedSwap in sections[3].Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var swap = ParsePair(encodedSwap, tier, boardNumber, "swap");
                    board.SolutionSwaps.Add(new SlicingSwap
                    {
                        FirstIndex = swap.First,
                        SecondIndex = swap.Second
                    });
                }
            }

            if (board.Tiles.Count != board.Width * board.Height)
                throw InvalidCatalogEntry(tier, boardNumber,
                    $"expected {board.Width * board.Height} tiles but decoded {board.Tiles.Count}");

            return board;
        }

        private static (int First, int Second) ParsePair(
            string encoded,
            int tier,
            int boardNumber,
            string section)
        {
            var values = encoded.Split(',');
            if (values.Length != 2 ||
                !int.TryParse(values[0], out var first) ||
                !int.TryParse(values[1], out var second))
            {
                throw InvalidCatalogEntry(tier, boardNumber, $"invalid {section} '{encoded}'");
            }

            return (first, second);
        }

        private static InvalidOperationException InvalidCatalogEntry(int tier, int boardNumber, string detail) =>
            new($"Slicing board T{tier}-{boardNumber:000} is malformed: {detail}.");
    }
}
