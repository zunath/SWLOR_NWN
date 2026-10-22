using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Kitchen window: the cooking minigame for every Agriculture recipe. It has two views. Setup shows the recipe, its target
    /// flavor and the enhancement slots, and Start Cooking commits the materials. Cooking binds the engine's meters, forecast
    /// and action previews, and a finished or ruined dish returns to setup.
    /// </summary>
    public class KitchenViewModel : CraftMinigameViewModel<KitchenViewModel, CraftPayload>
    {
        public const string ContentElement = "kitchen_content";
        public const string SetupPartial = "KITCHEN_SETUP";
        public const string CookPartial = "KITCHEN_COOK";

        private const int MaxEnhancementSlots = 4;
        private const string StartText = "Start Cooking";
        private const string SelectText = "Select";
        private const string RemoveText = "Remove";

        private static readonly GuiColor Gold = new(255, 200, 0);

        private RecipeType _recipe;
        private uint _blueprintItem;
        private BlueprintDetail _blueprint;
        private KitchenParameters _parameters;
        private int _slotCount;
        private KitchenEnhancementSlots _slots = new(0);
        private IReadOnlyList<int> _committedSlots = new List<int>();
        private CraftSession _session;
        private KitchenEngine _engine;

        protected override CraftMinigameType Minigame => CraftMinigameType.Kitchen;

        #region Shared

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

        #endregion

        #region Setup

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

        public string TargetText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FailureRuleText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string StartButtonText
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsStartEnabled
        {
            get => Get<bool>();
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

        public GuiBindingList<string> EnhancementTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EnhancementButtons
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        #endregion

        #region Cooking

        public float DonenessPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string DonenessText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float FreshnessPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string FreshnessText
        {
            get => Get<string>();
            set => Set(value);
        }

        public float QualityPercentage
        {
            get => Get<float>();
            set => Set(value);
        }

        public string QualityText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CpText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string LastResultText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> AxisNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<float> AxisValues
        {
            get => Get<GuiBindingList<float>>();
            set => Set(value);
        }

        public GuiBindingList<string> AxisTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string DistanceText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor DistanceColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string HeatText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DriftText
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AromaText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiColor AromaColor
        {
            get => Get<GuiColor>();
            set => Set(value);
        }

        public string ThresholdText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> ActionNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ActionSummaries
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> ActionTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ActionEnabled
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> ActionColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        #endregion

        protected override void Initialize(CraftPayload initialPayload)
        {
            _recipe = initialPayload.Recipe;
            _blueprintItem = initialPayload.BlueprintItem;
            var recipe = Craft.GetRecipe(_recipe);
            _blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            _parameters = recipe.MinigameParameters as KitchenParameters;
            _slotCount = Math.Min(MaxEnhancementSlots, recipe.EnhancementSlots + _blueprint.EnhancementSlots);
            _slots = new KitchenEnhancementSlots(_slotCount);

            AssignDefaults();
            RefreshSetup();
            if (_parameters == null)
                SetStatus("This recipe has no Kitchen settings yet and cannot be cooked.", GuiColor.Red);

            ChangePartialView(ContentElement, SetupPartial);
            StartMinigameSession();
        }

        public override Action OnWindowClosed()
        {
            var settle = base.OnWindowClosed();
            return () =>
            {
                settle();
                _engine = null;
                _session = null;
            };
        }

        private bool HasBlueprint => _blueprint.Recipe != RecipeType.Invalid;

        private void AssignDefaults()
        {
            StatusText = string.Empty;
            StatusColor = GuiColor.Green;

            RecipeName = string.Empty;
            RecipeLevel = string.Empty;
            YourSkill = string.Empty;
            TargetText = string.Empty;
            FailureRuleText = KitchenText.FailureRule();
            StartButtonText = StartText;
            IsStartEnabled = false;
            RecipeDescription = new GuiBindingList<string>();
            RecipeColors = new GuiBindingList<GuiColor>();
            EnhancementTexts = new GuiBindingList<string>();
            EnhancementButtons = new GuiBindingList<string>();

            DonenessPercentage = 0f;
            DonenessText = string.Empty;
            FreshnessPercentage = 0f;
            FreshnessText = string.Empty;
            QualityPercentage = 0f;
            QualityText = string.Empty;
            CpText = string.Empty;
            LastResultText = string.Empty;
            AxisNames = new GuiBindingList<string>();
            AxisValues = new GuiBindingList<float>();
            AxisTexts = new GuiBindingList<string>();
            DistanceText = string.Empty;
            DistanceColor = GuiColor.White;
            HeatText = string.Empty;
            DriftText = string.Empty;
            AromaText = string.Empty;
            AromaColor = GuiColor.White;
            ThresholdText = string.Empty;
            ActionNames = new GuiBindingList<string>();
            ActionSummaries = new GuiBindingList<string>();
            ActionTooltips = new GuiBindingList<string>();
            ActionEnabled = new GuiBindingList<bool>();
            ActionColors = new GuiBindingList<GuiColor>();
        }

        private void SetStatus(string text, GuiColor color)
        {
            StatusText = text;
            StatusColor = color;
        }

        #region Setup view

        private void RefreshSetup()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var rank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
            var stats = BuildStats(dbPlayer, recipe);

            RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
            RecipeLevel = _parameters == null
                ? $"Recipe level: {recipe.Level}"
                : $"Recipe level: {recipe.Level}. Dish: {_parameters.Dish}";
            YourSkill = $"Your Agriculture rank: {rank}. Craftsmanship {stats.Craftsmanship}, Control {stats.Control}, CP {stats.MaxCp}.";
            StartButtonText = HasBlueprint
                ? $"{StartText} [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]"
                : StartText;
            IsStartEnabled = _parameters != null;

            var description = new GuiBindingList<string>();
            var colors = new GuiBindingList<GuiColor>();
            if (_parameters != null)
            {
                var kitchen = KitchenRecipe.From(_parameters, recipe.Level, recipe.Components, Cache.GetItemNameByResref);
                TargetText = KitchenText.TargetText(kitchen.Target);

                description.Add("[KITCHEN]");
                colors.Add(GuiColor.Cyan);
                foreach (var line in kitchen.Lines)
                {
                    description.Add(KitchenText.IngredientLine(line));
                    colors.Add(GuiColor.White);
                }

                description.Add(KitchenText.PantryLine());
                colors.Add(GuiColor.White);
                description.Add($"The dish starts with {kitchen.Freshness} Freshness and is ruined at 0.");
                colors.Add(GuiColor.White);
                description.Add(string.Empty);
                colors.Add(GuiColor.White);
            }

            var (details, detailColors) = Craft.BuildRecipeDetail(Player, _recipe, _blueprint);
            for (var index = 0; index < details.Count; index++)
            {
                description.Add(details[index]);
                colors.Add(detailColors[index]);
            }

            RecipeDescription = description;
            RecipeColors = colors;
            RefreshEnhancements();
        }

        private void RefreshEnhancements()
        {
            var texts = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            for (var slot = 0; slot < _slotCount; slot++)
            {
                var threshold = CraftOutcomeRules.GetEnhancementThreshold(slot + 1);
                var chosen = _slots.IsFilled(slot);
                texts.Add(chosen
                    ? $"Slot {slot + 1} (guaranteed at {threshold}% quality): {_slots.NameAt(slot)}"
                    : $"Slot {slot + 1} (guaranteed at {threshold}% quality): empty");
                buttons.Add(chosen ? RemoveText : SelectText);
            }

            EnhancementTexts = texts;
            EnhancementButtons = buttons;
        }

        private KitchenStats BuildStats(Player dbPlayer, RecipeDetail recipe)
        {
            var rank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
            var gearCp = dbPlayer.CPBonus.TryGetValue(recipe.Skill, out var cp) ? cp : 0;
            return new KitchenStats(
                rank,
                Stat.CalculateCraftsmanship(Player, recipe.Skill),
                Stat.CalculateControl(Player, recipe.Skill),
                gearCp);
        }

        public Action OnClickEnhancement() => () =>
        {
            if (_engine != null)
                return;

            var slot = NuiGetEventArrayIndex();
            if (slot < 0 || slot >= _slotCount)
                return;

            if (_slots.IsFilled(slot))
            {
                _slots.Clear(slot);
                RefreshEnhancements();
                return;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.", item =>
            {
                var problem = ValidateEnhancement(item);
                if (!string.IsNullOrEmpty(problem))
                {
                    FloatingTextStringOnCreature(problem, Player, false);
                    return;
                }

                var reading = CraftEnhancementReader.Read(item, Craft.GetRecipe(_recipe));
                _slots.Fill(slot, item, GetName(item), reading.ProgressPenalty);
                RefreshEnhancements();
            });
        };

        /// <summary>The reason an item cannot be a cooking enhancement for this recipe, or an empty string when it can.</summary>
        private string ValidateEnhancement(uint item)
        {
            var recipe = Craft.GetRecipe(_recipe);
            if (GetItemPossessor(item) != Player)
                return "Item must be in your inventory.";
            if (_slots.Contains(item))
                return "That enhancement is already selected.";
            if (recipe.EnhancementType != RecipeEnhancementType.Food)
                return "This recipe takes no enhancements.";

            var isEnhancement = false;
            var enhancementLevel = -1;
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(property);
                if (type == ItemPropertyType.FoodEnhancement)
                    isEnhancement = true;
                else if (type == ItemPropertyType.EnhancementLevel)
                    enhancementLevel = GetItemPropertyCostTableValue(property);
            }

            if (!isEnhancement || enhancementLevel < 0)
                return "Item must be a cooking enhancement.";
            if (enhancementLevel - recipe.Level > 5)
                return "Enhancement must be within 5 levels of recipe level.";

            return string.Empty;
        }

        public Action OnClickStart() => () =>
        {
            if (_engine != null || _parameters == null)
                return;

            // Build the dish first, so a recipe the Kitchen cannot read never takes the player's materials.
            var recipe = Craft.GetRecipe(_recipe);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var kitchen = KitchenRecipe.From(_parameters, recipe.Level, recipe.Components, Cache.GetItemNameByResref);
            var penalty = _slots.TotalProgressPenalty;
            var engine = new KitchenEngine(
                ServiceMinigameRandom.Instance,
                kitchen,
                BuildStats(dbPlayer, recipe),
                KitchenTuning.EnhancementDoneness(penalty));

            var request = new CraftSessionStartRequest(
                _recipe,
                _blueprintItem,
                _slots.Items);
            var result = CraftSessionService.Start(Player, request);
            if (!result.Succeeded)
            {
                SetStatus(result.Message, GuiColor.Red);
                return;
            }

            _session = result.Session;
            _engine = engine;
            _committedSlots = _slots.FilledSlotNumbers;

            ActionNames = BuildActionNames();
            LastResultText = string.Empty;
            SetStatus("The ingredients are in the pot. Closing this window abandons the dish.", GuiColor.Green);
            RefreshCook();
            ChangePartialView(ContentElement, CookPartial);
        };

        #endregion

        #region Cooking view

        private GuiBindingList<string> BuildActionNames()
        {
            var names = new GuiBindingList<string>();
            foreach (var action in _engine.Actions)
                names.Add(action.Name);

            return names;
        }

        private void RefreshCook()
        {
            var meters = _engine.Meters;
            DonenessPercentage = meters.Progress.Fraction;
            DonenessText = $"Doneness {meters.Progress.Value} / {meters.Progress.Max}";
            FreshnessPercentage = meters.Integrity.Fraction;
            FreshnessText = $"Freshness {meters.Integrity.Value} / {meters.Integrity.Max} (the dish is ruined at 0)";
            QualityPercentage = meters.Quality.Fraction;
            QualityText = $"Quality {meters.QualityPercent}%";
            CpText = $"CP {meters.Cp.Value} / {meters.Cp.Max}";

            var names = new GuiBindingList<string>();
            var values = new GuiBindingList<float>();
            var texts = new GuiBindingList<string>();
            foreach (var axis in _engine.DescribeAxes())
            {
                names.Add(axis.Name);
                values.Add(axis.Value / (float)KitchenTuning.AxisMax);
                texts.Add(KitchenText.AxisText(axis));
            }

            AxisNames = names;
            AxisValues = values;
            AxisTexts = texts;

            var target = _engine.Recipe.Target;
            DistanceText = KitchenText.DistanceText(_engine.DistanceFromCenter, target);
            DistanceColor = _engine.DistanceFromCenter <= target.InnerRadius ? GuiColor.Green : GuiColor.White;
            HeatText = KitchenText.HeatText(_engine.Heat, _engine.CoverTurns);
            DriftText = KitchenText.DriftText(_engine.NextDrift, _engine.EffectiveNextDrift, _engine.Heat);
            AromaText = KitchenText.AromaText(_engine.IsPeakAromaNext);
            AromaColor = _engine.IsPeakAromaNext ? Gold : GuiColor.White;
            ThresholdText = KitchenText.ThresholdText(meters.QualityPercent, _committedSlots);

            var summaries = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            var colors = new GuiBindingList<GuiColor>();
            foreach (var preview in _engine.PreviewAll())
            {
                var line = KitchenText.ActionLine(preview);
                summaries.Add(line.Summary);
                tooltips.Add(line.Tooltip);
                enabled.Add(preview.Enabled);
                colors.Add(line.Tone switch
                {
                    KitchenActionTone.Unavailable => GuiColor.Grey,
                    KitchenActionTone.Finishes => Gold,
                    KitchenActionTone.Ruins => GuiColor.Red,
                    _ => GuiColor.White
                });
            }

            ActionSummaries = summaries;
            ActionTooltips = tooltips;
            ActionEnabled = enabled;
            ActionColors = colors;
        }

        public Action OnClickAction() => () =>
        {
            if (_engine == null)
                return;

            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _engine.Actions.Count)
                return;

            if (!CraftSessionService.IsActive(Player, _session))
            {
                ReturnToSetup("Your cooking was interrupted, so the dish was lost.", GuiColor.Red);
                return;
            }

            var action = _engine.Actions[index];
            var result = _engine.Perform(action.Id);
            if (!result.Performed)
            {
                SetStatus(result.RejectedReason, GuiColor.Red);
                RefreshCook();
                return;
            }

            SetStatus($"{action.Name}: {result.Outcome.Label}.", GuiColor.Green);
            LastResultText = KitchenText.ResultText(result.Outcome);
            if (result.Status == MinigameStatus.Succeeded)
                FinishDish();
            else if (result.Status == MinigameStatus.Failed)
                RuinDish();
            else
                RefreshCook();
        };

        private void FinishDish()
        {
            var quality = _engine.Meters.Quality;
            var dishName = Cache.GetItemNameByResref(Craft.GetRecipe(_recipe).Resref);
            var success = CraftSessionService.Succeed(Player, _session, quality);
            if (success == null)
            {
                ReturnToSetup("Your cooking was interrupted, so the dish was lost.", GuiColor.Red);
                return;
            }

            var message = $"You cooked {dishName} at {quality.Percent}% quality.";
            SendMessageToPC(Player, message);
            ReturnToSetup(message, GuiColor.Green);
        }

        private void RuinDish()
        {
            var settlement = CraftSessionService.Fail(Player, CraftSessionEndReason.Broken);
            if (!settlement.Settled)
            {
                ReturnToSetup("Your cooking was interrupted, so the dish was lost.", GuiColor.Red);
                return;
            }

            var total = settlement.ItemsLost + settlement.ItemsKept;
            var message = $"The dish is ruined! {settlement.ItemsLost} of {total} items were lost.";
            SendMessageToPC(Player, message);
            ReturnToSetup(message, GuiColor.Red);
        }

        private void ReturnToSetup(string message, GuiColor color)
        {
            _engine = null;
            _session = null;
            _committedSlots = new List<int>();
            _slots.ClearAll();
            RefreshSetup();
            SetStatus(message, color);
            ChangePartialView(ContentElement, SetupPartial);
        }

        #endregion
    }
}
