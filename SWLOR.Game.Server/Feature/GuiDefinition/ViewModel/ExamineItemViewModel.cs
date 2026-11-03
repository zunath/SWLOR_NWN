using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
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
        private ExamineItemPayload _payload;
        private string _renderedFacts;
        private string _renderedBody;
        private string _renderedIcon;
        private string _contentLayout;
        private bool _isContentApplied;

        public string WindowTitle
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor TitleColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string Facts { get; private set; }

        public string Body { get; private set; }

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
            _isContentApplied = false;
            WindowTitle = ExamineItemText.PlainText(initialPayload.ItemName);
            TitleColor = ExamineItemText.TitleColor(initialPayload.ItemName);
            RefreshContent();
        }

        private void RefreshContent()
        {
            if (_payload == null)
                return;
            var hasLiveItem = GetIsObjectValid(_payload.ItemObject) && GetObjectUUID(_payload.ItemObject) == _payload.ItemId;
            if (hasLiveItem)
                _payload.Capture(_payload.ItemObject);

            Facts = ExamineItemSheet.BuildFacts(_payload.BaseItemName, _payload.StackSize, _payload.Charges, _payload.Weight, _payload.IsIdentified);
            Body = ExamineItemSheet.BuildBody(_payload.Description, _payload.ItemProperties, _payload.IsIdentified, BuildWeaponDetails(hasLiveItem));
            ApplyContent();
        }

        private string BuildWeaponDetails(bool hasLiveItem)
        {
            if (!_payload.IsIdentified)
                return string.Empty;
            if (_payload.ItemType == BaseItem.PowerGlove)
                return WeaponDamage.PowerGloveGripDescription;
            if (!WeaponDamage.IsSingleWeaponType(_payload.ItemType))
                return string.Empty;

            var details = WeaponDamage.BuildSingleWeaponDescription(_payload.ItemDMG,
                WeaponDamage.GetNaturalSingleWeaponPercent(Player),
                Stat.GetStatAdjustment(Player, StatType.SingleWeaponDamagePercentAdjustment), _payload.HasItemDMG);
            if (_payload.HasItemDMG && hasLiveItem && GetItemInSlot(InventorySlot.RightHand, Player) == _payload.ItemObject)
                details += EquipmentPredicates.HasSingleWeapon(Player)
                    ? "\nSingle Weapon is active."
                    : "\nSingle Weapon is inactive: your off hand must be empty.";
            return details;
        }

        private void ApplyContent()
        {
            var changed = _contentLayout == null ||
                          !string.Equals(_renderedFacts, Facts, StringComparison.Ordinal) ||
                          !string.Equals(_renderedBody, Body, StringComparison.Ordinal) ||
                          !string.Equals(_renderedIcon, _payload.IconResref, StringComparison.Ordinal);
            if (changed)
            {
                _contentLayout = JsonDump(ExamineItemLayout.Build(_payload.IconResref, _payload.IconSlotWidth, _payload.IconSlotHeight, Facts, Body));
                _renderedFacts = Facts;
                _renderedBody = Body;
                _renderedIcon = _payload.IconResref;
            }

            // Bind clears the applied groups when reopening, but the cached layout can be reused.
            if (!_isContentApplied || changed)
            {
                SetGroupLayout(ContentElement, JsonParse(_contentLayout));
                _isContentApplied = true;
            }
        }

        public void Refresh(EquipItemRefreshEvent payload) => RefreshContent();
        public void Refresh(UnequipItemRefreshEvent payload) => RefreshContent();
        public void Refresh(PerkAcquiredRefreshEvent payload) => RefreshContent();
        public void Refresh(PerkRefundedRefreshEvent payload) => RefreshContent();
        public void Refresh(StatAdjustmentRefreshEvent payload) => RefreshContent();
    }
}
