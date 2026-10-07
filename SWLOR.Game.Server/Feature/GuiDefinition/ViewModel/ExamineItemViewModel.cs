using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ExamineItemViewModel: GuiViewModelBase<ExamineItemViewModel, ExamineItemPayload>,
        IGuiRefreshable<EquipItemRefreshEvent>, IGuiRefreshable<UnequipItemRefreshEvent>,
        IGuiRefreshable<PerkAcquiredRefreshEvent>, IGuiRefreshable<PerkRefundedRefreshEvent>,
        IGuiRefreshable<StatAdjustmentRefreshEvent>
    {
        public const string ContentElement = "examine_item_content";
        public const string ContentPartial = "EXAMINE_ITEM_BODY";
        private ExamineItemPayload _payload;

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Description
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ItemProperties
        {
            get => Get<string>();
            set => Set(value);
        }

        /// <summary>
        /// Replaces the native item examine panel, which is disabled for players on login.
        /// </summary>
        public static void ShowExamineWindow(uint viewer, uint item)
        {
            if (GetObjectType(item) != ObjectType.Item)
                return;

            // The native examine events do not run for a disabled panel, so apply their item details here.
            EquipmentRestrictions.MarkLegacyOffHandPistol(item);
            Space.ApplyExamineDetails(item);

            // A viewer-specific window avoids persisting personal stats on a shared item.
            Gui.ClosePlayerWindow(viewer, GuiWindowType.ExamineItem);
            Gui.TogglePlayerWindow(viewer, GuiWindowType.ExamineItem, new ExamineItemPayload(item, trackLiveItem: true));
        }

        protected override void Initialize(ExamineItemPayload initialPayload)
        {
            _payload = initialPayload;
            WindowTitle = initialPayload.ItemName;
            ItemProperties = initialPayload.ItemProperties;
            RefreshDescription();
            ChangePartialView(ContentElement, ContentPartial);
        }

        private void RefreshDescription()
        {
            if (_payload == null)
                return;
            var itemDMG = _payload.ItemDMG;
            var hasItemDMG = _payload.HasItemDMG;
            var hasLiveItem = GetIsObjectValid(_payload.ItemObject) && GetObjectUUID(_payload.ItemObject) == _payload.ItemId;
            if (hasLiveItem && _payload.IsIdentified)
            {
                itemDMG = Item.GetDMG(_payload.ItemObject);
                hasItemDMG = GetItemHasItemProperty(_payload.ItemObject, ItemPropertyType.DMG);
                ItemProperties = Item.BuildItemPropertyString(_payload.ItemObject);
            }
            Description = _payload.Description;
            if (!_payload.IsIdentified || !WeaponDamage.IsSingleWeaponType(_payload.ItemType))
                return;

            var preview = WeaponDamage.BuildSingleWeaponDescription(itemDMG,
                WeaponDamage.GetNaturalSingleWeaponPercent(Player),
                Stat.GetStatAdjustment(Player, StatType.SingleWeaponDamagePercentAdjustment), hasItemDMG);
            Description = string.IsNullOrWhiteSpace(Description) ? preview : $"{Description.TrimEnd()}\n\n{preview}";
            if (hasItemDMG && hasLiveItem && GetItemInSlot(InventorySlot.RightHand, Player) == _payload.ItemObject)
                Description += EquipmentPredicates.HasSingleWeapon(Player)
                    ? "\nSingle Weapon is active."
                    : "\nSingle Weapon is inactive: your off hand must be empty.";
        }

        public void Refresh(EquipItemRefreshEvent payload) => RefreshDescription();
        public void Refresh(UnequipItemRefreshEvent payload) => RefreshDescription();
        public void Refresh(PerkAcquiredRefreshEvent payload) => RefreshDescription();
        public void Refresh(PerkRefundedRefreshEvent payload) => RefreshDescription();
        public void Refresh(StatAdjustmentRefreshEvent payload) => RefreshDescription();
    }
}
