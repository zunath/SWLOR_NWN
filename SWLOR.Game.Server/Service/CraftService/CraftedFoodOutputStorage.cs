using System;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.CraftService
{
    /// <summary>Owns neutral assembly storage for one completed food batch.</summary>
    internal sealed class CraftedFoodOutputStorage : IDisposable
    {
        public uint Container { get; }

        public CraftedFoodOutputStorage()
        {
            var anchor = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (!GetIsObjectValid(anchor))
                throw new InvalidOperationException("Food assembly storage is unavailable.");

            Container = CreateObject(ObjectType.Placeable, "craft_temp_store", GetLocation(anchor));
            if (!GetIsObjectValid(Container) || !GetHasInventory(Container))
            {
                if (GetIsObjectValid(Container))
                    DestroyObject(Container);
                throw new InvalidOperationException("Food assembly storage could not be created.");
            }
        }

        public void Dispose()
        {
            if (GetIsObjectValid(Container))
                DestroyObject(Container);
        }
    }
}
