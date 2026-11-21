using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// How placed furniture adds item storage. Each piece gives its own storage (1 for most furniture, plus any Structure
    /// Bonus crafted into it). A layout with a <see cref="PropertyLayout.StoragePieceLimit"/> counts only its best pieces
    /// up to that many, so a ship can hold more furniture than it gets storage from: pieces past the limit decorate only.
    /// Which pieces count does not depend on the order they were placed, and the most storage a ship can reach is what it
    /// could before the furniture limits were raised.
    /// </summary>
    public static class FurnitureStorageRules
    {
        /// <summary>
        /// The storage the furniture gives together: every piece when <paramref name="storagePieceLimit"/> is 0 (no limit),
        /// otherwise the best <paramref name="storagePieceLimit"/> pieces.
        /// </summary>
        public static int GetFurnitureStorage(IEnumerable<int> pieceStorage, int storagePieceLimit)
        {
            var pieces = pieceStorage.Select(x => System.Math.Max(0, x));
            return storagePieceLimit <= 0
                ? pieces.Sum()
                : pieces.OrderByDescending(x => x).Take(storagePieceLimit).Sum();
        }

        /// <summary>The storage the furniture gains when a piece giving <paramref name="added"/> is placed.</summary>
        public static int GetStorageGained(IReadOnlyCollection<int> placedPieceStorage, int added, int storagePieceLimit) =>
            GetFurnitureStorage(placedPieceStorage.Append(added), storagePieceLimit) -
            GetFurnitureStorage(placedPieceStorage, storagePieceLimit);

        /// <summary>
        /// The storage the furniture loses when the piece at <paramref name="removedIndex"/> of
        /// <paramref name="placedPieceStorage"/> is picked up. A counted piece is replaced by the best uncounted one.
        /// </summary>
        public static int GetStorageLost(IReadOnlyList<int> placedPieceStorage, int removedIndex, int storagePieceLimit) =>
            GetFurnitureStorage(placedPieceStorage, storagePieceLimit) -
            GetFurnitureStorage(placedPieceStorage.Where((_, index) => index != removedIndex), storagePieceLimit);

        /// <summary>The furniture placed against its limit, for example "Furniture: 150 / 200".</summary>
        public static string DescribeFurniture(int furniture, int furnitureLimit) => $"Furniture: {furniture} / {furnitureLimit}";

        /// <summary>The items stored against the storage, for example "Item storage: 112 / 200 items".</summary>
        public static string DescribeItemStorage(int items, int storage) => $"Item storage: {items} / {storage} items";
    }
}
