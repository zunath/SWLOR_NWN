using System.Collections.Generic;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Takes items out of a character's inventory so the removal is complete at once, for a character file that is saved in the same
    /// script (<see cref="CharacterCheckpoint"/>). <c>DestroyObject</c> is native-deferred until the script that called it returns, so
    /// an item destroyed and then saved in one script is still in the saved file. Whole items are therefore moved out into a holding
    /// container (the way a migration retires items, <c>MigrationItemDisposal</c>) and destroyed once the save is done, and a stack that
    /// is only cut is cut in place. Nothing is lost if the save fails: <see cref="Restore"/> puts the same items back.
    /// <para>
    /// Call <see cref="Destroy"/> after the save succeeded, or <see cref="Restore"/> after it failed. The two cannot both be skipped:
    /// the parked items stay in a holding container until one of them runs.
    /// </para>
    /// </summary>
    public sealed class InventoryRemoval
    {
        private const string StorageTag = "TEMP_ITEM_STORAGE";
        private const string HoldingResref = "craft_temp_store";

        private readonly uint _owner;
        private readonly List<uint> _parked = new();
        private readonly List<(uint Item, int Size)> _cut = new();
        private readonly List<uint> _containers = new();
        private uint _current = OBJECT_INVALID;

        public InventoryRemoval(uint owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Takes <paramref name="count"/> of the items with the blueprint from the owner's inventory (stack sizes included, bags not
        /// searched). False, with some of them possibly taken, when the owner carries fewer or an item could not be moved out; the
        /// caller then calls <see cref="Restore"/>.
        /// </summary>
        public bool TryTake(string resref, int count)
        {
            var items = new List<uint>();
            for (var item = GetFirstItemInInventory(_owner); GetIsObjectValid(item); item = GetNextItemInInventory(_owner))
            {
                if (GetResRef(item) == resref)
                    items.Add(item);
            }

            var taken = 0;
            foreach (var item in items)
            {
                if (taken >= count)
                    break;

                var size = GetItemStackSize(item);
                var use = System.Math.Min(size, count - taken);
                if (use >= size)
                {
                    if (!Park(item))
                        return false;
                }
                else
                {
                    Cut(item, size, size - use);
                }

                taken += use;
            }

            return taken >= count;
        }

        /// <summary>Takes one of the stack the owner holds, or the item itself when it is alone. False, taking nothing, when they no longer hold it or it could not be moved out.</summary>
        public bool TryUseUpOne(uint item)
        {
            if (!GetIsObjectValid(item) || GetItemPossessor(item) != _owner)
                return false;

            var size = GetItemStackSize(item);
            if (size > 1)
            {
                Cut(item, size, size - 1);
                return true;
            }

            return Park(item);
        }

        /// <summary>Puts back everything this removal took: stacks to their sizes, and the parked items to the owner. An item that will not go back goes to the owner's overflow store.</summary>
        public void Restore()
        {
            for (var index = _cut.Count - 1; index >= 0; index--)
            {
                var (item, size) = _cut[index];
                if (GetIsObjectValid(item))
                    SetItemStackSize(item, size);
            }

            foreach (var item in _parked)
            {
                if (!GetIsObjectValid(item))
                    continue;

                if (GetIsObjectValid(_owner) && ItemPlugin.MoveTo(item, _owner, true))
                    continue;

                var playerId = GetObjectUUID(_owner);
                Log.Write(LogGroup.Error, $"An item taken from player [{playerId}] could not be put back after a failed save, so it went to their overflow storage: {GetName(item)}.");
                FarmItems.StoreInOverflow(playerId, item, string.Empty);
            }

            Release();
        }

        /// <summary>Destroys what this removal parked. Call it once the character file was saved without the items.</summary>
        public void Destroy()
        {
            foreach (var item in _parked)
            {
                if (GetIsObjectValid(item))
                    DestroyObject(item);
            }

            Release();
        }

        private void Cut(uint item, int from, int to)
        {
            SetItemStackSize(item, to);
            _cut.Add((item, from));
        }

        /// <summary>Moves the item out of the owner's inventory into this removal's holding container, made when the first item needs it and again when it is full.</summary>
        private bool Park(uint item)
        {
            var storage = GetObjectByTag(StorageTag);
            if (!GetIsObjectValid(storage))
            {
                Log.Write(LogGroup.Error, "Temporary item storage is unavailable, so items could not be taken from a character.");
                return false;
            }

            if (!TryMove(item))
            {
                _current = CreateObject(ObjectType.Placeable, HoldingResref, GetLocation(storage));
                if (GetIsObjectValid(_current))
                    _containers.Add(_current);
                if (!TryMove(item))
                    return false;
            }

            _parked.Add(item);
            return true;
        }

        private bool TryMove(uint item)
        {
            return GetIsObjectValid(_current) && GetHasInventory(_current) &&
                   ItemPlugin.MoveTo(item, _current, true) && GetItemPossessor(item, true) == _current;
        }

        private void Release()
        {
            foreach (var container in _containers)
            {
                if (GetIsObjectValid(container))
                    DestroyObject(container);
            }

            _containers.Clear();
            _parked.Clear();
            _cut.Clear();
            _current = OBJECT_INVALID;
        }
    }
}
