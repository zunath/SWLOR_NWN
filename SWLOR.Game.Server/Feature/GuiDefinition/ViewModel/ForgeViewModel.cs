using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The Plasma Forge window. It binds <see cref="ForgeEngine"/> and forwards clicks to it; every rule, cost and result the
    /// window shows comes from the engine's own previews. Starting, finishing and settling a craft go through
    /// <see cref="CraftSessionService"/>.
    /// </summary>
    public class ForgeViewModel : CraftMinigameViewModel<ForgeViewModel, CraftPayload>
    {
        public const string ContentElement = "forge_content";
        public const string SetupPartial = "FORGE_SETUP";
        public const string CraftPartial = "FORGE_CRAFT";
        private const string BlankTexture = "Blank";
        private const int MaxEnhancementSlots = 8;

        private static readonly GuiColor ColdColor = new(0, 191, 255);
        private static readonly GuiColor InRangeColor = new(50, 205, 50);
        private static readonly GuiColor HotColor = new(255, 99, 71);
        private static readonly GuiColor OpportunityColor = new(255, 215, 0);

        private RecipeType _recipe;
        private RecipeDetail _recipeDetail;
        private uint _blueprintItem;
        private bool _hasBlueprint;
        private int _slotCount;
        private readonly List<uint> _enhancementItems = new();
        private int _enhancementShapePercent;
        private ForgeEngine _engine;
        private CraftSession _session;

        // One entry per row of the style list: None first, then each style the crafter owns.
        private readonly List<ForgeStyle> _styleRows = new();

        protected override CraftMinigameType Minigame => CraftMinigameType.Forge;

        public string RecipeName { get => Get<string>(); set => Set(value); }
        public string RecipeLevelText { get => Get<string>(); set => Set(value); }
        public string YourSkillText { get => Get<string>(); set => Set(value); }
        public string FailureRuleText { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiColor StatusColor { get => Get<GuiColor>(); set => Set(value); }

        public string CraftButtonText { get => Get<string>(); set => Set(value); }
        public bool IsCraftEnabled { get => Get<bool>(); set => Set(value); }
        public string EnhancementSummaryText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> SlotIcons { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> SlotLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> RecipeDescription { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<GuiColor> RecipeColors { get => Get<GuiBindingList<GuiColor>>(); set => Set(value); }
        public string StyleHeader { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> StyleLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> StyleButtonTexts { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> StyleButtonEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }

        public string PerkText { get => Get<string>(); set => Set(value); }
        public string OpportunityText { get => Get<string>(); set => Set(value); }
        public GuiColor OpportunityTextColor { get => Get<GuiColor>(); set => Set(value); }
        public string ThermalText { get => Get<string>(); set => Set(value); }
        public float ThermalValue { get => Get<float>(); set => Set(value); }
        public GuiColor ThermalColor { get => Get<GuiColor>(); set => Set(value); }
        public string ShapeText { get => Get<string>(); set => Set(value); }
        public float ShapeValue { get => Get<float>(); set => Set(value); }
        public string FinishText { get => Get<string>(); set => Set(value); }
        public float FinishValue { get => Get<float>(); set => Set(value); }
        public string ThresholdsText { get => Get<string>(); set => Set(value); }
        public string IntegrityText { get => Get<string>(); set => Set(value); }
        public float IntegrityValue { get => Get<float>(); set => Set(value); }
        public string CpText { get => Get<string>(); set => Set(value); }
        public float CpValue { get => Get<float>(); set => Set(value); }
        public string LastResultText { get => Get<string>(); set => Set(value); }

        public GuiBindingList<string> ActionNames { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> ActionEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        public GuiBindingList<string> ActionTooltips { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> ActionPreviews { get => Get<GuiBindingList<string>>(); set => Set(value); }

        /// <summary>The actions the engine has resolved, which hold the XP of a craft the window ends to the modeled pace.</summary>
        protected override int? CraftActionsTaken => _engine?.Turn;

        protected override void Initialize(CraftPayload initialPayload)
        {
            _recipe = initialPayload.Recipe;
            _blueprintItem = initialPayload.BlueprintItem;
            _recipeDetail = Craft.GetRecipe(_recipe);
            var blueprint = Craft.GetBlueprintDetails(_blueprintItem);
            _hasBlueprint = blueprint.Recipe != RecipeType.Invalid;
            _slotCount = Math.Min(MaxEnhancementSlots, _recipeDetail.EnhancementSlots + blueprint.EnhancementSlots);
            _enhancementItems.Clear();
            _enhancementItems.AddRange(Enumerable.Repeat(OBJECT_INVALID, _slotCount));
            _enhancementShapePercent = 0;
            _engine = null;
            _session = null;

            // The record holds the selected style; a death, a login or a trip to space may have cost the crafter its effect.
            CraftingStyleSelection.Restore(Player);
            ForgeStyles.RemoveUnowned(Player);

            var itemName = Craft.GetRecipeName(_recipeDetail);
            RecipeName = $"Recipe: {_recipeDetail.Quantity}x {itemName}";
            RecipeLevelText = $"Recipe level: {_recipeDetail.Level}";
            FailureRuleText =
                $"If the piece breaks (Integrity reaches 0), or you stop before it is finished, each component stack and each enhancement " +
                $"is lost on a {CraftOutcomeRules.ChanceToLoseItemPercent}% chance. A failed craft still gives " +
                $"{(int)(CraftOutcomeRules.FailureXpFraction * 100)}% of the base XP. Closing this window or walking away counts as stopping.";
            StatusText = string.Empty;
            StatusColor = GuiColor.Green;
            CraftButtonText = "Craft";
            IsCraftEnabled = false;
            EnhancementSummaryText = string.Empty;
            SlotIcons = new GuiBindingList<string>();
            SlotLabels = new GuiBindingList<string>();
            RecipeDescription = new GuiBindingList<string>();
            RecipeColors = new GuiBindingList<GuiColor>();
            StyleHeader = string.Empty;
            StyleLabels = new GuiBindingList<string>();
            StyleButtonTexts = new GuiBindingList<string>();
            StyleButtonEnabled = new GuiBindingList<bool>();
            PerkText = string.Empty;
            OpportunityText = string.Empty;
            OpportunityTextColor = GuiColor.Grey;
            ThermalText = string.Empty;
            ThermalColor = InRangeColor;
            ShapeText = string.Empty;
            FinishText = string.Empty;
            ThresholdsText = string.Empty;
            IntegrityText = string.Empty;
            CpText = string.Empty;
            LastResultText = string.Empty;
            ActionNames = new GuiBindingList<string>();
            ActionEnabled = new GuiBindingList<bool>();
            ActionTooltips = new GuiBindingList<string>();
            ActionPreviews = new GuiBindingList<string>();

            var (description, colors) = Craft.BuildRecipeDetail(Player, _recipe, blueprint);
            RecipeDescription = description;
            RecipeColors = colors;

            if (_recipeDetail.MinigameParameters is not ForgeParameters)
            {
                StatusText = "This recipe has no Plasma Forge settings.";
                StatusColor = GuiColor.Red;
            }
            else
            {
                IsCraftEnabled = true;
                CraftButtonText = _hasBlueprint
                    ? $"Craft [{Craft.CalculateBlueprintCraftCreditCost(_blueprintItem):N0}cr]"
                    : "Craft";
            }

            RefreshSkillText();
            RefreshSlots();
            RefreshStyles();
            ChangePartialView(ContentElement, SetupPartial);
            StartMinigameSession();
        }

        protected override void OnModalClosedRestore() =>
            ChangePartialView(ContentElement, _engine == null ? SetupPartial : CraftPartial);

        public Action OnClickEnhancementSlot() => () =>
        {
            if (_engine != null)
                return;

            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _slotCount)
                return;

            if (_enhancementItems[index] != OBJECT_INVALID)
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    _enhancementItems[index] = OBJECT_INVALID;
                    RefreshSlots();
                });
                return;
            }

            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Please click on an enhancement within your inventory.", item =>
            {
                if (_engine != null || !Gui.IsWindowOpen(Player, GuiWindowType.Forge) || index >= _enhancementItems.Count)
                    return;

                if (!IsValidEnhancement(item))
                    return;

                if (_enhancementItems.Contains(item))
                {
                    FloatingTextStringOnCreature("That enhancement is already in a slot.", Player, false);
                    return;
                }

                _enhancementItems[index] = item;
                RefreshSlots();
            });
        };

        public Action OnClickStyle() => () =>
        {
            if (_engine != null)
                return;

            var row = NuiGetEventArrayIndex();
            if (row < 0 || row >= _styleRows.Count)
                return;

            ForgeStyles.Select(Player, _styleRows[row]);
            RefreshStyles();
        };

        public Action OnClickCraft() => () =>
        {
            if (_engine != null || !IsCraftEnabled)
                return;

            var parameters = _recipeDetail.MinigameParameters as ForgeParameters;
            if (parameters == null)
                return;

            // Everything the forge needs, the active style and the perks' stats included, is read before the materials are committed.
            ForgeStyles.RemoveUnowned(Player);
            var setup = BuildSetup(parameters);
            var request = new CraftSessionStartRequest(
                _recipe,
                _hasBlueprint ? _blueprintItem : OBJECT_INVALID,
                _enhancementItems.ToList());
            var started = CraftSessionService.Start(Player, request);
            if (!started.Succeeded)
            {
                StatusText = started.Message;
                StatusColor = GuiColor.Red;
                return;
            }

            _session = started.Session;
            _engine = new ForgeEngine(setup, ServiceMinigameRandom.Instance);
            for (var index = 0; index < _enhancementItems.Count; index++)
                _enhancementItems[index] = OBJECT_INVALID;

            StatusText = string.Empty;
            LastResultText = "The forge is lit. Choose an action.";
            RefreshCraft();
            ChangePartialView(ContentElement, CraftPartial);
        };

        public Action OnClickAction() => () =>
        {
            if (_engine == null || _engine.Status != MinigameStatus.InProgress)
                return;

            if (!CraftSessionService.IsActive(Player, _session))
            {
                EndCraft("The craft ended.", GuiColor.Red);
                return;
            }

            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _engine.Actions.Count)
                return;

            var result = _engine.Perform(_engine.Actions[index].Id);
            if (!result.Performed)
            {
                LastResultText = result.RejectedReason;
                RefreshCraft();
                return;
            }

            // The saved craft counts its actions, so a craft settled with no window (a logout, a death, a restart) is held to the same pace.
            if (result.Status == MinigameStatus.InProgress)
                CraftSessionService.RecordActions(Player, _session, _engine.Turn);

            var summary = ForgePreviewText.Summarize(result.Outcome, _engine.Setup);
            LastResultText = $"{result.Outcome.Label}: {summary}";
            RefreshCraft();

            if (result.Status == MinigameStatus.Succeeded)
                FinishCraft();
            else if (result.Status == MinigameStatus.Failed)
                BreakCraft();
        };

        public Action OnClickStop() => () =>
        {
            if (_engine == null)
                return;

            ShowModal(
                $"Stop crafting? Each component stack and enhancement is lost on a {CraftOutcomeRules.ChanceToLoseItemPercent}% chance.",
                () =>
                {
                    if (_engine == null)
                        return;

                    CraftSessionService.Fail(Player, CraftSessionEndReason.WindowClosed, _engine.Turn);
                    EndCraft("You stopped before the piece was finished.", GuiColor.Red);
                },
                confirmText: "Stop",
                cancelText: "Keep crafting");
        };

        private void FinishCraft()
        {
            var success = CraftSessionService.Succeed(Player, _session, _engine.Meters.Quality, _engine.Turn);
            if (success == null)
            {
                EndCraft("The craft ended.", GuiColor.Red);
                return;
            }

            var outcome = success.Outcome;
            var text = $"Crafted {Craft.GetRecipeName(_recipeDetail)} at {outcome.QualityPercent}% quality. {outcome.Xp} XP.";
            EndCraft(text, GuiColor.Green);
        }

        private void BreakCraft()
        {
            var settlement = CraftSessionService.Fail(Player, CraftSessionEndReason.Broken, _engine.Turn);
            var lost = settlement.Settled ? $" {settlement.ItemsLost} of {settlement.ItemsLost + settlement.ItemsKept} items were lost." : string.Empty;
            EndCraft($"The piece broke.{lost}", GuiColor.Red);
        }

        /// <summary>Returns to the setup page with the result of the craft that just ended.</summary>
        private void EndCraft(string text, GuiColor color)
        {
            _engine = null;
            _session = null;
            StatusText = text;
            StatusColor = color;
            _enhancementShapePercent = 0;
            PerkText = string.Empty;
            RefreshSkillText();
            RefreshSlots();
            RefreshStyles();
            ChangePartialView(ContentElement, SetupPartial);
        }

        private ForgeSetup BuildSetup(ForgeParameters parameters)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var skill = _recipeDetail.Skill;
            var rank = dbPlayer.Skills.TryGetValue(skill, out var playerSkill) ? playerSkill.Rank : 0;
            var gearCp = dbPlayer.CPBonus.TryGetValue(skill, out var cp) ? cp : 0;

            return new ForgeSetup(
                parameters,
                _recipeDetail.Level,
                rank,
                Stat.CalculateCraftsmanship(Player, skill),
                Stat.CalculateControl(Player, skill),
                gearCp,
                _enhancementShapePercent,
                ForgePerkStats.FromStats(stat => Stat.GetStatAdjustment(Player, stat)));
        }

        /// <summary>Lists the styles the crafter owns with what each does, and which one is active. Smithery is not split, so they may own both and toggle here before the craft starts.</summary>
        private void RefreshStyles()
        {
            _styleRows.Clear();
            var owned = ForgeStyles.Owned(Player);
            if (owned.Count > 0)
            {
                _styleRows.Add(ForgeStyle.None);
                _styleRows.AddRange(owned);
            }

            var active = ForgeStyles.Active(Player);
            var labels = new GuiBindingList<string>();
            var buttons = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            foreach (var style in _styleRows)
            {
                labels.Add(style == ForgeStyle.None
                    ? "None: the Plasma Forge plays by its usual rules."
                    : $"{ForgeStyles.Name(style)}: {ForgeStyles.Description(style)}");
                buttons.Add(style == active ? "Selected" : "Select");
                enabled.Add(style != active);
            }

            StyleHeader = owned.Count == 0
                ? $"Styles: learn a style perk at Smithery {ForgePerkRules.StyleRank} to choose one here."
                : "Style: pick one to craft with. Only one can be active at a time.";
            StyleLabels = labels;
            StyleButtonTexts = buttons;
            StyleButtonEnabled = enabled;
        }

        private void RefreshSkillText()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            var rank = dbPlayer.Skills.TryGetValue(_recipeDetail.Skill, out var playerSkill) ? playerSkill.Rank : 0;
            YourSkillText = $"Your skill: {Skill.GetSkillDetails(_recipeDetail.Skill).Name} {rank}";
        }

        /// <summary>Lists the enhancement slots with the Finish each one needs and the Shape the chosen enhancements add.</summary>
        private void RefreshSlots()
        {
            var icons = new GuiBindingList<string>();
            var labels = new GuiBindingList<string>();
            var penalty = 0;

            for (var index = 0; index < _slotCount; index++)
            {
                var threshold = CraftOutcomeRules.GetEnhancementThreshold(index + 1);
                var item = _enhancementItems[index];
                if (item == OBJECT_INVALID || !GetIsObjectValid(item))
                {
                    _enhancementItems[index] = OBJECT_INVALID;
                    icons.Add(BlankTexture);
                    labels.Add($"Slot {index + 1}: empty. Click to choose an enhancement. Needs {threshold}% Finish to transfer.");
                    continue;
                }

                icons.Add(Item.GetIconResref(item));
                labels.Add($"Slot {index + 1}: {GetName(item)}. Needs {threshold}% Finish to transfer. Click to remove.");
                penalty += CraftEnhancementReader.Read(item, _recipeDetail).ProgressPenalty;
            }

            _enhancementShapePercent = ForgeRules.EnhancementShapePercent(penalty, _recipeDetail.Level);
            SlotIcons = icons;
            SlotLabels = labels;
            EnhancementSummaryText = _slotCount == 0
                ? "This recipe has no enhancement slots."
                : _enhancementShapePercent > 0
                    ? $"The chosen enhancements make the piece need {_enhancementShapePercent}% more Shape."
                    : "An enhancement transfers when the Finish you reach meets its slot's threshold.";
        }

        /// <summary>Shows every meter, the announced opportunity and each action's exact preview, all from the engine.</summary>
        private void RefreshCraft()
        {
            var setup = _engine.Setup;
            var meters = _engine.Meters;

            var thermal = _engine.Thermal;
            ThermalValue = thermal / (float)ForgeRules.MaxThermal;
            ThermalColor = _engine.IsQuenched ? ColdColor : _engine.Zone switch
            {
                ForgeThermalZone.Below => ColdColor,
                ForgeThermalZone.Above => HotColor,
                _ => InRangeColor
            };
            ThermalText = $"Thermal {thermal}/{ForgeRules.MaxThermal}   Working range {_engine.RangeLow}-{_engine.RangeHigh}: {DescribeZone()}";

            ShapeText = $"Shape {ForgePreviewText.Percent(meters.Progress)}";
            ShapeValue = meters.Progress.Fraction;
            FinishText = $"Finish {ForgePreviewText.Percent(meters.Quality)}";
            FinishValue = meters.Quality.Fraction;
            IntegrityText = $"Integrity {meters.Integrity.Value}/{meters.Integrity.Max}";
            IntegrityValue = meters.Integrity.Fraction;
            CpText = $"CP {meters.Cp.Value}/{meters.Cp.Max}";
            CpValue = meters.Cp.Fraction;
            ThresholdsText = ForgePreviewText.Thresholds(_slotCount, meters.QualityPercent);
            PerkText = ForgePreviewText.PerkSummary(_engine, ForgeStyles.Active(Player));

            var opportunity = _engine.NextOpportunity;
            OpportunityText = opportunity == null
                ? "No opportunity is announced for the next action."
                : $"{opportunity.Name} next: {opportunity.Description}";
            OpportunityTextColor = opportunity == null ? GuiColor.Grey : OpportunityColor;

            var names = new GuiBindingList<string>();
            var enabled = new GuiBindingList<bool>();
            var tooltips = new GuiBindingList<string>();
            var previews = new GuiBindingList<string>();
            foreach (var preview in _engine.PreviewAll())
            {
                names.Add(ForgePreviewText.ButtonText(preview));
                enabled.Add(preview.Enabled);
                tooltips.Add(ForgePreviewText.Tooltip(preview));
                previews.Add(ForgePreviewText.Describe(preview, setup));
            }

            ActionNames = names;
            ActionEnabled = enabled;
            ActionTooltips = tooltips;
            ActionPreviews = previews;
        }

        private string DescribeZone()
        {
            if (_engine.IsQuenched)
                return "quenched. The piece is set; Vibro-Hone now works at a flat rate.";

            switch (_engine.Zone)
            {
                case ForgeThermalZone.Below:
                    return "too cold. Hydro-Press does little and cracks Integrity.";
                case ForgeThermalZone.Above:
                    return "too hot. Hydro-Press shapes more but cracks Integrity harder.";
                default:
                    var position = ForgeRules.PositionInRange(_engine.Thermal, _engine.RangeLow, _engine.RangeHigh);
                    return position <= 0.5
                        ? "cool half. Best for Vibro-Hone."
                        : "hot half. Hydro-Press hits harder here.";
            }
        }

        private bool IsValidEnhancement(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return false;
            }

            var propertyType = _recipeDetail.EnhancementType switch
            {
                RecipeEnhancementType.Armor => ItemPropertyType.ArmorEnhancement,
                RecipeEnhancementType.Weapon => ItemPropertyType.WeaponEnhancement,
                RecipeEnhancementType.Structure => ItemPropertyType.StructureEnhancement,
                RecipeEnhancementType.Food => ItemPropertyType.FoodEnhancement,
                RecipeEnhancementType.Starship => ItemPropertyType.StarshipEnhancement,
                RecipeEnhancementType.Module => ItemPropertyType.ModuleEnhancement,
                RecipeEnhancementType.Droid => ItemPropertyType.DroidEnhancement,
                _ => ItemPropertyType.Invalid
            };

            var foundProperty = false;
            var enhancementLevel = -1;
            if (propertyType != ItemPropertyType.Invalid)
            {
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    var type = GetItemPropertyType(ip);
                    if (type == propertyType)
                        foundProperty = true;
                    if (type == ItemPropertyType.EnhancementLevel)
                        enhancementLevel = GetItemPropertyCostTableValue(ip);
                }
            }

            if (!foundProperty || enhancementLevel == -1)
            {
                FloatingTextStringOnCreature("Item must be an enhancement.", Player, false);
                return false;
            }

            if (enhancementLevel - _recipeDetail.Level > 5)
            {
                FloatingTextStringOnCreature("Enhancement must be within 5 levels of recipe level.", Player, false);
                return false;
            }

            return true;
        }
    }
}
