using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CraftViewModel: GuiViewModelBase<CraftViewModel, CraftPayload>,
        IGuiRefreshable<SkillXPRefreshEvent>
    {
        public const string ViewName = "CraftView";
        public const string SetUpPartialName = "SetUpPartial";
        public const string CraftPartialName = "CraftPartial";
        private const string BlankTexture = "Blank";

        private RecipeType _recipe;

        private uint _blueprintItem;
        private BlueprintDetail _activeBlueprint;
        private bool _hasBlueprint;
        private static readonly BlueprintBonuses _blueprintBonuses = new();

        public const string CraftActionsElement = "CraftActions";
        private readonly List<string> _actionEventKeys = new();
        public const string CraftContentElement = "CraftContent";
        public const string CraftContentPartial = "CraftContentPartial";
        private static readonly Dictionary<uint, CraftViewModel> OpenCraftViews = new();
        private CraftSession _session;
        private CraftSession _setupSession;
        private CraftSessionSettlement _settlement = new();
        private bool _isSettling;
        private int _enhancementProgressPenalty;
        private readonly Dictionary<int, uint> _selectedEnhancementItems = new();
        private readonly List<string> _actionHistory = new();

        public bool IsClosable
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string RecipeName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string RecipeLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        public string YourSkill
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CraftText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> RecipeDescription
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> RecipeColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public bool IsInSetupMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsInCraftMode
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ControlTotal
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CraftsmanshipTotal
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsEnhancement1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsEnhancement8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Enhancement1Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement8Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement1Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Enhancement2Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement3Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement4Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement5Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement6Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement7Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }
        public string Enhancement8Tooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DurabilityText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float DurabilityPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string ProgressText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float ProgressPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string QualityText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float QualityPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string CP
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

        public string ConditionSummary { get => Get<string>(); set => Set(value); }
        public string ProfileSummary { get => Get<string>(); set => Set(value); }
        public string CraftHelp { get => Get<string>(); set => Set(value); }
        public bool IsHelpVisible { get => Get<bool>(); set => Set(value); }
        public Action OnToggleCraftHelp() => () => IsHelpVisible = !IsHelpVisible;
        public string BuffSummary { get => Get<string>(); set => Set(value); }
        public string QualityRewards { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> ActionHistory { get => Get<GuiBindingList<string>>(); set => Set(value); }

        private readonly List<string> _components = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement1 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement2 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement3 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement4 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement5 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement6 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement7 = new();
        private readonly List<ItemProperty> _itemPropertiesEnhancement8 = new();
        private string _enhancement1;
        private string _enhancement2;
        private string _enhancement3;
        private string _enhancement4;
        private string _enhancement5;
        private string _enhancement6;
        private string _enhancement7;
        private string _enhancement8;
        private int _durability => (_session ?? _setupSession)?.Durability ?? 0;
        private int _maxDurability => (_session ?? _setupSession)?.MaxDurability ?? 0;
        private int _progress => (_session ?? _setupSession)?.Progress ?? 0;
        private int _maxProgress => (_session ?? _setupSession)?.MaxProgress ?? 0;
        private int _quality => (_session ?? _setupSession)?.Quality ?? 0;
        private int _maxQuality => (_session ?? _setupSession)?.MaxQuality ?? 0;
        private int _cp => (_session ?? _setupSession)?.CP ?? 0;
        private int _maxCP => (_session ?? _setupSession)?.MaxCP ?? 0;

        protected override void Initialize(CraftPayload initialPayload)
        {
            if (OpenCraftViews.TryGetValue(Player, out var previous))
                previous.OnCloseWindow().Invoke();
            OpenCraftViews[Player] = this;
            _components.Clear();
            IsHelpVisible = false;
            CraftHelp = "Fill progress to finish immediately. Quality improves rewards. Hover over each action for exact costs, gains and risks. Conditions apply to this turn; every accepted action advances the forecast. No timer.";
            _actionHistory.Clear();
            _enhancementProgressPenalty = 0;

            if (CraftingJournal.Get(Player) is { } pending)
            {
                RestoreTransaction(pending);
                return;
            }
            _recipe = initialPayload.Recipe;
            _blueprintItem = initialPayload.BlueprintItem;
            var recipe = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            _hasBlueprint = blueprint.Recipe != RecipeType.Invalid;

            var itemName = Cache.GetItemNameByResref(recipe.Resref);

            SwitchToSetUpMode();
            StatusColor = GuiColor.Green;
            StatusText = string.Empty;

            RefreshSetupRecipe();

            LoadCraftingState();
            RefreshRecipeStats();
            RestoreCraftContent();
        }

        private void RestoreCraftContent() => SwapNestedPartialView(CraftContentElement, CraftContentPartial, onAfterApply: RefreshActionLayout);
        protected override void OnModalClosedRestore() => RestoreCraftContent();

        private CraftSession CreateSessionSnapshot(bool commit = true)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var recipe = Craft.GetRecipe(_recipe);
            var equipmentCP = dbPlayer.CPBonus.TryGetValue(recipe.Skill, out var cp) ? cp : 0;
            var stats = CraftRuleAttribute.All.ToDictionary(entry => entry.Stat,
                entry => Stat.GetCraftingStatAdjustment(Player, recipe.Skill, entry.Stat));
            var profile = CraftRolloutPolicy.UseConditions(ApplicationSettings.Get().CraftingRollout, recipe) ? recipe.CraftingProfile : CraftProfile.Legacy;
            return CraftSession.Create(dbPlayer.Skills[recipe.Skill].Rank, recipe.Level,
                Craft.GetRecipeLevelDetail(recipe.Level), Stat.CalculateCraftsmanship(Player, recipe.Skill),
                Stat.CalculateControl(Player, recipe.Skill), equipmentCP, profile, recipe.CraftingTechnique, stats,
                count => commit ? System.Random.Shared.Next(count) : 0, _enhancementProgressPenalty);
        }

        private void LoadCraftingState() => _setupSession = CreateSessionSnapshot(false);

        private void RefreshActionPreviews()
        {
            var state = _session ?? _setupSession;
            if (state == null) return;
            var buffs = new List<string>();
            if (state.SteadyHandActive) buffs.Add("Steady Hand: next synthesis");
            if (state.MuscleMemoryActive) buffs.Add("Muscle Memory: next touch");
            if (state.VenerationCharges > 0) buffs.Add($"Veneration: {state.VenerationCharges} paid syntheses");
            if (state.WasteNotCharges > 0) buffs.Add($"Waste Not: {state.WasteNotCharges} durability-spending actions");
            if (state.RulesVersion == 2)
            {
                foreach (var (type, buff) in state.Buffs) buffs.Add($"{CraftRolloutPolicy.BuffName(type)}: {buff.Charges} charge(s), {buff.ActionsRemaining} actions left");
                foreach (var buff in new[] { CraftBuffType.SteadyHand, CraftBuffType.MuscleMemory, CraftBuffType.Veneration, CraftBuffType.WasteNot })
                    buffs.Add($"{CraftRolloutPolicy.BuffName(buff)} uses left: {(buff == CraftBuffType.SteadyHand || buff == CraftBuffType.MuscleMemory ? 2 : 1) - state.Used(buff)}");
            }
            ConditionSummary = state.RulesVersion == 2
                ? IsInCraftMode ? $"Now: {state.CurrentCondition}. Next: {state.Forecast}" : "Starts at Normal. Forecast is revealed when materials are committed."
                : "Classic crafting rules.";
            var recipe = Craft.GetRecipe(_recipe);
            ProfileSummary = state.RulesVersion == 2 ? $"{state.Profile}: {CraftRolloutPolicy.ProfileDescription(state.Profile)}" : "Classic workpiece.";
            var investment = state.CraftingStats.Where(entry => entry.Value > 0).Select(entry =>
                $"{CraftRolloutPolicy.StatName(entry.Key)}: {entry.Value}, triggered {state.TriggerCount(entry.Key)} time(s)");
            CraftHelp = "Progress finishes immediately; quality improves rewards. Hover actions for costs, gains and risks. " +
                (state.RulesVersion == 2 ? "Accepted actions advance preparations; some perks preserve a condition. No timer.\n" : "") +
                string.Join("\n", investment);
            BuffSummary = buffs.Count == 0 ? "Active preparations: none" : string.Join(" | ", buffs);
            var qualityChance = (int)((float)state.Quality / state.MaxQuality * 100);
            QualityRewards = $"Quality: {qualityChance}% transfer chance per enhancement property group; " +
                (recipe.Category == RecipeCategoryType.Food ? "food duration gains its quality bonus at 100%; " : "") +
                "also improves crafting XP and vendor value.";
            var history = new GuiBindingList<string>();
            foreach (var entry in _actionHistory) history.Add(entry);
            ActionHistory = history;
        }

        private void RefreshRecipeStats()
        {
            if (_session == null)
                LoadCraftingState();
            RefreshActionPreviews();
            CP = $"CP: {_cp}/{_maxCP}";

            DurabilityPercentage = (float)_durability / (float)_maxDurability;
            DurabilityText = $"Durability ({_durability}/{_maxDurability})";

            ProgressPercentage = (float)_progress / (float)_maxProgress;
            ProgressText = $"Progress ({_progress}/{_maxProgress})";

            QualityPercentage = (float)_quality / (float)_maxQuality;
            QualityText = $"Quality ({_quality}/{_maxQuality})";
            RefreshActionLayout();
        }

        public override Action OnWindowClosed() => OnCloseWindow();

        public static void CloseForPlayer(uint player)
        {
            if (!OpenCraftViews.TryGetValue(player, out var view))
                return;
            if (Gui.IsWindowOpen(player, GuiWindowType.Craft))
                Gui.ClosePlayerWindow(player, GuiWindowType.Craft);
            else
                view.OnCloseWindow().Invoke();
        }

        public Action OnCloseWindow() => () =>
        {
            if (_isSettling)
                return;
            OpenCraftViews.Remove(Player);
            ClearActionEvents();
            if (_session != null && _settlement.IsClaimed)
            {
                RemoveImmobility();
                return;
            }
            // Closing the window while in craft mode results in an immediate failure,
            // possibly resulting in losing components and enhancements
            if (IsInCraftMode && _session != null)
            {
                _session = _session with { Status = CraftSessionStatus.Aborted };
                CraftingJournal.SaveSession(Player, _session);
                ProcessFailure();
            }
            else
            {
                ClearReservedState();
            }
            IsInSetupMode = false;
            IsInCraftMode = false;
            RemoveImmobility();
        };

        private bool IsValidEnhancement(uint item)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var typeIP = ItemPropertyType.Invalid;

            if (!IsInSetupMode || !Gui.IsWindowOpen(Player, GuiWindowType.Craft))
                return false;
            if (!GetIsObjectValid(item) || _selectedEnhancementItems.Values.Count(selected => selected == item) >= GetItemStackSize(item))
                return false;
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return false;
            }

            if (recipe.EnhancementType == RecipeEnhancementType.Armor)
            {
                typeIP = ItemPropertyType.ArmorEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Weapon)
            {
                typeIP = ItemPropertyType.WeaponEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Structure)
            {
                typeIP = ItemPropertyType.StructureEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Food)
            {
                typeIP = ItemPropertyType.FoodEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Starship)
            {
                typeIP = ItemPropertyType.StarshipEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Module)
            {
                typeIP = ItemPropertyType.ModuleEnhancement;
            }
            else if (recipe.EnhancementType == RecipeEnhancementType.Droid)
            {
                typeIP = ItemPropertyType.DroidEnhancement;
            }

            if (typeIP == ItemPropertyType.Invalid)
            {
                FloatingTextStringOnCreature("Item must be an enhancement.", Player, false);
                return false;
            }

            var foundIp = false;
            var enhancementLevel = -1;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == typeIP)
                {
                    foundIp = true;
                }

                if (type == ItemPropertyType.EnhancementLevel)
                {
                    enhancementLevel = GetItemPropertyCostTableValue(ip);
                }

                if(foundIp && enhancementLevel > -1)
                    break;
            }

            if (!foundIp || enhancementLevel == -1)
            {
                FloatingTextStringOnCreature("Item must be an enhancement.", Player, false);
                return false;
            }

            var levelDiff = enhancementLevel - recipe.Level;
            if (levelDiff > 5)
            {
                FloatingTextStringOnCreature("Enhancement must be within 5 levels of recipe level.", Player, false);
                return false;
            }

            return true;
        }

        private int CalculateProgressPenaltyAndProcessItemProperties(uint item, List<ItemProperty> itemProperties)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var progressPenalty = 0;
            var weaponDamageType = CombatDamageType.Invalid;

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.WeaponDamageType)
                {
                    var subType = GetItemPropertySubType(ip);
                    if (Enum.IsDefined(typeof(CombatDamageType), subType))
                        weaponDamageType = (CombatDamageType)subType;
                }
            }

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                var subType = (EnhancementSubType)GetItemPropertySubType(ip);
                var amount = GetItemPropertyCostTableValue(ip);

                // Progress Penalty - Add to total
                if (type == ItemPropertyType.ProgressPenalty)
                {
                    progressPenalty += GetItemPropertyCostTableValue(ip);
                }
                // Enhancement - Add to item property list.
                else if (type == ItemPropertyType.ArmorEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Armor)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
                }
                else if (type == ItemPropertyType.WeaponEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Weapon)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount, weaponDamageType));
                }
                else if (type == ItemPropertyType.StructureEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Structure)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
                }
                else if (type == ItemPropertyType.FoodEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Food)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
                }
                else if (type == ItemPropertyType.StarshipEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Starship)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
                }
                else if (type == ItemPropertyType.ModuleEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Module)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
                }
                else if (type == ItemPropertyType.DroidEnhancement &&
                         recipe.EnhancementType == RecipeEnhancementType.Droid)
                {
                    itemProperties.AddRange(Craft.BuildItemPropertiesForEnhancement(subType, amount));
                }
            }

            return progressPenalty;
        }

        public Action OnClickEnhancement1() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement1))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                {
                    if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement1))
                        return;

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement1);
                    _enhancement1 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[1] = item;
                    Enhancement1Tooltip = GetName(item);
                    Enhancement1Resref = Item.GetIconResref(item);
                    _enhancementProgressPenalty += progressPenalty;

                    RefreshRecipeStats();
                });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement1))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement1);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement1);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(1);
                    _enhancement1 = string.Empty;
                    Enhancement1Resref = BlankTexture;
                    Enhancement1Tooltip = "Select Enhancement #1";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement1.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement2() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement2))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                {
                    if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement2))
                        return;

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement2);
                    _enhancement2 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[2] = item;
                    Enhancement2Tooltip = GetName(item);
                    Enhancement2Resref = Item.GetIconResref(item);
                    _enhancementProgressPenalty += progressPenalty;

                    RefreshRecipeStats();
                });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement2))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement2);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement2);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(2);
                    _enhancement2 = string.Empty;
                    Enhancement2Resref = BlankTexture;
                    Enhancement2Tooltip = "Select Enhancement #2";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement2.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement3() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement3))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement3))
                            return;

                        var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement3);
                        _enhancement3 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[3] = item;
                        Enhancement3Tooltip = GetName(item);
                        Enhancement3Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        RefreshRecipeStats();
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement3))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement3);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement3);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(3);
                    _enhancement3 = string.Empty;
                    Enhancement3Resref = BlankTexture;
                    Enhancement3Tooltip = "Select Enhancement #3";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement3.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement4() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement4))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement4))
                            return;

                        var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement4);
                        _enhancement4 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[4] = item;
                        Enhancement4Tooltip = GetName(item);
                        Enhancement4Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        RefreshRecipeStats();
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement4))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement4);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement4);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(4);
                    _enhancement4 = string.Empty;
                    Enhancement4Resref = BlankTexture;
                    Enhancement4Tooltip = "Select Enhancement #4";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement4.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement5() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement5))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement5))
                            return;

                        var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement5);
                        _enhancement5 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[5] = item;
                        Enhancement5Tooltip = GetName(item);
                        Enhancement5Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        RefreshRecipeStats();
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement5))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement5);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement5);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(5);
                    _enhancement5 = string.Empty;
                    Enhancement5Resref = BlankTexture;
                    Enhancement5Tooltip = "Select Enhancement #5";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement5.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement6() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement6))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement6))
                            return;

                        var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement6);
                        _enhancement6 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[6] = item;
                        Enhancement6Tooltip = GetName(item);
                        Enhancement6Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        RefreshRecipeStats();
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement6))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement6);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement6);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(6);
                    _enhancement6 = string.Empty;
                    Enhancement6Resref = BlankTexture;
                    Enhancement6Tooltip = "Select Enhancement #6";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement6.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement7() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement7))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement7))
                            return;

                        var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement7);
                        _enhancement7 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[7] = item;
                        Enhancement7Tooltip = GetName(item);
                        Enhancement7Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        RefreshRecipeStats();
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement7))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement7);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement7);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(7);
                    _enhancement7 = string.Empty;
                    Enhancement7Resref = BlankTexture;
                    Enhancement7Tooltip = "Select Enhancement #7";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement7.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement8() => () =>
        {
            if (!IsInSetupMode)
                return;
            if (string.IsNullOrWhiteSpace(_enhancement8))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.",
                    item =>
                    {
                        if (!IsValidEnhancement(item) || !string.IsNullOrWhiteSpace(_enhancement8))
                            return;

                        var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement8);
                        _enhancement8 = CraftingJournal.SerializeQuantity(item, 1);
                    _selectedEnhancementItems[8] = item;
                        Enhancement8Tooltip = GetName(item);
                        Enhancement8Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        RefreshRecipeStats();
                    });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    if (!IsInSetupMode || string.IsNullOrWhiteSpace(_enhancement8))
                        return;
                    var item = ObjectPlugin.Deserialize(_enhancement8);

                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement8);
                    DestroyObject(item);
                    _selectedEnhancementItems.Remove(8);
                    _enhancement8 = string.Empty;
                    Enhancement8Resref = BlankTexture;
                    Enhancement8Tooltip = "Select Enhancement #8";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement8.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        private void RefreshSetupRecipe()
        {
            var recipe = Craft.GetRecipe(_recipe);
            _hasBlueprint = GetIsObjectValid(_blueprintItem) && GetItemPossessor(_blueprintItem) == Player &&
                string.IsNullOrWhiteSpace(GetLocalString(_blueprintItem, CraftingJournal.ConsumedItemVariable)) &&
                Craft.GetBlueprintDetails(_blueprintItem).Recipe == _recipe && Craft.GetBlueprintDetails(_blueprintItem).LicensedRuns > 0;
            if (!_hasBlueprint) _blueprintItem = OBJECT_INVALID;
            var blueprint = _hasBlueprint ? Craft.GetBlueprintDetails(_blueprintItem) : new BlueprintDetail();
            var slots = recipe.EnhancementSlots + blueprint.EnhancementSlots;
            IsEnhancement1Visible = slots >= 1; IsEnhancement2Visible = slots >= 2;
            IsEnhancement3Visible = slots >= 3; IsEnhancement4Visible = slots >= 4;
            IsEnhancement5Visible = slots >= 5; IsEnhancement6Visible = slots >= 6;
            IsEnhancement7Visible = slots >= 7; IsEnhancement8Visible = slots >= 8;
            RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
            RecipeLevel = $"Recipe level: {recipe.Level}";
            var (description, colors) = Craft.BuildRecipeDetail(Player, _recipe, blueprint);
            RecipeDescription = description; RecipeColors = colors;
            CraftText = CraftingJournal.Get(Player) != null ? "Collect pending rewards" : _hasBlueprint
                ? $"Craft [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]" : "Craft";
        }

        private void RefreshYourSkill(Player dbPlayer)
        {
            var detail = Craft.GetRecipe(_recipe);
            YourSkill = $"Your Skill: {Skill.GetSkillDetails(detail.Skill).Name} {dbPlayer.Skills[detail.Skill].Rank}";
        }

        private void ApplyImmobility()
        {
            var effect = TagEffect(EffectCutsceneImmobilize(), PlayerActivityEffectTag.CraftingImmobilize);
            ApplyEffectToObject(DurationType.Permanent, effect, Player);
        }

        private void RemoveImmobility()
        {
            RemoveEffectByTag(Player, PlayerActivityEffectTag.CraftingImmobilize);
        }

        private void SwitchToSetUpMode()
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            IsInCraftMode = false;
            IsInSetupMode = true;
            IsClosable = true;
            RefreshYourSkill(dbPlayer);





            _session = null;
            _enhancementProgressPenalty = 0;
            RefreshSetupRecipe();

            Enhancement1Resref = BlankTexture;
            Enhancement2Resref = BlankTexture;
            Enhancement3Resref = BlankTexture;
            Enhancement4Resref = BlankTexture;
            Enhancement5Resref = BlankTexture;
            Enhancement6Resref = BlankTexture;
            Enhancement7Resref = BlankTexture;
            Enhancement8Resref = BlankTexture;
            Enhancement1Tooltip = "Select Enhancement #1";
            Enhancement2Tooltip = "Select Enhancement #2";
            Enhancement3Tooltip = "Select Enhancement #3";
            Enhancement4Tooltip = "Select Enhancement #4";
            Enhancement5Tooltip = "Select Enhancement #5";
            Enhancement6Tooltip = "Select Enhancement #6";
            Enhancement7Tooltip = "Select Enhancement #7";
            Enhancement8Tooltip = "Select Enhancement #8";

            RemoveImmobility();
        }

        private void SwitchToCraftMode(CraftSession session)
        {
            _session = session;
            _settlement = new CraftSessionSettlement();
            _actionHistory.Clear();
            if (_hasBlueprint)
                SendMessageToPC(Player, $"Remaining licensed runs: {_activeBlueprint.LicensedRuns - 1}");
            StatusText = string.Empty;
            StatusColor = GuiColor.Green;
            IsInCraftMode = true;
            IsInSetupMode = false;
            IsClosable = false;
            RefreshRecipeStats();
            ApplyImmobility();
        }

        private bool ProcessBlueprintRequirements()
        {
            if (!_hasBlueprint)
                return true;

            if (!GetIsObjectValid(_blueprintItem) || GetItemPossessor(_blueprintItem) != Player)
            { StatusText = "The selected blueprint is no longer in your inventory."; StatusColor = GuiColor.Red; return false; }
            var blueprintDetails = Craft.GetBlueprintDetails(_blueprintItem);

            if (blueprintDetails.LicensedRuns <= 0)
            {
                StatusText = $"No licensed runs remaining!";
                StatusColor = GuiColor.Red;

                return false;
            }

            var cost = Craft.CalculateBlueprintCraftCreditCost(_blueprintItem);
            if (GetGold(Player) < cost)
            {
                StatusText = $"Insufficient credits!";
                StatusColor = GuiColor.Red;

                return false;
            }

            return true;
        }

        public Action OnClickManualCraft() => () =>
        {
            if (!IsInSetupMode || _isSettling || _session != null)
                return;
            if (CraftingJournal.Get(Player) != null)
            {
                RestoreTransaction(CraftingJournal.Get(Player));
                return;
            }
            if (!Craft.CanPlayerCraftRecipe(Player, _recipe))
            {
                StatusText = "Recipe requirements not met!";
                StatusColor = GuiColor.Red;
                return;
            }

            if (!ProcessBlueprintRequirements()) return;
            var selected = _selectedEnhancementItems.Values.GroupBy(item => item).ToDictionary(group => group.Key, group => group.Count());
            if (selected.Any(entry => !GetIsObjectValid(entry.Key) || GetItemPossessor(entry.Key) != Player || GetItemStackSize(entry.Key) < entry.Value))
            { StatusText = "A selected enhancement is no longer available. Select it again."; StatusColor = GuiColor.Red; return; }
            var recipe = Craft.GetRecipe(_recipe);
            var inventory = CraftingJournal.Inventory(Player).Select(item => new CraftComponentStack(item, GetResRef(item),
                GetItemStackSize(item) - (selected.TryGetValue(item, out var reserved) ? reserved : 0))).ToArray();
            var components = CraftComponentBudget.Plan(recipe.Components, inventory);
            if (components.Count == 0 && recipe.Components.Count > 0)
            { StatusText = "Required components are missing."; StatusColor = GuiColor.Red; return; }
            _components.Clear();
            _components.AddRange(components.Select(entry => CraftingJournal.SerializeQuantity(entry.Item, entry.Quantity)));
            var reservations = components.Concat(selected.Select(entry => new CraftComponentReservation(entry.Key, entry.Value))).ToArray();
            _activeBlueprint = _hasBlueprint ? Craft.GetBlueprintDetails(_blueprintItem) : null;
            var session = CreateSessionSnapshot();
            CraftingJournal.Begin(Player, _recipe, session, reservations, _components, EnhancementData(), _blueprintItem);
            SwitchToCraftMode(session);
        };

        private int CalculateXP(
            RecipeDetail recipe,
            int playerLevel,
            int blueprintLevel,
            bool firstTime,
            float qualityPercent, int? frozenBaseXP = null)
        {
            var xp = frozenBaseXP ?? Craft.GetBaseRecipeXP(recipe, playerLevel);
            // 20% bonus for the first time.
            if (firstTime)
                xp += (int)(xp * 0.20f);
            xp += (int)(xp * (blueprintLevel * 0.5f));
            xp += (int)(xp * qualityPercent);

            return xp;
        }

        private void ApplyProperty(uint item, ItemProperty ip)
        {
            Craft.ApplyCraftedItemProperty(item, ip);
        }

        private void PrepareSuccessRewards(CraftingTransaction transaction)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var recipe = transaction.RecipeRewards?.ToRecipe() ?? Craft.GetRecipe(_recipe);
            var item = CreateObject(ObjectType.Item, recipe.Resref, GetLocation(Player));
            if (!GetIsObjectValid(item)) throw new InvalidOperationException("Unable to create the crafted item.");
            SetItemStackSize(item, recipe.Quantity);
            SetLocalBool(item, Item.PlayerProducedItemVariable, true);
            try
            {
                var firstTime = !dbPlayer.CraftedRecipes.ContainsKey(_recipe);
                var propertyTransferChance = (int)(((float)_quality / (float)_maxQuality) * 100);
                var qualityPercent = (float)_quality / (float)_maxQuality;

                // Vendor bonus scales with recipe level (stronger at high level) and craft quality.
                // Tuned so high-level crafts are not trivial to vendor-trash vs mat cost; low quality still gets a floor.
                const float LevelBucketMultiplier = 48f;
                const float LevelScalingPerRecipeLevel = 9f;
                var levelBonus = LevelBucketMultiplier * ((recipe.Level / 10f) + 1) + LevelScalingPerRecipeLevel * recipe.Level;
                var scaledByQuality = (int)Math.Round(levelBonus * qualityPercent);
                var minimumVendorBonus = Math.Max(25, (int)Math.Round(recipe.Level * 1.6f));
                const float CraftedVendorBonusMultiplier = 1.225f;
                var addGoldPiece = (int)Math.Round(Math.Max(scaledByQuality, minimumVendorBonus) * CraftedVendorBonusMultiplier);
                ItemPlugin.SetAddGoldPieceValue(item, addGoldPiece);

                // Apply item properties provided by enhancements, provided the transfer check passes.
                var allProperties = _itemPropertiesEnhancement1
                    .Concat(_itemPropertiesEnhancement2)
                    .Concat(_itemPropertiesEnhancement3)
                    .Concat(_itemPropertiesEnhancement4)
                    .Concat(_itemPropertiesEnhancement5)
                    .Concat(_itemPropertiesEnhancement6)
                    .Concat(_itemPropertiesEnhancement7)
                    .Concat(_itemPropertiesEnhancement8)
                    .ToList();
                for (var index = 0; index < allProperties.Count; index++)
                {
                    var propertiesToApply = new List<ItemProperty> { allProperties[index] };
                    if (GetItemPropertyType(allProperties[index]) == ItemPropertyType.DMG &&
                        index + 1 < allProperties.Count &&
                        GetItemPropertyType(allProperties[index + 1]) == ItemPropertyType.WeaponDamageType)
                    {
                        propertiesToApply.Add(allProperties[index + 1]);
                        index++;
                    }

                    if (Random.D100(1) <= propertyTransferChance)
                    {
                        foreach (var property in propertiesToApply)
                        {
                            ApplyProperty(item, property);
                        }

                        SendMessageToPC(Player, ColorToken.Green("Enhancement applied successfully."));
                    }
                    else
                    {
                        SendMessageToPC(Player, ColorToken.Red("Enhancement failed to apply."));
                    }
                }

                // Food items have increased duration based on quality percentage
                if (recipe.Category == RecipeCategoryType.Food && (int)qualityPercent > 0)
                {
                    var durationBonus = (int)qualityPercent;
                    var ip = ItemPropertyCustom(ItemPropertyType.FoodBonus, (int)FoodItemPropertySubType.Duration, durationBonus);
                    BiowareXP2.IPSafeAddItemProperty(item, ip, 0.0f, AddItemPropertyPolicy.IgnoreExisting, false, false);

                    // Also increase charges based on the blueprint upgrade level
                    if (_hasBlueprint)
                    {
                        var charges = GetItemCharges(item) + _activeBlueprint.Level;
                        SetItemCharges(item, charges);
                    }
                }

                ProcessBlueprintBonuses(item, recipe);


                var xp = CalculateXP(recipe, transaction.Session.SkillRank, _hasBlueprint ? _activeBlueprint.Level : 0, firstTime, qualityPercent, transaction.RecipeRewards?.BaseXP);
                CraftingJournal.PrepareRewards(Player, transaction, new[] { ObjectPlugin.Serialize(item) }, xp, firstTime);
            }
            finally { DestroyObject(item); }
        }

        private void ProcessSuccess()
        {
            if (_session == null || _session.Status != CraftSessionStatus.Succeeded || !_settlement.TryClaim(_session)) return;
            _isSettling = true; IsInCraftMode = false; IsInSetupMode = false;
            try
            {
                var transaction = CraftingJournal.Get(Player);
                if (transaction.Phase != CraftingTransactionPhase.RewardsReady) PrepareSuccessRewards(transaction);
                var delivered = CraftingJournal.DeliverRewards(Player, transaction);
                ClearReservedState(); SwitchToSetUpMode(); LoadCraftingState(); RefreshRecipeStats();
                StatusText = delivered ? "Successfully created the item!" : "Crafting rewards are saved. Reopen crafting to collect pending rewards.";
                StatusColor = delivered ? GuiColor.Green : GuiColor.Red;
            }
            finally { _isSettling = false; RemoveImmobility(); }
        }


        private void ProcessBlueprintBonuses(uint item, RecipeDetail recipe)
        {
            if (!_hasBlueprint)
                return;

            // Random bonuses
            for (var currentBonus = 1; currentBonus <= _activeBlueprint.ItemBonuses; currentBonus++)
            {
                var tier = currentBonus;

                // Stat pool tier is based on the recipe level.
                // This ensures top-end stats don't get applied to a low-tier weapon, for balancing purposes.
                if (recipe.Level >= 0 && recipe.Level <= 20 && tier > 1)
                    tier = 1;
                else if (recipe.Level >= 21 && recipe.Level <= 40 && tier > 2)
                    tier = 2;

                var bonus = _blueprintBonuses.PickBonus(recipe.EnhancementType, tier, recipe.IsItemIntendedForCrafting);
                if (bonus == null)
                    continue;

                foreach (var ip in Craft.BuildItemPropertiesForEnhancement(bonus.Type, bonus.Amount, bonus.DamageType))
                {
                    ApplyProperty(item, ip);
                }

                var subTypeDetail = Craft.GetEnhancementSubType(bonus.Type);
                var bonusName = bonus.DamageType != CombatDamageType.Invalid &&
                                !bonus.DamageType.IsPhysicalDamageType()
                    ? $"{subTypeDetail.Name} - {bonus.DamageType}"
                    : subTypeDetail.Name;
                SendMessageToPC(Player, ColorToken.Green($"Blueprint Bonus applied: {bonusName} +{bonus.Amount}"));
            }

            // Guaranteed bonuses
            foreach (var ip in _activeBlueprint.GuaranteedBonuses)
            {
                ApplyProperty(item, ip);
            }

            // Identify the blueprint level on the finished item
            var blueprintLevelIP = ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.Level, _activeBlueprint.Level);
            BiowareXP2.IPSafeAddItemProperty(item, blueprintLevelIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
        }

        private void ProcessFailure()
        {
            if (_session == null || _session.Status is not (CraftSessionStatus.Failed or CraftSessionStatus.Aborted) || !_settlement.TryClaim(_session)) return;
            _isSettling = true; IsInCraftMode = false; IsInSetupMode = false;
            try
            {
                var transaction = CraftingJournal.Get(Player);
                if (transaction.Phase != CraftingTransactionPhase.RewardsReady)
                {
                    var surviving = transaction.Enhancements.Concat(transaction.Components)
                        .Where(data => !string.IsNullOrWhiteSpace(data)).Where(_ => Random.D100(1) > 65).ToArray();
                    var recipe = transaction.RecipeRewards?.ToRecipe() ?? Craft.GetRecipe(_recipe);
                    var xp = (int)(CalculateXP(recipe, transaction.Session.SkillRank, _hasBlueprint ? _activeBlueprint.Level : 0, false, 0f, transaction.RecipeRewards?.BaseXP) * 0.15f);
                    CraftingJournal.PrepareRewards(Player, transaction, surviving, xp, false);
                }
                var delivered = CraftingJournal.DeliverRewards(Player, transaction);
                ClearReservedState(); SwitchToSetUpMode(); LoadCraftingState(); RefreshRecipeStats();
                StatusText = delivered ? "Crafting failed. Surviving materials were returned." : "Surviving materials are saved. Reopen crafting to collect pending rewards.";
                StatusColor = GuiColor.Red;
            }
            finally { _isSettling = false; RemoveImmobility(); }
        }


        private string[] EnhancementData() => new[] { _enhancement1, _enhancement2, _enhancement3, _enhancement4, _enhancement5, _enhancement6, _enhancement7, _enhancement8 };

        private void ClearReservedState()
        {
            _enhancement1 = _enhancement2 = _enhancement3 = _enhancement4 = _enhancement5 = _enhancement6 = _enhancement7 = _enhancement8 = string.Empty;
            foreach (var properties in new[] { _itemPropertiesEnhancement1, _itemPropertiesEnhancement2, _itemPropertiesEnhancement3, _itemPropertiesEnhancement4,
                _itemPropertiesEnhancement5, _itemPropertiesEnhancement6, _itemPropertiesEnhancement7, _itemPropertiesEnhancement8 }) properties.Clear();
            _selectedEnhancementItems.Clear(); _components.Clear();
        }

        public static void RecoverForPlayer(uint player)
        {
            if (!GetIsObjectValid(player)) return;
            if (OpenCraftViews.TryGetValue(player, out var view))
            {
                var receipt = CraftingJournal.Get(player);
                if (receipt != null && !view.IsInCraftMode && !view._isSettling) view.RestoreTransaction(receipt);
                return;
            }
            var pending = CraftingJournal.Get(player);
            if (pending == null) { CraftingJournal.RemoveConsumedItems(player); return; }
            if (!Gui.IsWindowOpen(player, GuiWindowType.Craft))
                Gui.TogglePlayerWindow(player, GuiWindowType.Craft, new CraftPayload(pending.Recipe, OBJECT_INVALID));
        }

        private void RestoreTransaction(CraftingTransaction transaction)
        {
            CraftingJournal.CompletePreparation(Player, transaction);
            _recipe = transaction.Recipe; _session = transaction.Session; _settlement = new CraftSessionSettlement();
            RecipeName = $"Recipe: {Cache.GetItemNameByResref(Craft.GetRecipe(_recipe).Resref)}";
            RecipeLevel = $"Recipe level: {Craft.GetRecipe(_recipe).Level}";
            _hasBlueprint = !string.IsNullOrWhiteSpace(transaction.BlueprintData);
            var blueprint = _hasBlueprint ? ObjectPlugin.Deserialize(transaction.BlueprintData) : OBJECT_INVALID;
            try
            {
                if (_hasBlueprint) _activeBlueprint = Craft.GetBlueprintDetails(blueprint);
                var propertyLists = new[] { _itemPropertiesEnhancement1, _itemPropertiesEnhancement2, _itemPropertiesEnhancement3, _itemPropertiesEnhancement4,
                    _itemPropertiesEnhancement5, _itemPropertiesEnhancement6, _itemPropertiesEnhancement7, _itemPropertiesEnhancement8 };
                for (var index = 0; index < transaction.Enhancements.Count && index < propertyLists.Length; index++)
                {
                    if (string.IsNullOrWhiteSpace(transaction.Enhancements[index])) continue;
                    var item = ObjectPlugin.Deserialize(transaction.Enhancements[index]);
                    try { CalculateProgressPenaltyAndProcessItemProperties(item, propertyLists[index]); }
                    finally { DestroyObject(item); }
                }
                if (_session.Status == CraftSessionStatus.Active)
                { _session = _session with { Status = CraftSessionStatus.Aborted }; CraftingJournal.SaveSession(Player, _session); }
                if (_session.Status == CraftSessionStatus.Succeeded) ProcessSuccess(); else ProcessFailure();
            }
            finally
            {
                if (GetIsObjectValid(blueprint)) DestroyObject(blueprint);
                _hasBlueprint = false; _blueprintItem = OBJECT_INVALID;
                ClearReservedState(); RemoveImmobility();
            }
            RefreshSetupRecipe();
            var delivered = CraftingJournal.Get(Player) == null;
            CraftText = delivered ? "Craft" : "Collect pending rewards";
            IsInSetupMode = true; IsClosable = true;
            SendMessageToPC(Player, delivered ? "Your interrupted crafting session has been settled." : "Your crafting rewards remain saved until they can be collected.");
            RefreshRecipeStats(); RestoreCraftContent();
        }

        private void ClearActionEvents()
        {
            foreach (var key in _actionEventKeys) Gui.UnregisterElementEvents(key);
            _actionEventKeys.Clear();
        }

        private void RefreshActionLayout()
        {
            if (WindowToken <= 0) return;
            ClearActionEvents();
            var state = _session ?? _setupSession;
            if (state == null) return;
            var group = new GuiGroup<CraftViewModel>();
            group.SetShowBorder(false).SetScrollbars(NuiScrollbars.None);
            group.SetMargin(0f);
            group.AddColumn(column =>
            {
                var actions = state.RulesVersion == 2 ? CraftActionDetail.Actions : CraftActionDetail.LegacyActions;
                for (var offset = 0; offset < actions.Count; offset += 3)
                {
                    var start = offset;
                    column.AddRow(row =>
                    {
                        foreach (var action in actions.Skip(start).Take(3))
                        {
                            var preview = CraftActionEvaluator.Preview(state, action.Type);
                            var sessionId = state.Id.ToString("N");
                            var revision = state.ActionCount;
                            var type = (int)action.Type;
                            var button = row.AddButton().SetId($"craft_{sessionId}_{revision}_{type}")
                                .SetText(preview.ButtonText).SetTooltip(preview.Description)
                                .SetIsEnabled(IsInCraftMode && preview.IsAvailable).SetHeight(30f).SetMargin(0f)
                                .BindOnClicked(model => model.OnCraftAction(sessionId, revision, type));
                            var key = Gui.BuildEventKey(Gui.BuildWindowId(GuiWindowType.Craft), button.Id);
                            Gui.RegisterElementEvent(key, "click", button.Events["click"]);
                            _actionEventKeys.Add(key);
                        }
                    });
                }
            });
            SetGroupLayout(CraftActionsElement, group.ToJson());
        }

        public Action OnCraftAction(string sessionId, int revision, int type) =>
            Guid.TryParseExact(sessionId, "N", out var id) && Enum.IsDefined(typeof(CraftActionType), type)
                ? CraftAction(new CraftActionRequest(id, revision, (CraftActionType)type)) : () => { };

        private Action CraftAction(CraftActionRequest request)
        {
            return () =>
            {
                if (!IsInCraftMode || _isSettling || _session == null || request == null)
                    return;
                if (request.SessionId != _session.Id || request.ExpectedActionCount != _session.ActionCount)
                { StatusText = "This action belongs to an earlier crafting state."; StatusColor = GuiColor.Red; return; }
                if (GetIsDead(Player) || TetherObject != OBJECT_INVALID &&
                    (!GetIsObjectValid(TetherObject) || GetDistanceBetween(Player, TetherObject) > 5f))
                {
                    CloseForPlayer(Player);
                    return;
                }
                var outcome = CraftActionEvaluator.Resolve(_session, request, () => Random.D100(1));
                if (!outcome.Accepted)
                {
                    StatusText = outcome.Reason;
                    StatusColor = GuiColor.Red;
                    return;
                }
                var previous = _session;
                var gains = new CraftActionRecord(outcome.Session.ActionCount, request.Action, outcome.Succeeded,
                    previous.CurrentCondition, outcome.Session.CurrentCondition, outcome.Preview.CPCost,
                    outcome.Session.CP - previous.CP + outcome.Preview.CPCost, outcome.Preview.DurabilityCost,
                    outcome.Session.Durability - Math.Max(0, previous.Durability - outcome.Preview.DurabilityCost),
                    outcome.Session.Progress - previous.Progress, outcome.Session.Quality - previous.Quality);
                CraftingJournal.SaveSession(Player, outcome.Session, gains);
                _session = outcome.Session;
                var preview = outcome.Preview;
                StatusText = $"{preview.Action.Name}: {(outcome.Succeeded ? "Success!" : "FAILURE")}";
                StatusColor = outcome.Succeeded ? GuiColor.Green : GuiColor.Red;
                _actionHistory.Add($"{_session.ActionCount}. {preview.Action.Name}: {(outcome.Succeeded ? "success" : "failure")}; " +
                    $"-{preview.CPCost} CP, -{preview.DurabilityCost} durability; " +
                    $"+{gains.Progress} progress, +{gains.Quality} quality, +{gains.DurabilityRestored} durability, +{gains.CPRestored} CP; " +
                    $"{gains.Condition} > {gains.NextCondition}.");

                if (_session.Status == CraftSessionStatus.Succeeded) ProcessSuccess();
                else if (_session.Status == CraftSessionStatus.Failed) ProcessFailure();
                RefreshRecipeStats();
            };
        }


        public void Refresh(SkillXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            RefreshYourSkill(dbPlayer);
        }
    }
}
