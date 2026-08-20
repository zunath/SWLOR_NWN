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
        private int _bottomIndex;

        private readonly string[] _enhancementSerialized = new string[EnhancementSlotCount];
        private readonly List<ItemProperty>[] _enhancementProperties =
        {
            new List<ItemProperty>(),
            new List<ItemProperty>()
        };

        private string _submissionSerialized;
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

        public string SubmissionTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SubmissionResref
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

            _submissionSerialized = string.Empty;
            SubmissionTooltip = "Select Weapon Submission Token";
            SubmissionResref = BlankTexture;

            StatusText = string.Empty;
            StatusColor = GuiColor.Green;

            RefreshWeaponType();
        }

        private void RefreshWeaponType()
        {
            IsLightsaberSelected = _weaponType == BaseItem.Lightsaber;
            IsSaberstaffSelected = _weaponType == BaseItem.Saberstaff;

            RefreshBottom();
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

            var topKey = SelectedTop.PreviewResref;
            _weaponType = weaponType;
            _bottomIndex = 0;
            RetainTopByKey(topKey);
            RefreshWeaponType();
        }

        private void ChangeBottom(int direction)
        {
            if (_isConstructing)
                return;

            var topKey = SelectedTop.PreviewResref;
            var count = Bottoms.Count;
            _bottomIndex = (_bottomIndex + direction + count) % count;
            RetainTopByKey(topKey);
            RefreshBottom();
        }

        private void ChangeTop(int direction)
        {
            if (_isConstructing)
                return;

            var count = AvailableTops.Count;
            _topIndex = (_topIndex + direction + count) % count;
            RefreshTop();
        }

        // Retains the selected top across bottom/weapon changes by its unique preview
        // resref rather than its display name, since some colors share a display name.
        private void RetainTopByKey(string topKey)
        {
            var tops = AvailableTops;
            var index = -1;
            for (var i = 0; i < tops.Count; i++)
            {
                if (tops[i].PreviewResref == topKey)
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

        public Action OnClickSubmissionToken() => () =>
        {
            if (_isConstructing)
                return;

            if (string.IsNullOrWhiteSpace(_submissionSerialized))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on a Weapon Submission Token within your inventory.",
                    item =>
                    {
                        if (GetItemPossessor(item) != Player)
                        {
                            FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                            return;
                        }

                        if (GetTag(item) != LightsaberWorkbench.WeaponSubmissionTokenTag)
                        {
                            FloatingTextStringOnCreature("Item must be a Weapon Submission Token.", Player, false);
                            return;
                        }

                        _submissionSerialized = ObjectPlugin.Serialize(item);
                        SubmissionTooltip = GetName(item);
                        SubmissionResref = Item.GetIconResref(item);

                        DestroyObject(item);
                    });
            }
            else
            {
                ShowModal("Will you remove the Weapon Submission Token?", () =>
                {
                    ReturnSubmissionToken();
                    SubmissionTooltip = "Select Weapon Submission Token";
                    SubmissionResref = BlankTexture;
                });
            }
        };

        private void ReturnSubmissionToken()
        {
            if (string.IsNullOrWhiteSpace(_submissionSerialized))
                return;

            var item = ObjectPlugin.Deserialize(_submissionSerialized);
            ObjectPlugin.AcquireItem(Player, item);
            _submissionSerialized = string.Empty;
        }

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
            var consumed = string.IsNullOrWhiteSpace(_submissionSerialized)
                ? "The Kyber Token and socketed enhancements will be consumed."
                : "The Kyber Token, socketed enhancements, and Weapon Submission Token will be consumed.";
            ShowModal($"Construct this {weaponName}? {consumed}", () =>
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

            // CopyItemAndModify is unreliable on items held in a creature's inventory
            // (it returns OBJECT_INVALID), which is why the appearance never applied.
            // Build the weapon inside a neutral storage placeable - the same approach the
            // outfit and ship-stat systems use - then hand the finished weapon to the player.
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (!GetIsObjectValid(storage))
            {
                StatusText = "The workbench is missing its assembly module. Please notify staff.";
                StatusColor = GuiColor.Red;
                return;
            }

            var item = CreateItemOnObject(resref, storage);
            item = ModifyWeaponPart(item, AppearanceWeapon.Bottom, bottom.PartValue);
            item = ModifyWeaponPart(item, AppearanceWeapon.Middle, LightsaberWorkbench.MiddlePartValue);
            item = ModifyWeaponPart(item, AppearanceWeapon.Top, topValue);

            if (!GetIsObjectValid(item))
            {
                StatusText = "Something went wrong constructing that weapon. Your Kyber Token was not consumed.";
                StatusColor = GuiColor.Red;
                return;
            }

            SetLocalBool(item, Item.PlayerProducedItemVariable, true);

            // Hand the weapon to the player BEFORE its properties are edited. Merging two
            // enhancements of the same stat removes the pre-merge property, and that
            // removal is not committed to the item's underlying property list right away:
            // GetFirstItemProperty already skips it, but a CopyItem taken later in the same
            // execution still snapshots it, resurrecting the pre-merge value alongside the
            // merged one. That is what put a stray "Accuracy Bonus: +5" next to the merged
            // "+10" when two accuracy kits were socketed - one kit never removed anything,
            // so it copied clean, which is why only the two-kit case was affected.
            //
            // The appearance work above still has to happen inside the storage placeable,
            // because CopyItemAndModify returns OBJECT_INVALID for items held in a
            // creature's inventory. The ordering rule is only that nothing may copy the
            // weapon after its item properties have been edited.
            var finishedItem = CopyItem(item, Player, true);
            DestroyObject(item);
            item = finishedItem;

            if (!GetIsObjectValid(item))
            {
                StatusText = "Something went wrong constructing that weapon. Your Kyber Token was not consumed.";
                StatusColor = GuiColor.Red;
                return;
            }

            ApplyBladeLight(item, top.LightColor);

            foreach (var property in _enhancementProperties.SelectMany(x => x))
            {
                Craft.ApplyCraftedItemProperty(item, property);
            }

            for (var slot = 0; slot < EnhancementSlotCount; slot++)
            {
                _enhancementSerialized[slot] = string.Empty;
                _enhancementProperties[slot].Clear();
            }

            // Transfer the Weapon Submission Token's crafted stats onto the saber. The
            // saber's damage profile is owned by its tier - it is built at tier 1 and
            // advanced by the saber upgrade kits - so DMG, weapon damage type, and
            // attack delay are skipped, as is the token blueprint's own skill
            // requirement scaffolding and its anti-equip Use Limitation: Perk lock
            // (the token is meant to be unusable on its own, not the finished saber).
            if (!string.IsNullOrWhiteSpace(_submissionSerialized))
            {
                var submissionToken = ObjectPlugin.Deserialize(_submissionSerialized);
                if (GetIsObjectValid(submissionToken))
                {
                    for (var ip = GetFirstItemProperty(submissionToken); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(submissionToken))
                    {
                        var type = GetItemPropertyType(ip);
                        if (type == ItemPropertyType.DMG ||
                            type == ItemPropertyType.Delay ||
                            type == ItemPropertyType.WeaponDamageType ||
                            type == ItemPropertyType.RequiresSkill ||
                            type == ItemPropertyType.UseLimitationPerk)
                            continue;

                        Craft.ApplyCraftedItemProperty(item, ip);
                    }

                    DestroyObject(submissionToken);
                }

                _submissionSerialized = string.Empty;
            }

            Currency.TakeCurrency(Player, CurrencyType.KyberToken, 1);
            SendMessageToPC(Player, $"You spend a Kyber Token constructing your {GetName(item)}. Total Kyber Tokens: {Currency.GetCurrency(Player, CurrencyType.KyberToken)}");

            var playerId = GetObjectUUID(Player);
            Log.Write(LogGroup.Crafting, $"{GetName(Player)} ({playerId}) constructed '{GetName(item)}' (bottom: {bottom.Name}, top: {top.Name}) at a lightsaber workbench.");
            FloatingTextStringOnCreature($"You have constructed your {GetName(item)}!", Player, false);

            Gui.TogglePlayerWindow(Player, GuiWindowType.LightsaberWorkbench);
        }

        private static void ApplyBladeLight(uint item, LightColor lightColor)
        {
            // The output blueprints carry a neutral white fallback light. Replace it
            // with the selected blade's supported engine color during construction.
            // Scan first because removing properties while iterating skips entries.
            var existingLights = new List<ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == ItemPropertyType.Light)
                    existingLights.Add(ip);
            }

            foreach (var light in existingLights)
            {
                RemoveItemProperty(item, light);
            }

            var bladeLight = ItemPropertyLight(LightBrightness.LIGHTBRIGHTNESS_DIM, lightColor);
            AddItemProperty(DurationType.Permanent, bladeLight, item);
        }

        private static uint ModifyWeaponPart(uint item, AppearanceWeapon partSlot, int partValue)
        {
            // The engine stores weapon part appearance as two channels: a model number
            // and a color number. The catalog values encode both, matching the model
            // file names (e.g. hilt 25.4 -> wswglsbr_b_254 -> model 25, color 4), so
            // split the value and set each channel separately - the same way the
            // appearance editor does. Passing the combined value as the model number
            // is invalid and makes CopyItemAndModify return OBJECT_INVALID.
            var modelId = partValue / 10;
            var colorId = partValue % 10;

            if (GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)partSlot) != colorId)
            {
                var copy = CopyItemAndModify(item, ItemAppearanceType.WeaponColor, (int)partSlot, colorId, true);
                if (GetIsObjectValid(copy))
                {
                    DestroyObject(item);
                    item = copy;
                }
            }

            if (GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)partSlot) != modelId)
            {
                var copy = CopyItemAndModify(item, ItemAppearanceType.WeaponModel, (int)partSlot, modelId, true);
                if (GetIsObjectValid(copy))
                {
                    DestroyObject(item);
                    item = copy;
                }
            }

            return item;
        }

        public override Action OnWindowClosed() => () =>
        {
            for (var slot = 0; slot < EnhancementSlotCount; slot++)
            {
                ReturnEnhancement(slot);
            }

            ReturnSubmissionToken();
        };
    }
}
