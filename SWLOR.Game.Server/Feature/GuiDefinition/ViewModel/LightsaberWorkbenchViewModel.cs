using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LightsaberWorkbenchService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class LightsaberWorkbenchViewModel : GuiViewModelBase<LightsaberWorkbenchViewModel, GuiPayloadBase>
    {
        private const string BlankTexture = "Blank";
        private const int EnhancementSlotCount = 2;

        private BaseItem _weaponType;
        private int _topIndex;
        private int _middleIndex;
        private int _bottomIndex;

        private readonly string[] _enhancementSerialized = new string[EnhancementSlotCount];
        private readonly List<ItemProperty>[] _enhancementProperties =
        {
            new List<ItemProperty>(),
            new List<ItemProperty>()
        };

        private bool _isConstructing;

        public bool IsLightsaberSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSaberstaffSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string TopName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TopPreview
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TopCountText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MiddleName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MiddlePreview
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MiddleCountText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string BottomName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string BottomPreview
        {
            get => Get<string>();
            set => Set(value);
        }

        public string BottomCountText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement1Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement2Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StatusText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor StatusColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        private IReadOnlyList<SaberHiltPart> Bottoms => LightsaberWorkbench.GetHilts(_weaponType);

        private SaberHiltPart SelectedBottom => Bottoms[_bottomIndex];

        private IReadOnlyList<SaberHiltPart> Middles => LightsaberWorkbench.GetMiddles(_weaponType);

        private SaberHiltPart SelectedMiddle => Middles[_middleIndex];

        // The top (emitter) model carries the blade color; curved bottom hilts
        // require the curved emitter set, so the available tops depend on the bottom.
        private IReadOnlyList<SaberBladeColor> AvailableTops =>
            LightsaberWorkbench.GetBladeColors(_weaponType, SelectedBottom.IsCurved);

        private SaberBladeColor SelectedTop => AvailableTops[_topIndex];

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _isConstructing = false;
            _weaponType = BaseItem.Lightsaber;
            _topIndex = 0;
            _middleIndex = 0;
            _bottomIndex = 0;

            for (var slot = 0; slot < EnhancementSlotCount; slot++)
            {
                _enhancementSerialized[slot] = string.Empty;
                _enhancementProperties[slot].Clear();
            }

            Enhancement1Tooltip = "Select Enhancement #1";
            Enhancement1Resref = BlankTexture;
            Enhancement2Tooltip = "Select Enhancement #2";
            Enhancement2Resref = BlankTexture;

            StatusText = string.Empty;
            StatusColor = GuiColor.Green;

            RefreshWeaponType();
        }

        private void RefreshWeaponType()
        {
            IsLightsaberSelected = _weaponType == BaseItem.Lightsaber;
            IsSaberstaffSelected = _weaponType == BaseItem.Saberstaff;

            RefreshBottom();
            RefreshMiddle();
        }

        private void RefreshBottom()
        {
            var bottoms = Bottoms;
            if (_bottomIndex >= bottoms.Count)
                _bottomIndex = 0;

            BottomName = SelectedBottom.Name;
            BottomPreview = SelectedBottom.PreviewResref;
            BottomCountText = $"{_bottomIndex + 1} / {bottoms.Count}";

            RefreshTop();
        }

        private void RefreshMiddle()
        {
            var middles = Middles;
            if (_middleIndex >= middles.Count)
                _middleIndex = 0;

            MiddleName = SelectedMiddle.Name;
            MiddlePreview = SelectedMiddle.PreviewResref;
            MiddleCountText = $"{_middleIndex + 1} / {middles.Count}";
        }

        private void RefreshTop()
        {
            var tops = AvailableTops;
            if (_topIndex >= tops.Count)
                _topIndex = 0;

            TopName = SelectedTop.Name;
            TopPreview = SelectedTop.PreviewResref;
            TopCountText = $"{_topIndex + 1} / {tops.Count}";
        }

        private void SwitchWeaponType(BaseItem weaponType)
        {
            if (_isConstructing || _weaponType == weaponType)
            {
                RefreshWeaponType();
                return;
            }

            var topName = SelectedTop.Name;
            _weaponType = weaponType;
            _bottomIndex = 0;
            _middleIndex = 0;
            RetainTopByName(topName);
            RefreshWeaponType();
        }

        private void ChangeBottom(int direction)
        {
            if (_isConstructing)
                return;

            var topName = SelectedTop.Name;
            var count = Bottoms.Count;
            _bottomIndex = (_bottomIndex + direction + count) % count;
            RetainTopByName(topName);
            RefreshBottom();
        }

        private void ChangeMiddle(int direction)
        {
            if (_isConstructing)
                return;

            var count = Middles.Count;
            _middleIndex = (_middleIndex + direction + count) % count;
            RefreshMiddle();
        }

        private void ChangeTop(int direction)
        {
            if (_isConstructing)
                return;

            var count = AvailableTops.Count;
            _topIndex = (_topIndex + direction + count) % count;
            RefreshTop();
        }

        private void RetainTopByName(string topName)
        {
            var tops = AvailableTops;
            var index = -1;
            for (var i = 0; i < tops.Count; i++)
            {
                if (tops[i].Name == topName)
                {
                    index = i;
                    break;
                }
            }

            _topIndex = index > -1 ? index : 0;
        }

        public Action OnClickLightsaber() => () => SwitchWeaponType(BaseItem.Lightsaber);

        public Action OnClickSaberstaff() => () => SwitchWeaponType(BaseItem.Saberstaff);

        public Action OnClickPreviousTop() => () => ChangeTop(-1);

        public Action OnClickNextTop() => () => ChangeTop(1);

        public Action OnClickPreviousMiddle() => () => ChangeMiddle(-1);

        public Action OnClickNextMiddle() => () => ChangeMiddle(1);

        public Action OnClickPreviousBottom() => () => ChangeBottom(-1);

        public Action OnClickNextBottom() => () => ChangeBottom(1);

        private bool IsValidEnhancement(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return false;
            }

            var foundWeaponEnhancement = false;
            var enhancementLevel = -1;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.WeaponEnhancement)
                {
                    foundWeaponEnhancement = true;
                }

                if (type == ItemPropertyType.EnhancementLevel)
                {
                    enhancementLevel = GetItemPropertyCostTableValue(ip);
                }

                if (foundWeaponEnhancement && enhancementLevel > -1)
                    break;
            }

            if (!foundWeaponEnhancement || enhancementLevel == -1)
            {
                FloatingTextStringOnCreature("Item must be a weapon enhancement.", Player, false);
                return false;
            }

            if (enhancementLevel - LightsaberWorkbench.EnhancementRecipeLevel > 5)
            {
                FloatingTextStringOnCreature("That enhancement is too advanced for this workbench.", Player, false);
                return false;
            }

            return true;
        }

        private void CollectEnhancementProperties(uint item, List<ItemProperty> itemProperties)
        {
            var weaponDamageType = CombatDamageType.Invalid;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.WeaponDamageType)
                {
                    var subType = GetItemPropertySubType(ip);
                    if (Enum.IsDefined(typeof(CombatDamageType), subType))
                        weaponDamageType = (CombatDamageType)subType;
                }
            }

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.WeaponEnhancement)
                    continue;

                var subType = (EnhancementSubType)GetItemPropertySubType(ip);
                var amount = GetItemPropertyCostTableValue(ip);
                itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount, weaponDamageType));
            }
        }

        private void ToggleEnhancementSlot(int slot, Action<string> setTooltip, Action<string> setResref)
        {
            if (_isConstructing)
                return;

            if (string.IsNullOrWhiteSpace(_enhancementSerialized[slot]))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item))
                            return;

                        CollectEnhancementProperties(item, _enhancementProperties[slot]);
                        _enhancementSerialized[slot] = ObjectPlugin.Serialize(item);
                        setTooltip(GetName(item));
                        setResref(Item.GetIconResref(item));

                        DestroyObject(item);
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    ReturnEnhancement(slot);
                    setTooltip($"Select Enhancement #{slot + 1}");
                    setResref(BlankTexture);
                });
            }
        }

        private void ReturnEnhancement(int slot)
        {
            if (string.IsNullOrWhiteSpace(_enhancementSerialized[slot]))
                return;

            var item = ObjectPlugin.Deserialize(_enhancementSerialized[slot]);
            ObjectPlugin.AcquireItem(Player, item);
            _enhancementSerialized[slot] = string.Empty;
            _enhancementProperties[slot].Clear();
        }

        public Action OnClickEnhancement1() => () =>
        {
            ToggleEnhancementSlot(0, tooltip => Enhancement1Tooltip = tooltip, resref => Enhancement1Resref = resref);
        };

        public Action OnClickEnhancement2() => () =>
        {
            ToggleEnhancementSlot(1, tooltip => Enhancement2Tooltip = tooltip, resref => Enhancement2Resref = resref);
        };

        public Action OnClickConstruct() => () =>
        {
            if (_isConstructing)
                return;

            var error = LightsaberWorkbench.ValidateAccess(Player);
            if (!string.IsNullOrWhiteSpace(error))
            {
                StatusText = error;
                StatusColor = GuiColor.Red;
                return;
            }

            var weaponName = _weaponType == BaseItem.Saberstaff ? "saberstaff" : "lightsaber";
            ShowModal($"Construct this {weaponName}? The Kyber Token and socketed enhancements will be consumed.", () =>
            {
                _isConstructing = true;
                ConstructSaber();
                _isConstructing = false;
            });
        };

        private void ConstructSaber()
        {
            if (Currency.GetCurrency(Player, CurrencyType.KyberToken) < 1)
            {
                StatusText = "You need a Kyber Token to construct this weapon.";
                StatusColor = GuiColor.Red;
                return;
            }

            var bottom = SelectedBottom;
            var middle = SelectedMiddle;
            var top = SelectedTop;
            var topValue = LightsaberWorkbench.GetTopValue(top, _weaponType, bottom.IsCurved);
            if (topValue <= -1)
            {
                StatusText = "That blade color is not available for the selected hilt.";
                StatusColor = GuiColor.Red;
                return;
            }

            var resref = _weaponType == BaseItem.Saberstaff
                ? LightsaberWorkbench.SaberstaffResref
                : LightsaberWorkbench.LightsaberResref;

            var item = CreateItemOnObject(resref, Player);
            SetLocalBool(item, Item.PlayerProducedItemVariable, true);

            item = ModifyWeaponPart(item, AppearanceWeapon.Bottom, bottom.PartValue);
            item = ModifyWeaponPart(item, AppearanceWeapon.Middle, middle.PartValue);
            item = ModifyWeaponPart(item, AppearanceWeapon.Top, topValue);

            foreach (var property in _enhancementProperties.SelectMany(x => x))
            {
                Craft.ApplyCraftedItemProperty(item, property);
            }

            for (var slot = 0; slot < EnhancementSlotCount; slot++)
            {
                _enhancementSerialized[slot] = string.Empty;
                _enhancementProperties[slot].Clear();
            }

            Currency.TakeCurrency(Player, CurrencyType.KyberToken, 1);

            var playerId = GetObjectUUID(Player);
            Log.Write(LogGroup.Crafting, $"{GetName(Player)} ({playerId}) constructed '{GetName(item)}' (bottom: {bottom.Name}, middle: {middle.Name}, top: {top.Name}) at a lightsaber workbench.");
            FloatingTextStringOnCreature($"You have constructed your {GetName(item)}!", Player, false);

            Gui.TogglePlayerWindow(Player, GuiWindowType.LightsaberWorkbench);
        }

        private static uint ModifyWeaponPart(uint item, AppearanceWeapon partSlot, int partValue)
        {
            var copy = CopyItemAndModify(item, ItemAppearanceType.WeaponModel, (int)partSlot, partValue, true);
            DestroyObject(item);
            return copy;
        }

        public override Action OnWindowClosed() => () =>
        {
            for (var slot = 0; slot < EnhancementSlotCount; slot++)
            {
                ReturnEnhancement(slot);
            }
        };
    }
}
