using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The equipment removal guards (the facade <c>Service.EquipmentUnequip</c> until P2-ZJ).</summary>
        public static IEquipmentUnequipService EquipmentUnequip => FacadeAccessor.Resolve<IEquipmentUnequipService>();

        /// <summary>Items: use actions, equip and use limits, inventory checks and item text (the facade <c>Service.Item</c> until P2-ZJ).</summary>
        public static IItemService Item => FacadeAccessor.Resolve<IItemService>();

        /// <summary>The item trash audit (the facade <c>Service.ItemTrash</c> until P2-ZJ).</summary>
        public static IItemTrashService ItemTrash => FacadeAccessor.Resolve<IItemTrashService>();

        /// <summary>The key items (the facade <c>Service.KeyItem</c> until P2-ZJ).</summary>
        public static IKeyItemService KeyItem => FacadeAccessor.Resolve<IKeyItemService>();

        /// <summary>The lightsaber workbench (the facade <c>Service.LightsaberWorkbench</c> until P2-ZJ).</summary>
        public static ILightsaberWorkbenchService LightsaberWorkbench => FacadeAccessor.Resolve<ILightsaberWorkbenchService>();

        /// <summary>Loot tables, carried loot and corpses (the facade <c>Service.Loot</c> until P2-ZJ).</summary>
        public static ILootService Loot => FacadeAccessor.Resolve<ILootService>();

        /// <summary>Overflow storage for items a character upgrade could not keep (the facade <c>Service.OverflowStorage</c> until P2-ZJ).</summary>
        public static IOverflowStorageService OverflowStorage => FacadeAccessor.Resolve<IOverflowStorageService>();

        /// <summary>The saved outfits (the facade <c>Service.Wardrobe</c> until P2-ZJ).</summary>
        public static IWardrobeService Wardrobe => FacadeAccessor.Resolve<IWardrobeService>();
    }
}
