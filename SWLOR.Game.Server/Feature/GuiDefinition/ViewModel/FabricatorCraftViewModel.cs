using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Fabrication crafting window. Before the craft it selects enhancements and shows what the craft needs; during it binds the
    /// <see cref="FabricatorEngine"/>'s meters and previews and forwards each click to it.
    /// </summary>
    public class FabricatorCraftViewModel : CraftMinigameViewModel<FabricatorCraftViewModel, CraftPayload>
    {
        public const string ContentElement = "fabricator_craft_content";
        public const string SetupPartial = "FABRICATOR_SETUP";
        public const string CraftPartial = "FABRICATOR_CRAFT";

        private RecipeType _recipe;
        private uint _blueprintItem;
        private CraftSession _session;
        private FabricatorEngine _engine;
        private bool _settling;

        // One entry per enhancement slot, counting from slot 1; an empty slot holds OBJECT_INVALID.
        private readonly List<uint> _slotItems = new();
        private readonly List<string> _slotNames = new();
        private readonly List<int> _slotPenalties = new();

        protected override CraftMinigameType Minigame => CraftMinigameType.Fabricator;

        public string RecipeName { get => Get<string>(); set => Set(value); }
        public string RecipeLevel { get => Get<string>(); set => Set(value); }
        public string GearSummary { get => Get<string>(); set => Set(value); }
        public string FabricatorSummary { get => Get<string>(); set => Set(value); }
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

        public string HeadText { get => Get<string>(); set => Set(value); }
        public string OpportunityText { get => Get<string>(); set => Set(value); }
        public GuiColor OpportunityColor { get => Get<GuiColor>(); set => Set(value); }
        public string FrameText { get => Get<string>(); set => Set(value); }
        public float FrameProgress { get => Get<float>(); set => Set(value); }
        public float FrameQuality { get => Get<float>(); set => Set(value); }
        public string ShellText { get => Get<string>(); set => Set(value); }
        public float ShellProgress { get => Get<float>(); set => Set(value); }
        public float ShellQuality { get => Get<float>(); set => Set(value); }
        public string FittingsText { get => Get<string>(); set => Set(value); }
        public float FittingsProgress { get => Get<float>(); set => Set(value); }
        public float FittingsQuality { get => Get<float>(); set => Set(value); }
        public string QualityText { get => Get<string>(); set => Set(value); }
        public float QualityFraction { get => Get<float>(); set => Set(value); }
        public string MachineText { get => Get<string>(); set => Set(value); }
        public float MachineFraction { get => Get<float>(); set => Set(value); }
        public string CpText { get => Get<string>(); set => Set(value); }
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

        private FabricatorSetup BuildSetup()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var rank = dbPlayer.Skills.TryGetValue(recipe.Skill, out var skill) ? skill.Rank : 0;
            var gearCp = dbPlayer.CPBonus.TryGetValue(recipe.Skill, out var cp) ? cp : 0;

            return FabricatorSetup.ForRecipe(
                recipe,
                rank,
                Stat.CalculateCraftsmanship(Player, recipe.Skill),
                Stat.CalculateControl(Player, recipe.Skill),
                gearCp,
                _slotPenalties.Sum());
        }

        private void RefreshSetup()
        {
            var recipe = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            var hasBlueprint = blueprint.Recipe != RecipeType.Invalid;
            var setup = BuildSetup();
            var parameters = setup.Parameters;

            RecipeName = $"Recipe: {recipe.Quantity}x {Cache.GetItemNameByResref(recipe.Resref)}";
            RecipeLevel = $"Recipe level {recipe.Level}  |  Your Fabrication rank {setup.Rank}";
            GearSummary =
                $"Craftsmanship {setup.Craftsmanship} (+{setup.Craftsmanship}% Run)  |  Control {setup.Control} (+{setup.Control}% Precision Pass)  |  CP {setup.MaxCp}";
            FabricatorSummary =
                $"Work: Frame {parameters.FrameSizePercent}%, Shell {parameters.ShellSizePercent}%, Fittings {parameters.FittingsSizePercent}%. " +
                $"Quality: Frame {parameters.FrameQualityPercent}%, Shell {parameters.ShellQualityPercent}%, Fittings {parameters.FittingsQualityPercent}%. " +
                $"Wear {parameters.WearRate} Machine Condition per Run or Precision Pass. Starts with the {setup.StartingHead} loaded.";
            NeedsSummary = setup.EnhancementProgressPenalty > 0
                ? $"Needs {setup.MaxProgress} progress in all ({setup.EnhancementProgressPenalty * FabricatorRules.ProgressScale} added by enhancements) and starts with {setup.MaxMachineCondition} Machine Condition. Retool costs {setup.RetoolCp} CP and Service {setup.ServiceCp} CP."
                : $"Needs {setup.MaxProgress} progress in all and starts with {setup.MaxMachineCondition} Machine Condition. Retool costs {setup.RetoolCp} CP and Service {setup.ServiceCp} CP.";
            SlotHeader = _slotItems.Count == 0
                ? "This recipe has no enhancement slots."
                : "Enhancement slots. An enhancement transfers when the quality reaches its slot's threshold.";
            RuleText =
                $"If the piece breaks (Machine Condition reaches 0) or you close this window mid-craft, each component stack and each enhancement is lost on a roll of {CraftOutcomeRules.ChanceToLoseItemPercent} or lower on a D100. " +
                $"You still earn {(int)(CraftOutcomeRules.FailureXpFraction * 100)}% of the XP.";
            CraftText = hasBlueprint
                ? $"Craft [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]"
                : "Craft";

            var (description, colors) = Craft.BuildRecipeDetail(Player, _recipe, blueprint);
            RecipeDescription = description;
            RecipeColors = colors;
            RefreshSlots();
        }

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
                if (_session != null || !Gui.IsWindowOpen(Player, GuiWindowType.CraftFabricator))
                    return;

                var recipe = Craft.GetRecipe(_recipe);
                var problem = FabricatorEnhancementCheck.Problem(Player, item, recipe);
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

            var result = CraftSessionService.Start(Player, new CraftSessionStartRequest(_recipe, _blueprintItem, _slotItems.ToList()));
            if (!result.Succeeded)
            {
                StatusText = result.Message;
                StatusColor = GuiColor.Red;
                return;
            }

            _session = result.Session;
            _settling = false;
            _engine = new FabricatorEngine(ServiceMinigameRandom.Instance, BuildSetup());

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

            StatusText = $"{result.Outcome.Label}: {FabricatorActionText.DescribeChanges(result.Outcome)}";
            StatusColor = GuiColor.Green;

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
            var head = _engine.ActiveHead;

            HeadText = $"Loaded head: {head} (serves the {head.Serves()}).";

            var opportunity = _engine.NextOpportunity;
            OpportunityText = FabricatorActionText.OpportunityText(opportunity);
            OpportunityColor = opportunity == null ? GuiColor.Grey : GuiColor.Green;

            (FrameText, FrameProgress, FrameQuality) = DescribePart(SubAssembly.Frame);
            (ShellText, ShellProgress, ShellQuality) = DescribePart(SubAssembly.Shell);
            (FittingsText, FittingsProgress, FittingsQuality) = DescribePart(SubAssembly.Fittings);

            QualityText = $"Quality: {meters.QualityPercent}%  (no higher than {_engine.QualityCap}% until your weakest sub-assembly improves)";
            QualityFraction = meters.Quality.Fraction;
            MachineText = $"Machine Condition: {meters.Integrity.Value} / {meters.Integrity.Max}  (wear {setup.WearRate} per Run or Precision Pass)";
            MachineFraction = meters.Integrity.Fraction;
            CpText = $"CP: {meters.Cp.Value} / {meters.Cp.Max}";
            ThresholdText = BuildThresholdText(meters.QualityPercent);

            var names = new GuiBindingList<string>();
            var texts = new GuiBindingList<string>();
            var tooltips = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            foreach (var preview in _engine.PreviewAll())
            {
                names.Add(FabricatorActionText.ButtonText(preview));
                texts.Add(FabricatorActionText.ResultText(preview, meters.QualityPercent));
                tooltips.Add(FabricatorActionText.Tooltip(preview, meters.QualityPercent));
                enabled.Add(preview.Enabled);
            }

            ActionNames = names;
            ActionTexts = texts;
            ActionTooltips = tooltips;
            ActionEnabled = enabled;
        }

        private (string Text, float Progress, float Quality) DescribePart(SubAssembly part)
        {
            var state = _engine.Part(part);
            var size = _engine.Setup.SizeOf(part);
            var text = $"{part} ({part.HeadFor()}): {state.Progress}/{size}, quality {state.Quality}%";
            return (text, size <= 0 ? 0f : Math.Min(1f, (float)state.Progress / size), state.Quality / (float)FabricatorRules.MaxQuality);
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
            HeadText = string.Empty;
            OpportunityText = string.Empty;
            OpportunityColor = GuiColor.Grey;
            FrameText = string.Empty;
            FrameProgress = 0f;
            FrameQuality = 0f;
            ShellText = string.Empty;
            ShellProgress = 0f;
            ShellQuality = 0f;
            FittingsText = string.Empty;
            FittingsProgress = 0f;
            FittingsQuality = 0f;
            QualityText = string.Empty;
            QualityFraction = 0f;
            MachineText = string.Empty;
            MachineFraction = 0f;
            CpText = string.Empty;
            ThresholdText = string.Empty;
            ActionNames = new GuiBindingList<string>();
            ActionTexts = new GuiBindingList<string>();
            ActionTooltips = new GuiBindingList<string>();
            ActionEnabled = new GuiBindingList<bool>();
        }
    }
}
