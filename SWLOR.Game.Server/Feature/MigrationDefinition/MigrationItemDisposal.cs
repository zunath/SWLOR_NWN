using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// Detaches retired items immediately and owns disposal containers for one
    /// migration pass. Native destruction runs only after the script returns.
    /// </summary>
    internal sealed class MigrationItemDisposal : IDisposable
    {
        private readonly List<uint> _containers = new();
        private uint _current = OBJECT_INVALID;
        private bool _disposed;

        /// <summary>
        /// Removes an item from the inventory being saved, rotating to an empty
        /// container when the current container cannot accept another item.
        /// </summary>
        public void Remove(uint item)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var possessor = GetItemPossessor(item, true);
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (GetIsObjectValid(possessor) && possessor != storage)
            {
                if (!GetIsObjectValid(storage))
                    throw new InvalidOperationException("Migration item storage is unavailable.");

                if (!TryMove(item))
                {
                    _current = CreateObject(ObjectType.Placeable, "craft_temp_store", GetLocation(storage));
                    if (GetIsObjectValid(_current)) _containers.Add(_current);
                    if (!TryMove(item))
                        throw new InvalidOperationException("Could not remove a retired item from its migration container.");
                }
            }
            DestroyObject(item);
        }

        /// <summary>Verifies native ownership after an immediate inventory transfer.</summary>
        private bool TryMove(uint item) =>
            GetIsObjectValid(_current) && GetHasInventory(_current) &&
            ItemPlugin.MoveTo(item, _current, true) && GetItemPossessor(item, true) == _current;

        /// <summary>Schedules every container owned by this pass for cleanup, even after failure.</summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var container in _containers)
                if (GetIsObjectValid(container)) DestroyObject(container);
            _containers.Clear();
            _current = OBJECT_INVALID;
        }
    }
}
