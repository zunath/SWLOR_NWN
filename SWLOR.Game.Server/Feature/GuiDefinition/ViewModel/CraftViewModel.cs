using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class CraftViewModel: GuiViewModelBase<CraftViewModel, CraftPayload>
    {
        public const string ViewName = "CraftView";
        public const string SetUpPartialName = "SetUpPartial";
        public const string CraftPartialName = "CraftPartial";
        private const string BlankTexture = "Blank";

        private RecipeType _recipe;
        
        private PerkType _rapidSynthesisPerk;
        private PerkType _carefulSynthesisPerk;
        
        private PerkType _basicTouchPerk;
        private PerkType _standardTouchPerk;
        private PerkType _preciseTouchPerk;

        private PerkType _mastersMendPerk;
        private PerkType _steadyHandPerk;
        private PerkType _muscleMemoryPerk;

        private PerkType _venerationPerk;
        private PerkType _wasteNotPerk;

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

        public bool IsAutoCraftEnabled
        {
            get => Get<bool>();
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

        public string CraftingPoints
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

        private string _enhancement1;
        private string _enhancement2;
        private int _durability;
        private int _progress;
        private int _quality;
        private int _craftingPoints;

        protected override void Initialize(CraftPayload initialPayload)
        {
            var playerId = GetObjectUUID(Player);
            var dbPlayer = DB.Get<Player>(playerId);

            IsInSetupMode = true;

            _recipe = initialPayload.Recipe;
            var detail = Craft.GetRecipe(_recipe);
            var itemName = Cache.GetItemNameByResref(detail.Resref);

            IsInSetupMode = true;
            IsInCraftMode = false;
            IsClosable = true;
            IsAutoCraftEnabled = dbPlayer.CraftedRecipes.ContainsKey(_recipe);

            IsEnhancement1Visible = detail.EnhancementSlots >= 1;
            IsEnhancement2Visible = detail.EnhancementSlots >= 2;
            Enhancement1Resref = BlankTexture;
            Enhancement2Resref = BlankTexture;
            Enhancement1Tooltip = "Select Enhancement #1";
            Enhancement2Tooltip = "Select Enhancement #2";

            RecipeName = $"Recipe: {detail.Quantity}x {itemName}";
            RecipeLevel = $"Level: {detail.Level}";

            var (recipeDescription, recipeColors) = Craft.BuildRecipeDetail(Player, _recipe);
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

            LoadRequiredPerks();
            RefreshRecipeStats();
        }

        private void LoadRequiredPerks()
        {
            var detail = Craft.GetRecipe(_recipe);
            switch (detail.Skill)
            {
                case SkillType.Smithery:
                    _rapidSynthesisPerk = PerkType.RapidSynthesisSmithery;
                    _carefulSynthesisPerk = PerkType.CarefulSynthesisSmithery;
                    
                    _basicTouchPerk = PerkType.BasicTouchSmithery;
                    _standardTouchPerk = PerkType.StandardTouchSmithery;
                    _preciseTouchPerk = PerkType.PreciseTouchSmithery;

                    _mastersMendPerk = PerkType.MastersMendSmithery;
                    _steadyHandPerk = PerkType.SteadyHandSmithery;
                    _muscleMemoryPerk = PerkType.MuscleMemorySmithery;

                    _venerationPerk = PerkType.VenerationSmithery;
                    _wasteNotPerk = PerkType.WasteNotSmithery;
                    break;
                case SkillType.Fabrication:
                    _rapidSynthesisPerk = PerkType.RapidSynthesisFabrication;
                    _carefulSynthesisPerk = PerkType.CarefulSynthesisFabrication;

                    _basicTouchPerk = PerkType.BasicTouchFabrication;
                    _standardTouchPerk = PerkType.StandardTouchFabrication;
                    _preciseTouchPerk = PerkType.PreciseTouchFabrication;

                    _mastersMendPerk = PerkType.MastersMendFabrication;
                    _steadyHandPerk = PerkType.SteadyHandFabrication;
                    _muscleMemoryPerk = PerkType.MuscleMemoryFabrication;

                    _venerationPerk = PerkType.VenerationFabrication;
                    _wasteNotPerk = PerkType.WasteNotFabrication;
                    break;
            }
        }

        private void RefreshRecipeStats()
        {
            DurabilityPercentage = 0.4f;
            DurabilityText = $"Durability (40/100)";

            ProgressPercentage = 0.33f;
            ProgressText = $"Progress (10/30)";

            QualityPercentage = 0.125f;
            QualityText = $"Quality (20/160)";
        }

        public Action OnCloseWindow() => () =>
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
        };

        private bool IsValidEnhancement(uint item)
        {
            if (GetItemPossessor(item) != Player)
            {
                FloatingTextStringOnCreature("Item must be in your inventory.", Player, false);
                return false;
            }

            return true;
        }

        public Action OnClickEnhancement1() => () =>
        {
            if (string.IsNullOrWhiteSpace(_enhancement1))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, item =>
                {
                    if (!IsValidEnhancement(item))
                        return;

                    _enhancement1 = ObjectPlugin.Serialize(item);
                    Enhancement1Tooltip = GetName(item);
                    Enhancement1Resref = Item.GetIconResref(item);

                    DestroyObject(item);

                    RefreshRecipeStats();
                });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_enhancement1);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement1 = string.Empty;
                    Enhancement1Resref = BlankTexture;
                    Enhancement1Tooltip = "Select Enhancement #1";

                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickEnhancement2() => () =>
        {
            if (string.IsNullOrWhiteSpace(_enhancement2))
            {
                Targeting.EnterTargetingMode(Player, ObjectType.Item, item =>
                {
                    if (!IsValidEnhancement(item))
                        return;

                    _enhancement2 = ObjectPlugin.Serialize(item);
                    Enhancement2Tooltip = GetName(item);
                    Enhancement2Resref = Item.GetIconResref(item);

                    DestroyObject(item);
                    RefreshRecipeStats();
                });
            }
            else
            {
                ShowModal("Will you remove the enhancement?", () =>
                {
                    var item = ObjectPlugin.Deserialize(_enhancement2);
                    ObjectPlugin.AcquireItem(Player, item);
                    _enhancement2 = string.Empty;
                    Enhancement2Resref = BlankTexture;
                    Enhancement2Tooltip = "Select Enhancement #2";
                    RefreshRecipeStats();
                });
            }
        };

        public Action OnClickAutoCraft() => () =>
        {

        };

        public Action OnClickManualCraft() => () =>
        {
            IsInCraftMode = true;
            IsInSetupMode = false;
            IsAutoCraftEnabled = false;

            IsRapidSynthesisEnabled = Perk.GetEffectivePerkLevel(Player, _rapidSynthesisPerk) > 0;
            IsCarefulSynthesisEnabled = Perk.GetEffectivePerkLevel(Player, _carefulSynthesisPerk) > 0;

            IsBasicTouchEnabled = Perk.GetEffectivePerkLevel(Player, _basicTouchPerk) > 0;
            IsStandardTouchEnabled = Perk.GetEffectivePerkLevel(Player, _standardTouchPerk) > 0;
            IsPreciseTouchEnabled = Perk.GetEffectivePerkLevel(Player, _preciseTouchPerk) > 0;

            IsMastersMendEnabled = Perk.GetEffectivePerkLevel(Player, _mastersMendPerk) > 0;
            IsSteadyHandEnabled = Perk.GetEffectivePerkLevel(Player, _steadyHandPerk) > 0;
            IsMuscleMemoryEnabled = Perk.GetEffectivePerkLevel(Player, _muscleMemoryPerk) > 0;

            IsVenerationEnabled = Perk.GetEffectivePerkLevel(Player, _venerationPerk) > 0;
            IsWasteNotEnabled = Perk.GetEffectivePerkLevel(Player, _wasteNotPerk) > 0;
        };

        public Action OnClickBasicSynthesis() => () =>
        {

        };

        public Action OnClickRapidSynthesis() => () =>
        {

        };

        public Action OnClickCarefulSynthesis() => () =>
        {

        };


        public Action OnClickBasicTouch() => () =>
        {

        };

        public Action OnClickStandardTouch() => () =>
        {

        };

        public Action OnClickPreciseTouch() => () =>
        {

        };


        public Action OnClickMastersMend() => () =>
        {

        };

        public Action OnClickSteadyHand() => () =>
        {

        };

        public Action OnClickMuscleMemory() => () =>
        {

        };

        public Action OnClickVeneration() => () =>
        {

        };

        public Action OnClickWasteNot() => () =>
        {

        };
    }
}
