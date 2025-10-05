namespace SWLOR.Shared.Domain.Inventory
{
    internal class InventoryScriptName
    {
        // Inventory events
        public const string OnInventoryOpenBefore = "inv_open_bef";
        public const string OnInventoryOpenAfter = "inv_open_aft";
        public const string OnInventorySelectPanelBefore = "inv_panel_bef";
        public const string OnInventorySelectPanelAfter = "inv_panel_aft";
        public const string OnInventoryAddItemBefore = "inv_add_bef";
        public const string OnInventoryAddItemAfter = "inv_add_aft";
        public const string OnInventoryRemoveItemBefore = "inv_rem_bef";
        public const string OnInventoryRemoveItemAfter = "inv_rem_aft";
        public const string OnInventoryAddGoldBefore = "add_gold_bef";
        public const string OnInventoryAddGoldAfter = "add_gold_aft";
        public const string OnInventoryRemoveGoldBefore = "rem_gold_bef";
        public const string OnInventoryRemoveGoldAfter = "rem_gold_aft";
        public const string OnTrashOpened = "trash_opened";
        public const string OnTrashClosed = "trash_closed";
        public const string OnTrashDisturbed = "trash_disturbed";
        public const string OnSpeederHook = "speeder_hook";
        public const string OnOpenTrainingStore = "open_train_store";
        public const string OnHarvesterUsed = "harvester_used";
        public const string OnGetKeyItem = "get_key_item";
        public const string OnCorpseDisturbed = "corpse_disturbed";
        public const string OnCorpseClosed = "corpse_closed";
        public const string OnOpenBank = "open_bank";
    }
}
