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

        public const string CraftContentElement = "CraftContent";
        public const string CraftContentPartial = "CraftContentPartial";
        private static readonly Dictionary<uint, CraftViewModel> OpenCraftViews = new();
        private CraftSession _session;
        private CraftSession _setupSession;
        private CraftSessionSettlement _settlement = new();
        private bool _isSettling;
        private int _enhancementProgressPenalty;
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

        public bool IsRapidSynthesisEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsCarefulSynthesisEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsBasicTouchEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsStandardTouchEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsPreciseTouchEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsMastersMendEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsSteadyHandEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsMuscleMemoryEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsVenerationEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsWasteNotEnabled
        {
            get => Get<bool>();
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

        public bool IsBasicSynthesisEnabled { get => Get<bool>(); set => Set(value); }
        public string BuffSummary { get => Get<string>(); set => Set(value); }
        public string QualityRewards { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> ActionHistory { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public string BasicSynthesisText { get => Get<string>(); set => Set(value); }
        public string BasicSynthesisTooltip { get => Get<string>(); set => Set(value); }
        public string RapidSynthesisText { get => Get<string>(); set => Set(value); }
        public string RapidSynthesisTooltip { get => Get<string>(); set => Set(value); }
        public string CarefulSynthesisText { get => Get<string>(); set => Set(value); }
        public string CarefulSynthesisTooltip { get => Get<string>(); set => Set(value); }
        public string BasicTouchText { get => Get<string>(); set => Set(value); }
        public string BasicTouchTooltip { get => Get<string>(); set => Set(value); }
        public string StandardTouchText { get => Get<string>(); set => Set(value); }
        public string StandardTouchTooltip { get => Get<string>(); set => Set(value); }
        public string PreciseTouchText { get => Get<string>(); set => Set(value); }
        public string PreciseTouchTooltip { get => Get<string>(); set => Set(value); }
        public string MastersMendText { get => Get<string>(); set => Set(value); }
        public string MastersMendTooltip { get => Get<string>(); set => Set(value); }
        public string SteadyHandText { get => Get<string>(); set => Set(value); }
        public string SteadyHandTooltip { get => Get<string>(); set => Set(value); }
        public string MuscleMemoryText { get => Get<string>(); set => Set(value); }
        public string MuscleMemoryTooltip { get => Get<string>(); set => Set(value); }
        public string VenerationText { get => Get<string>(); set => Set(value); }
        public string VenerationTooltip { get => Get<string>(); set => Set(value); }
        public string WasteNotText { get => Get<string>(); set => Set(value); }
        public string WasteNotTooltip { get => Get<string>(); set => Set(value); }

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
            _actionHistory.Clear();
            _enhancementProgressPenalty = 0;

            _recipe = initialPayload.Recipe;
            _blueprintItem = initialPayload.BlueprintItem;
            var recipe = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            _hasBlueprint = blueprint.Recipe != RecipeType.Invalid;

            var itemName = Cache.GetItemNameByResref(recipe.Resref);

            SwitchToSetUpMode();
            StatusColor = GuiColor.Green;
            StatusText = string.Empty;

            var enhancementSlots = recipe.EnhancementSlots + blueprint.EnhancementSlots;

            IsEnhancement1Visible = enhancementSlots >= 1;
            IsEnhancement2Visible = enhancementSlots >= 2;
            IsEnhancement3Visible = enhancementSlots >= 3;
            IsEnhancement4Visible = enhancementSlots >= 4;
            IsEnhancement5Visible = enhancementSlots >= 5;
            IsEnhancement6Visible = enhancementSlots >= 6;
            IsEnhancement7Visible = enhancementSlots >= 7;
            IsEnhancement8Visible = enhancementSlots >= 8;

            CraftText = _hasBlueprint
                ? $"Craft [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]"
                : "Craft";
            RecipeName = $"Recipe: {recipe.Quantity}x {itemName}";
            RecipeLevel = $"Recipe level: {recipe.Level}";

            var (recipeDescription, recipeColors) = Craft.BuildRecipeDetail(Player, _recipe, blueprint);
            RecipeDescription = recipeDescription;
            RecipeColors = recipeColors;

            IsRapidSynthesisEnabled = false;
            IsCarefulSynthesisEnabled = false;

            IsBasicTouchEnabled = false;
            IsStandardTouchEnabled = false;
            IsPreciseTouchEnabled = false;

            IsMastersMendEnabled = false;
            IsSteadyHandEnabled = false;
            IsMuscleMemoryEnabled = false;

            IsVenerationEnabled = false;
            IsWasteNotEnabled = false;

            LoadCraftingState();
            RefreshRecipeStats();
            ChangePartialView(CraftContentElement, CraftContentPartial);
        }

        protected override void OnModalClosedRestore() => ChangePartialView(CraftContentElement, CraftContentPartial);

        private CraftSession CreateSessionSnapshot()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var recipe = Craft.GetRecipe(_recipe);
            var equipmentCP = dbPlayer.CPBonus.TryGetValue(recipe.Skill, out var cp) ? cp : 0;
            return CraftSession.CreateLegacy(dbPlayer.Skills[recipe.Skill].Rank, recipe.Level,
                Craft.GetRecipeLevelDetail(recipe.Level), Stat.CalculateCraftsmanship(Player, recipe.Skill),
                Stat.CalculateControl(Player, recipe.Skill), equipmentCP, _enhancementProgressPenalty);
        }

        private void LoadCraftingState() => _setupSession = CreateSessionSnapshot();

        private void RefreshActionPreviews()
        {
            var state = _session ?? _setupSession;
            if (state == null) return;
            var basicSynthesis = CraftActionEvaluator.Preview(state, CraftActionType.BasicSynthesis);
            BasicSynthesisText = basicSynthesis.ButtonText;
            BasicSynthesisTooltip = basicSynthesis.Description;
            IsBasicSynthesisEnabled = IsInCraftMode && basicSynthesis.IsAvailable;
            var rapidSynthesis = CraftActionEvaluator.Preview(state, CraftActionType.RapidSynthesis);
            RapidSynthesisText = rapidSynthesis.ButtonText;
            RapidSynthesisTooltip = rapidSynthesis.Description;
            IsRapidSynthesisEnabled = IsInCraftMode && rapidSynthesis.IsAvailable;
            var carefulSynthesis = CraftActionEvaluator.Preview(state, CraftActionType.CarefulSynthesis);
            CarefulSynthesisText = carefulSynthesis.ButtonText;
            CarefulSynthesisTooltip = carefulSynthesis.Description;
            IsCarefulSynthesisEnabled = IsInCraftMode && carefulSynthesis.IsAvailable;
            var basicTouch = CraftActionEvaluator.Preview(state, CraftActionType.BasicTouch);
            BasicTouchText = basicTouch.ButtonText;
            BasicTouchTooltip = basicTouch.Description;
            IsBasicTouchEnabled = IsInCraftMode && basicTouch.IsAvailable;
            var standardTouch = CraftActionEvaluator.Preview(state, CraftActionType.StandardTouch);
            StandardTouchText = standardTouch.ButtonText;
            StandardTouchTooltip = standardTouch.Description;
            IsStandardTouchEnabled = IsInCraftMode && standardTouch.IsAvailable;
            var preciseTouch = CraftActionEvaluator.Preview(state, CraftActionType.PreciseTouch);
            PreciseTouchText = preciseTouch.ButtonText;
            PreciseTouchTooltip = preciseTouch.Description;
            IsPreciseTouchEnabled = IsInCraftMode && preciseTouch.IsAvailable;
            var mastersMend = CraftActionEvaluator.Preview(state, CraftActionType.MastersMend);
            MastersMendText = mastersMend.ButtonText;
            MastersMendTooltip = mastersMend.Description;
            IsMastersMendEnabled = IsInCraftMode && mastersMend.IsAvailable;
            var steadyHand = CraftActionEvaluator.Preview(state, CraftActionType.SteadyHand);
            SteadyHandText = steadyHand.ButtonText;
            SteadyHandTooltip = steadyHand.Description;
            IsSteadyHandEnabled = IsInCraftMode && steadyHand.IsAvailable;
            var muscleMemory = CraftActionEvaluator.Preview(state, CraftActionType.MuscleMemory);
            MuscleMemoryText = muscleMemory.ButtonText;
            MuscleMemoryTooltip = muscleMemory.Description;
            IsMuscleMemoryEnabled = IsInCraftMode && muscleMemory.IsAvailable;
            var veneration = CraftActionEvaluator.Preview(state, CraftActionType.Veneration);
            VenerationText = veneration.ButtonText;
            VenerationTooltip = veneration.Description;
            IsVenerationEnabled = IsInCraftMode && veneration.IsAvailable;
            var wasteNot = CraftActionEvaluator.Preview(state, CraftActionType.WasteNot);
            WasteNotText = wasteNot.ButtonText;
            WasteNotTooltip = wasteNot.Description;
            IsWasteNotEnabled = IsInCraftMode && wasteNot.IsAvailable;
            var buffs = new List<string>();
            if (state.SteadyHandActive) buffs.Add("Steady Hand: next synthesis");
            if (state.MuscleMemoryActive) buffs.Add("Muscle Memory: next touch");
            if (state.VenerationCharges > 0) buffs.Add($"Veneration: {state.VenerationCharges} paid syntheses");
            if (state.WasteNotCharges > 0) buffs.Add($"Waste Not: {state.WasteNotCharges} durability-spending actions");
            BuffSummary = buffs.Count == 0 ? "Active preparations: none" : string.Join(" | ", buffs);
            var qualityChance = (int)((float)state.Quality / state.MaxQuality * 100);
            QualityRewards = $"Quality: {qualityChance}% transfer chance per enhancement property group; " +
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
                ProcessFailure();
            }
            // Closing the window before entering craft mode returns the items to the player.
            else
            {
                if (!string.IsNullOrWhiteSpace(_enhancement1))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement1);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement1 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement2))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement2);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement2 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement3))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement3);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement3 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement4))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement4);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement4 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement5))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement5);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement5 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement6))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement6);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement6 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement7))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement7);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement7 = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace(_enhancement8))
                {
                    var item = ObjectPlugin.Deserialize(_enhancement8);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement8 = string.Empty;
                }

                foreach (var serialized in _components)
                {
                    var item = ObjectPlugin.Deserialize(serialized);
                    ObjectPlugin.AcquireItem(Player, item);
                }

                _itemPropertiesEnhancement1.Clear();
                _itemPropertiesEnhancement2.Clear();
                _itemPropertiesEnhancement3.Clear();
                _itemPropertiesEnhancement4.Clear();
                _itemPropertiesEnhancement5.Clear();
                _itemPropertiesEnhancement6.Clear();
                _itemPropertiesEnhancement7.Clear();
                _itemPropertiesEnhancement8.Clear();
                _components.Clear();
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
                    _enhancement1 = ObjectPlugin.Serialize(item);
                    Enhancement1Tooltip = GetName(item);
                    Enhancement1Resref = Item.GetIconResref(item);
                    _enhancementProgressPenalty += progressPenalty;

                    DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement1);
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
                    _enhancement2 = ObjectPlugin.Serialize(item);
                    Enhancement2Tooltip = GetName(item);
                    Enhancement2Resref = Item.GetIconResref(item);
                    _enhancementProgressPenalty += progressPenalty;

                    DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement2);
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
                        _enhancement3 = ObjectPlugin.Serialize(item);
                        Enhancement3Tooltip = GetName(item);
                        Enhancement3Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement3);
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
                        _enhancement4 = ObjectPlugin.Serialize(item);
                        Enhancement4Tooltip = GetName(item);
                        Enhancement4Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement4);
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
                        _enhancement5 = ObjectPlugin.Serialize(item);
                        Enhancement5Tooltip = GetName(item);
                        Enhancement5Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement5);
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
                        _enhancement6 = ObjectPlugin.Serialize(item);
                        Enhancement6Tooltip = GetName(item);
                        Enhancement6Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement6);
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
                        _enhancement7 = ObjectPlugin.Serialize(item);
                        Enhancement7Tooltip = GetName(item);
                        Enhancement7Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement7);
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
                        _enhancement8 = ObjectPlugin.Serialize(item);
                        Enhancement8Tooltip = GetName(item);
                        Enhancement8Resref = Item.GetIconResref(item);
                        _enhancementProgressPenalty += progressPenalty;

                        DestroyObject(item);
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
                    ObjectPlugin.AcquireItem(Player, item);
                    var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement8);
                    _enhancement8 = string.Empty;
                    Enhancement8Resref = BlankTexture;
                    Enhancement8Tooltip = "Select Enhancement #8";
                    _enhancementProgressPenalty -= progressPenalty;
                    _itemPropertiesEnhancement8.Clear();

                    RefreshRecipeStats();
                });
            }
        };

        private List<uint> GetComponents()
        {
            var components = new List<uint>();
            var recipe = Craft.GetRecipe(_recipe);

            for (var item = GetFirstItemInInventory(Player); GetIsObjectValid(item); item = GetNextItemInInventory(Player))
            {
                var resref = GetResRef(item);
                if(recipe.Components.ContainsKey(resref))
                    components.Add(item);
            }

            return components;
        }

        /// <summary>
        /// Determines if the player has all of the necessary components for this recipe
        /// and aggregates them into a new list.
        /// </summary>
        /// <returns>A list of components which will be used, an empty list if not all components are found.</returns>
        private List<uint> AggregateComponents(List<uint> components)
        {
            var recipe = Craft.GetRecipe(_recipe);
            var inventory = components.Select(item => new CraftComponentStack(item, GetResRef(item), GetItemStackSize(item))).ToList();
            var reservations = CraftComponentBudget.Plan(recipe.Components, inventory);
            var result = new List<uint>();
            foreach (var reservation in reservations)
            {
                var originalQuantity = GetItemStackSize(reservation.Item);
                SetItemStackSize(reservation.Item, reservation.Quantity);
                _components.Add(ObjectPlugin.Serialize(reservation.Item));
                if (reservation.Quantity < originalQuantity)
                    SetItemStackSize(reservation.Item, originalQuantity - reservation.Quantity);
                else
                    DestroyObject(reservation.Item);
                result.Add(reservation.Item);
            }
            return result;
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

            IsRapidSynthesisEnabled = false;
            IsCarefulSynthesisEnabled = false;

            IsBasicTouchEnabled = false;
            IsStandardTouchEnabled = false;
            IsPreciseTouchEnabled = false;

            IsMastersMendEnabled = false;
            IsSteadyHandEnabled = false;
            IsMuscleMemoryEnabled = false;

            IsVenerationEnabled = false;
            IsWasteNotEnabled = false;

            _session = null;
            _enhancementProgressPenalty = 0;

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

        private void SwitchToCraftMode()
        {
            _session = CreateSessionSnapshot();
            _settlement = new CraftSessionSettlement();
            _actionHistory.Clear();
            if (_hasBlueprint)
            {
                _activeBlueprint = Craft.GetBlueprintDetails(_blueprintItem);
                var cost = Craft.CalculateBlueprintCraftCreditCost(_blueprintItem);
                AssignCommand(Player, () => TakeGoldFromCreature(cost, Player, true));

                _activeBlueprint.LicensedRuns--;
                Craft.SetBlueprintDetails(_blueprintItem, _activeBlueprint);

                SendMessageToPC(Player, $"Remaining licensed runs: {_activeBlueprint.LicensedRuns}");

                var (recipeDescription, recipeColors) = Craft.BuildRecipeDetail(Player, _recipe, _activeBlueprint);
                RecipeDescription = recipeDescription;
                RecipeColors = recipeColors;
            }

            StatusText = string.Empty;
            StatusColor = GuiColor.Green;

            IsInCraftMode = true;
            IsInSetupMode = false;
            IsClosable = false;

            RefreshRecipeStats();
            ApplyImmobility();
        }

        private bool ProcessComponents()
        {
            var components = GetComponents();
            var aggregateList = AggregateComponents(components);
            if (aggregateList.Count <= 0)
            {
                StatusText = $"Missing components!";
                StatusColor = GuiColor.Red;

                return false;
            }

            return true;
        }

        private bool ProcessBlueprintRequirements()
        {
            if (!_hasBlueprint)
                return true;

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
            if (!Craft.CanPlayerCraftRecipe(Player, _recipe))
            {
                StatusText = "Recipe requirements not met!";
                StatusColor = GuiColor.Red;
                return;
            }

            if (ProcessBlueprintRequirements() && ProcessComponents())
            {
                SwitchToCraftMode();
            }
        };

        private int CalculateXP(
            RecipeDetail recipe,
            int playerLevel,
            int blueprintLevel,
            bool firstTime,
            float qualityPercent)
        {
            var xp = Craft.GetBaseRecipeXP(recipe, playerLevel);
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

        private void ProcessSuccess()
        {
            if (_session == null ||
                _session.Status != CraftSessionStatus.Succeeded ||
                !_settlement.TryClaim(_session))
                return;
            _isSettling = true;
            IsInCraftMode = false;
            IsInSetupMode = false;
            try
            {
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                var recipe = Craft.GetRecipe(_recipe);
                var item = CreateItemOnObject(recipe.Resref, Player, recipe.Quantity);
                SetLocalBool(item, Item.PlayerProducedItemVariable, true);
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

                ProcessBlueprintBonuses(item);

                // Add the recipe to the completed list (unlocks auto-crafting)
                if (firstTime)
                {
                    dbPlayer.CraftedRecipes[_recipe] = DateTime.UtcNow;
                    DB.Set(dbPlayer);
                }

                // Give XP plus a percent bonus based on the quality achieved.
                var xp = CalculateXP(
                    recipe,
                    dbPlayer.Skills[recipe.Skill].Rank,
                    _hasBlueprint ? _activeBlueprint.Level : 0,
                    firstTime,
                    qualityPercent);
                Skill.GiveSkillXP(Player, recipe.Skill, xp, false, false);

                // Clean up and return to the Set Up mode.
                _itemPropertiesEnhancement1.Clear();
                _itemPropertiesEnhancement2.Clear();
                _itemPropertiesEnhancement3.Clear();
                _itemPropertiesEnhancement4.Clear();
                _itemPropertiesEnhancement5.Clear();
                _itemPropertiesEnhancement6.Clear();
                _itemPropertiesEnhancement7.Clear();
                _itemPropertiesEnhancement8.Clear();
                _enhancement1 = string.Empty;
                _enhancement2 = string.Empty;
                _enhancement3 = string.Empty;
                _enhancement4 = string.Empty;
                _enhancement5 = string.Empty;
                _enhancement6 = string.Empty;
                _enhancement7 = string.Empty;
                _enhancement8 = string.Empty;
                _components.Clear();
                SwitchToSetUpMode();
                LoadCraftingState();
                RefreshRecipeStats();
                StatusText = "Successfully created the item!";
                StatusColor = GuiColor.Green;

                Log.Write(LogGroup.Crafting, $"{GetName(Player)} ({GetObjectUUID(Player)}) successfully crafted '{GetName(item)}'.");
            }
            finally
            {
                _isSettling = false;
                RemoveImmobility();
            }
        }


        private void ProcessBlueprintBonuses(uint item)
        {
            if (!_hasBlueprint)
                return;

            // Random bonuses
            var recipe = Craft.GetRecipe(_recipe);
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
            if (_session == null ||
                _session.Status is not (CraftSessionStatus.Failed or CraftSessionStatus.Aborted) ||
                !_settlement.TryClaim(_session))
                return;
            _isSettling = true;
            IsInCraftMode = false;
            IsInSetupMode = false;
            try
            {
                var recipe = Craft.GetRecipe(_recipe);
                var playerId = GetObjectUUID(Player);
                var dbPlayer = DB.Get<Player>(playerId);
                const int ChanceToLoseItem = 65;

                // Process enhancements
                if (!string.IsNullOrWhiteSpace(_enhancement1) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement1);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement1 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement2) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement2);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement2 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement3) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement3);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement3 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement4) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement4);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement4 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement5) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement5);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement5 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement6) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement6);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement6 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement7) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement7);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement7 = string.Empty;

                if (!string.IsNullOrWhiteSpace(_enhancement8) && Random.D100(1) > ChanceToLoseItem)
                {
                    var item = ObjectPlugin.Deserialize(_enhancement8);
                    ObjectPlugin.AcquireItem(Player, item);
                }
                _enhancement8 = string.Empty;

                // Process components
                foreach (var serialized in _components)
                {
                    if (Random.D100(1) > ChanceToLoseItem)
                    {
                        var item = ObjectPlugin.Deserialize(serialized);
                        ObjectPlugin.AcquireItem(Player, item);
                    }
                }

                _itemPropertiesEnhancement1.Clear();
                _itemPropertiesEnhancement2.Clear();
                _itemPropertiesEnhancement3.Clear();
                _itemPropertiesEnhancement4.Clear();
                _itemPropertiesEnhancement5.Clear();
                _itemPropertiesEnhancement6.Clear();
                _itemPropertiesEnhancement7.Clear();
                _itemPropertiesEnhancement8.Clear();
                _components.Clear();

                SwitchToSetUpMode();
                LoadCraftingState();
                RefreshRecipeStats();
                StatusText = "Failed to craft the item...";
                StatusColor = GuiColor.Red;

                // 15% of XP is gained for failures.
                var xp = CalculateXP(
                    recipe,
                    dbPlayer.Skills[recipe.Skill].Rank,
                    _hasBlueprint ? _activeBlueprint.Level : 0,
                    false,
                    0f);
                xp = (int)(xp * 0.15f);
                Skill.GiveSkillXP(Player, recipe.Skill, xp, false, false);

                Log.Write(LogGroup.Crafting, $"{GetName(Player)} ({GetObjectUUID(Player)}) failed to craft '{_recipe}'.");
            }
            finally
            {
                _isSettling = false;
                RemoveImmobility();
            }
        }


        private Action CraftAction(CraftActionType type)
        {
            var request = _session == null ? null : new CraftActionRequest(_session.Id, _session.ActionCount, type);
            return () =>
            {
                if (!IsInCraftMode || _isSettling || _session == null || request == null)
                    return;
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
                _session = outcome.Session;
                var preview = outcome.Preview;
                StatusText = $"{preview.Action.Name}: {(outcome.Succeeded ? "Success!" : "FAILURE")}";
                StatusColor = outcome.Succeeded ? GuiColor.Green : GuiColor.Red;
                _actionHistory.Add($"{_session.ActionCount}. {preview.Action.Name}: {(outcome.Succeeded ? "success" : "failure")}; " +
                    $"-{preview.CPCost} CP, -{preview.DurabilityCost} durability; " +
                    $"+{(outcome.Succeeded ? preview.ProgressGain : 0)} progress, +{(outcome.Succeeded ? preview.QualityGain : 0)} quality, " +
                    $"+{(outcome.Succeeded ? preview.DurabilityRestored : 0)} durability.");
                if (_actionHistory.Count > 6) _actionHistory.RemoveAt(0);
                if (_session.Status == CraftSessionStatus.Succeeded) ProcessSuccess();
                else if (_session.Status == CraftSessionStatus.Failed) ProcessFailure();
                RefreshRecipeStats();
            };
        }

        public Action OnClickBasicSynthesis() => CraftAction(CraftActionType.BasicSynthesis);
        public Action OnClickRapidSynthesis() => CraftAction(CraftActionType.RapidSynthesis);
        public Action OnClickCarefulSynthesis() => CraftAction(CraftActionType.CarefulSynthesis);
        public Action OnClickBasicTouch() => CraftAction(CraftActionType.BasicTouch);
        public Action OnClickStandardTouch() => CraftAction(CraftActionType.StandardTouch);
        public Action OnClickPreciseTouch() => CraftAction(CraftActionType.PreciseTouch);
        public Action OnClickMastersMend() => CraftAction(CraftActionType.MastersMend);
        public Action OnClickSteadyHand() => CraftAction(CraftActionType.SteadyHand);
        public Action OnClickMuscleMemory() => CraftAction(CraftActionType.MuscleMemory);
        public Action OnClickVeneration() => CraftAction(CraftActionType.Veneration);
        public Action OnClickWasteNot() => CraftAction(CraftActionType.WasteNot);

        public void Refresh(SkillXPRefreshEvent payload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);
            RefreshYourSkill(dbPlayer);
        }
    }
}
