using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Engineering crafting window. Before the craft it selects enhancements and shows what the craft needs; during it
    /// binds the <see cref="CircuitEngine"/>'s meters and previews and forwards each click to it.
    /// </summary>
    public class CircuitCraftViewModel : CraftMinigameViewModel<CircuitCraftViewModel, CraftPayload>
    {
        public const string ContentElement = "circuit_craft_content";
        public const string SetupPartial = "CIRCUIT_SETUP";
        public const string CraftPartial = "CIRCUIT_CRAFT";

        private RecipeType _recipe;
        private uint _blueprintItem;
        private CraftSession _session;
        private CircuitEngine _engine;
        private bool _settling;

        // One entry per enhancement slot, counting from slot 1; an empty slot holds OBJECT_INVALID.
        private readonly List<uint> _slotItems = new();
        private readonly List<string> _slotNames = new();
        private readonly List<int> _slotPenalties = new();

        // One entry per row of the style list: None first, then each style the crafter owns.
        private readonly List<CircuitStyle> _styleRows = new();

        protected override CraftMinigameType Minigame => CraftMinigameType.Circuit;

        public string RecipeName { get => Get<string>(); set => Set(value); }
        public string RecipeLevel { get => Get<string>(); set => Set(value); }
        public string GearSummary { get => Get<string>(); set => Set(value); }
        public string CircuitSummary { get => Get<string>(); set => Set(value); }
        public string NeedsSummary { get => Get<string>(); set => Set(value); }
        public string SlotHeader { get => Get<string>(); set => Set(value); }
        public string RuleText { get => Get<string>(); set => Set(value); }
        public string CraftText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }
        public GuiBindingList<string> RecipeDescription { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RecipeColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public GuiBindingList<string> SlotLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> SlotButtonTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public string StyleHeader { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> StyleLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> StyleButtonTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> StyleButtonEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string AssemblyText { get => Get<string>(); set => Set(value); }
        public float AssemblyFraction { get => Get<float>(); set => Set(value); }
        public string CalibrationText { get => Get<string>(); set => Set(value); }
        public float CalibrationFraction { get => Get<float>(); set => Set(value); }
        public string IntegrityText { get => Get<string>(); set => Set(value); }
        public float IntegrityFraction { get => Get<float>(); set => Set(value); }
        public string CpText { get => Get<string>(); set => Set(value); }
        public string LoadText { get => Get<string>(); set => Set(value); }
        public GuiColor LoadColor { get => Get<GuiColor>(); set => Set(value); }
        public float LoadFraction { get => Get<float>(); set => Set(value); }
        public string LoadRuleText { get => Get<string>(); set => Set(value); }
        public string PerkText { get => Get<string>(); set => Set(value); }
        public string OpportunityText { get => Get<string>(); set => Set(value); }
        public GuiColor OpportunityColor { get => Get<GuiColor>(); set => Set(value); }
        public string ThresholdText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> ActionNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ActionTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ActionTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> ActionEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        protected override void Initialize(CraftPayload initialPayload)
        {
            _recipe = initialPayload.Recipe;
            _blueprintItem = initialPayload.BlueprintItem;
            _session = null;
            _engine = null;
            _settling = false;

            var recipe = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            ResetSlots(recipe.EnhancementSlots + blueprint.EnhancementSlots);

            // The record holds the selected style; a death, a login or a trip to space may have cost the crafter its effect.
            CraftingStyleSelection.Restore(Player);
            CircuitStyles.RemoveUnowned(Player);
            ClearCraftView();
            StatusText = string.Empty;
            StatusColor = GuiColor.Green;
            RefreshSetup();

            ChangePartialView(ContentElement, SetupPartial);
            StartMinigameSession();
        }

        // ---- setup ---------------------------------------------------------------------------------------------------

        private void ResetSlots(int count)
        {
            _slotItems.Clear();
            _slotNames.Clear();
            _slotPenalties.Clear();
            for (var slot = 0; slot < count; slot++)
            {
                _slotItems.Add(OBJECT_INVALID);
                _slotNames.Add(string.Empty);
                _slotPenalties.Add(0);
            }
        }

        private CircuitSetup BuildSetup()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var levelDetail = Craft.GetRecipeLevelDetail(recipe.Level);
            var rank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
            var gearCp = dbPlayer.CPBonus.TryGetValue(recipe.Skill, out var cp) ? cp : 0;

            return new CircuitSetup(
                recipe.MinigameParameters as CircuitParameters,
                recipe.Level,
                levelDetail.Progress,
                levelDetail.Quality,
                levelDetail.Durability,
                rank,
                Stat.CalculateCraftsmanship(Player, recipe.Skill),
                Stat.CalculateControl(Player, recipe.Skill),
                gearCp,
                _slotPenalties.Sum(),
                CircuitPerkStats.FromStats(stat => Stat.GetStatAdjustment(Player, stat)));
        }

        private void RefreshSetup()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            var hasBlueprint = blueprint.Recipe != RecipeType.Invalid;
            var setup = BuildSetup();
            var parameters = setup.Parameters;

            RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
            RecipeLevel = $"Recipe level {recipe.Level}  |  Your Engineering rank {setup.Rank}";
            GearSummary =
                $"Craftsmanship {setup.Craftsmanship} (+{setup.Craftsmanship}% Assembly)  |  Control {setup.Control} (+{setup.Control}% Calibration)  |  CP {setup.MaxCp}";
            CircuitSummary =
                $"Overload line {setup.OverloadLine}  |  Each Load point adds {setup.LoadBoostPercent}% output  |  " +
                $"Work: {parameters.AssemblySharePercent}% Assembly, {100 - parameters.AssemblySharePercent}% Calibration";
            NeedsSummary = setup.EnhancementAssembly > 0
                ? $"Needs {setup.MaxAssembly} Assembly ({setup.EnhancementAssembly} added by enhancements) and {setup.MaxCalibration} Calibration. Integrity {setup.MaxIntegrity}."
                : $"Needs {setup.MaxAssembly} Assembly and {setup.MaxCalibration} Calibration. Integrity {setup.MaxIntegrity}.";
            SlotHeader = _slotItems.Count == 0
                ? "This recipe has no enhancement slots."
                : "Enhancement slots. An enhancement transfers when the quality reaches its slot's threshold.";
            RuleText =
                $"If the piece breaks (Integrity reaches 0) or you close this window mid-craft, each component stack and each enhancement is lost on a roll of {CraftOutcomeRules.ChanceToLoseItemPercent} or lower on a D100. " +
                $"You still earn {(int)(CraftOutcomeRules.FailureXpFraction * 100)}% of the XP.";
            CraftText = hasBlueprint
                ? $"Craft [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]"
                : "Craft";

            var (description, colors) = Craft.BuildRecipeDetail(Player, _recipe, blueprint);
            RecipeDescription = description;
            RecipeColors = colors;
            RefreshSlots();
            RefreshStyles();
        }

        private void RefreshStyles()
        {
            _styleRows.Clear();
            var owned = CircuitStyles.Owned(Player);
            if (owned.Count > 0)
            {
                _styleRows.Add(CircuitStyle.None);
                _styleRows.AddRange(owned);
            }

            var active = CircuitStyles.Active(Player);
            var labels = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            foreach (var style in _styleRows)
            {
                labels.Add(style == CircuitStyle.None
                    ? "None: the Circuit plays by its usual rules."
                    : $"{CircuitStyles.Name(style)}: {CircuitStyles.Description(style)}");
                buttons.Add(style == active ? "Selected" : "Select");
                enabled.Add(style != active);
            }

            StyleHeader = owned.Count == 0
                ? $"Styles: learn a style perk at Engineering {CircuitRules.StyleRank} to choose one here."
                : "Style: pick one to craft with. Only one can be active at a time.";
            StyleLabels = labels;
            StyleButtonTexts = buttons;
            StyleButtonEnabled = enabled;
        }

        public Action OnClickStyle() => () =>
        {
            if (_session != null)
                return;

            var row = NuiGetEventArrayIndex();
            if (row < 0 || row >= _styleRows.Count)
                return;

            CircuitStyles.Select(Player, _styleRows[row]);
            RefreshSetup();
        };

        private void RefreshSlots()
        {
            var labels = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            for (var slot = 0; slot < _slotItems.Count; slot++)
            {
                var threshold = CraftOutcomeRules.GetEnhancementThreshold(slot + 1);
                var filled = _slotItems[slot] != OBJECT_INVALID;
                labels.Add($"Slot {slot + 1} (needs {threshold}% quality): {(filled ? _slotNames[slot] : "empty")}");
                buttons.Add(filled ? "Remove" : "Select");
            }

            SlotLabels = labels;
            SlotButtonTexts = buttons;
        }

        public Action OnClickSlot() => () =>
        {
            if (_session != null)
                return;

            var slot = NuiGetEventArrayIndex();
            if (slot < 0 || slot >= _slotItems.Count)
                return;

            if (_slotItems[slot] != OBJECT_INVALID)
            {
                _slotItems[slot] = OBJECT_INVALID;
                _slotNames[slot] = string.Empty;
                _slotPenalties[slot] = 0;
                RefreshSetup();
                return;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.", item =>
            {
                if (_session != null || !Gui.IsWindowOpen(Player, GuiWindowType.CraftCircuit))
                    return;

                var recipe = Craft.GetRecipe(_recipe);
                var problem = CircuitEnhancementCheck.Problem(Player, item, recipe);
                if (string.IsNullOrEmpty(problem) && _slotItems.Contains(item))
                    problem = "That enhancement is already in a slot.";

                if (!string.IsNullOrEmpty(problem))
                {
                    StatusText = problem;
                    StatusColor = GuiColor.Red;
                    return;
                }

                _slotItems[slot] = item;
                _slotNames[slot] = GetName(item);
                _slotPenalties[slot] = CraftEnhancementReader.Read(item, recipe).ProgressPenalty;
                StatusText = string.Empty;
                RefreshSetup();
            });
        };

        public Action OnClickCraft() => () =>
        {
            if (_session != null)
                return;

            CircuitStyles.RemoveUnowned(Player);
            var result = CraftSessionService.Start(Player, new CraftSessionStartRequest(_recipe, _blueprintItem, _slotItems.ToList()));
            if (!result.Succeeded)
            {
                StatusText = result.Message;
                StatusColor = GuiColor.Red;
                return;
            }

            _session = result.Session;
            _settling = false;
            _engine = new CircuitEngine(ServiceMinigameRandom.Instance, BuildSetup());

            if (result.Session.HasBlueprint)
            {
                var (description, colors) = Craft.BuildRecipeDetail(Player, _recipe, result.Session.Blueprint);
                RecipeDescription = description;
                RecipeColors = colors;
            }

            StatusText = "Choose an action. Each button shows what it will do.";
            StatusColor = GuiColor.Green;
            RefreshCraft();
            ChangePartialView(ContentElement, CraftPartial);
        };

        // ---- crafting ------------------------------------------------------------------------------------------------

        public Action OnClickAction() => () =>
        {
            if (_engine == null || _session == null || _settling)
                return;

            var index = NuiGetEventArrayIndex();
            var actions = _engine.Actions;
            if (index < 0 || index >= actions.Count)
                return;

            if (!CraftSessionService.IsActive(Player, _session))
            {
                ReturnToSetup("Your craft has ended.", GuiColor.Red);
                return;
            }

            var result = _engine.Perform(actions[index].Id);
            if (!result.Performed)
            {
                StatusText = result.RejectedReason;
                StatusColor = GuiColor.Red;
                RefreshCraft();
                return;
            }

            StatusText = $"{result.Outcome.Label}: {CircuitActionText.DescribeChanges(result.Outcome)}";
            StatusColor = result.Outcome.Label == CircuitEngine.OverloadLabel ? GuiColor.Red : GuiColor.Green;

            switch (result.Status)
            {
                case MinigameStatus.Succeeded:
                    FinishSuccess();
                    break;
                case MinigameStatus.Failed:
                    FinishFailure();
                    break;
                default:
                    RefreshCraft();
                    break;
            }
        };

        private void FinishSuccess()
        {
            _settling = true;
            var recipe = Craft.GetRecipe(_recipe);
            var success = CraftSessionService.Succeed(Player, _session, _engine.Meters.Quality);
            if (success == null)
            {
                ReturnToSetup("Your craft had already ended.", GuiColor.Red);
                return;
            }

            var name = Cache.GetItemNameByResref(recipe.Resref);
            ReturnToSetup(
                $"Crafted {name} at {success.Outcome.QualityPercent}% quality. +{success.Outcome.Xp} XP.",
                GuiColor.Green);
        }

        private void FinishFailure()
        {
            _settling = true;
            var settlement = CraftSessionService.Fail(Player, CraftSessionEndReason.Broken);
            var text = settlement.Settled
                ? $"The piece broke. {settlement.ItemsLost} of {settlement.ItemsLost + settlement.ItemsKept} items were lost. +{settlement.Xp} XP."
                : "The piece broke.";
            ReturnToSetup(text, GuiColor.Red);
        }

        private void ReturnToSetup(string status, GuiColor color)
        {
            _session = null;
            _engine = null;
            _settling = false;
            ResetSlots(_slotItems.Count);
            ClearCraftView();
            RefreshSetup();
            StatusText = status;
            StatusColor = color;
            ChangePartialView(ContentElement, SetupPartial);
        }

        private void RefreshCraft()
        {
            var setup = _engine.Setup;
            var meters = _engine.Meters;
            var load = _engine.Load;

            AssemblyText = $"Assembly: {meters.Progress.Value} / {meters.Progress.Max}";
            AssemblyFraction = meters.Progress.Fraction;
            CalibrationText = $"Calibration: {meters.Quality.Value} / {meters.Quality.Max}  (quality {meters.QualityPercent}%)";
            CalibrationFraction = meters.Quality.Fraction;
            IntegrityText = $"Integrity: {meters.Integrity.Value} / {meters.Integrity.Max}";
            IntegrityFraction = meters.Integrity.Fraction;
            CpText = $"CP: {meters.Cp.Value} / {meters.Cp.Max}";
            LoadText = $"Load: {load} / {CircuitRules.MaxLoad}  (overload line {setup.OverloadLine})";
            LoadColor = load > setup.SafeLoad ? GuiColor.Red : GuiColor.White;
            LoadFraction = (float)load / CircuitRules.MaxLoad;
            LoadRuleText = CircuitActionText.LoadRule(setup);
            PerkText = CircuitActionText.PerkSummary(_engine, CircuitStyles.Active(Player));

            OpportunityText = CircuitActionText.OpportunityText(_engine);
            OpportunityColor = _engine.NextOpportunity == null && _engine.LaterOpportunity == null ? GuiColor.Grey : GuiColor.Green;
            ThresholdText = BuildThresholdText(meters.QualityPercent);

            var names = new GuiBindingList<string>();
            var texts = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            foreach (var preview in _engine.PreviewAll())
            {
                names.Add(CircuitActionText.ButtonText(preview));
                texts.Add(CircuitActionText.RowText(preview));
                tooltips.Add(CircuitActionText.Tooltip(preview));
                enabled.Add(preview.Enabled);
            }

            ActionNames = names;
            ActionTexts = texts;
            ActionTooltips = tooltips;
            ActionEnabled = enabled;
        }

        private string BuildThresholdText(int qualityPercent)
        {
            var parts = new List<string>();
            for (var slot = 0; slot < _slotItems.Count; slot++)
            {
                if (_slotItems[slot] == OBJECT_INVALID)
                    continue;

                var threshold = CraftOutcomeRules.GetEnhancementThreshold(slot + 1);
                parts.Add($"slot {slot + 1} needs {threshold}% ({(qualityPercent >= threshold ? "met" : "not met")})");
            }

            return parts.Count == 0
                ? $"Quality {qualityPercent}%. No enhancements are slotted."
                : $"Quality {qualityPercent}%. Enhancements: {string.Join(", ", parts)}.";
        }

        private void ClearCraftView()
        {
            AssemblyText = string.Empty;
            AssemblyFraction = 0f;
            CalibrationText = string.Empty;
            CalibrationFraction = 0f;
            IntegrityText = string.Empty;
            IntegrityFraction = 0f;
            CpText = string.Empty;
            LoadText = string.Empty;
            LoadColor = GuiColor.White;
            LoadFraction = 0f;
            LoadRuleText = string.Empty;
            PerkText = string.Empty;
            OpportunityText = string.Empty;
            OpportunityColor = GuiColor.Grey;
            ThresholdText = string.Empty;
            ActionNames = new GuiBindingList<string>();
            ActionTexts = new GuiBindingList<string>();
            ActionTooltips = new GuiBindingList<string>();
            ActionEnabled = new GuiBindingList<bool>();
            StyleHeader = string.Empty;
            StyleLabels = new GuiBindingList<string>();
            StyleButtonTexts = new GuiBindingList<string>();
            StyleButtonEnabled = new GuiBindingList<bool>();
        }
    }
}
