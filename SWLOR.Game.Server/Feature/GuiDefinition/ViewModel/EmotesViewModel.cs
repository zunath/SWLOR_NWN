using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class EmotesViewModel: GuiViewModelBase<EmotesViewModel, GuiPayloadBase>
    {
        public GuiBindingList<string> CategoryNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteNamesCol1
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteNamesCol2
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteDescriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteDescriptionsCol1
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteDescriptionsCol2
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string CategoryAllName
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsCategoryAllToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CategoryCombatName
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsCategoryCombatToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CategoryExplorationName
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsCategoryExplorationToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CategoryTasksName
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsCategoryTasksToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CategorySocialName
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsCategorySocialToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CategoryFeelingsName
        {
            get => Get<string>();
            set => Set(value);
        }
        public bool IsCategoryFeelingsToggled
        {
            get => Get<bool>();
            set => Set(value);
        }
        
        public bool IsHorizontalLayoutToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsVerticalLayoutToggled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsHorizontalLayoutVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsVerticalLayoutVisible
        {
            get => Get<bool>();
            set => Set(value);
        }

        private List<Animation> EmoteAnimations { get; set; }
        private List<Animation> EmoteAnimationsCol1 { get; set; }
        private List<Animation> EmoteAnimationsCol2 { get; set; }
        private List<EmoteCategoryType> EmoteCategories { get; set; }

        public GuiBindingList<bool> IsEmoteLoopingAnimations
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> IsEmoteLoopingAnimationsCol1
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<bool> IsEmoteLoopingAnimationsCol2
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedEmoteIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        private List<EmoteCategoryType> _categories;
        public int SelectedCategoryIndex
        {
            get => Get<int>();
            set
            {
                Set(value);
                FilterEmotes();
            }
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            _categories = Enum.GetValues(typeof(EmoteCategoryType))
                .Cast<EmoteCategoryType>()
                .Where(x => x.GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().IsVisible)
                .ToList();

            var categoryNames = new GuiBindingList<string>();
            var categoryToggled = new GuiBindingList<bool>();
            foreach (var category in _categories)
            {
                categoryNames.Add(category.GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name);
                categoryToggled.Add(false);
            }

            CategoryNames = categoryNames;
            CategoryAllName = _categories.Count > 0 ? _categories[0].GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name : "All";
            CategoryCombatName = _categories.Count > 1 ? _categories[1].GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name : "Combat";
            CategoryExplorationName = _categories.Count > 2 ? _categories[2].GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name : "Exploration";
            CategoryTasksName = _categories.Count > 3 ? _categories[3].GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name : "Tasks";
            CategorySocialName = _categories.Count > 4 ? _categories[4].GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name : "Social";
            CategoryFeelingsName = _categories.Count > 5 ? _categories[5].GetAttribute<EmoteCategoryType, EmoteCategoryAttribute>().Name : "Feelings";

            SelectedCategoryIndex = 0;
            IsCategoryAllToggled = true;

            var layout = (EmoteLayoutType)GetLocalInt(Player, "EMOTE_GUI_LAYOUT");
            OnSelectLayout((int)layout)();

            SelectedEmoteIndex = -1;
            FilterEmotes();
        }

        private void FilterEmotes()
        {
            var selectedCategory = _categories[SelectedCategoryIndex];
            var emoteNames = new GuiBindingList<string>();
            var emoteDescriptions = new GuiBindingList<string>();
            var isEmoteLoopingAnimations = new GuiBindingList<bool>();
            var emoteAnimations = new List<Animation>();
            var emoteCategories = new List<EmoteCategoryType>();

            for (var i = 0; i < ChatCommand.EmoteNames.Count; i++)
            {
                var category = ChatCommand.EmoteCategories[i];

                if (selectedCategory == EmoteCategoryType.All || category == selectedCategory)
                {
                    emoteNames.Add(ChatCommand.EmoteNames[i]);
                    emoteDescriptions.Add(ChatCommand.EmoteDescriptions[i]);
                    isEmoteLoopingAnimations.Add(ChatCommand.EmoteIsLooping[i]);
                    emoteAnimations.Add(ChatCommand.EmoteAnimations[i]);
                    emoteCategories.Add(category);
                }
            }

            EmoteNames = emoteNames;
            EmoteDescriptions = emoteDescriptions;
            IsEmoteLoopingAnimations = isEmoteLoopingAnimations;
            EmoteAnimations = emoteAnimations;
            EmoteCategories = emoteCategories;

            // Dividi in due colonne per il layout verticale
            var emoteNamesCol1 = new GuiBindingList<string>();
            var emoteDescriptionsCol1 = new GuiBindingList<string>();
            var isEmoteLoopingCol1 = new GuiBindingList<bool>();
            var emoteAnimationsCol1 = new List<Animation>();

            var emoteNamesCol2 = new GuiBindingList<string>();
            var emoteDescriptionsCol2 = new GuiBindingList<string>();
            var isEmoteLoopingCol2 = new GuiBindingList<bool>();
            var emoteAnimationsCol2 = new List<Animation>();

            for (var i = 0; i < EmoteNames.Count; i++)
            {
                if (i % 2 == 0)
                {
                    emoteNamesCol1.Add(EmoteNames[i]);
                    emoteDescriptionsCol1.Add(EmoteDescriptions[i]);
                    isEmoteLoopingCol1.Add(IsEmoteLoopingAnimations[i]);
                    emoteAnimationsCol1.Add(EmoteAnimations[i]);
                }
                else
                {
                    emoteNamesCol2.Add(EmoteNames[i]);
                    emoteDescriptionsCol2.Add(EmoteDescriptions[i]);
                    isEmoteLoopingCol2.Add(IsEmoteLoopingAnimations[i]);
                    emoteAnimationsCol2.Add(EmoteAnimations[i]);
                }
            }

            EmoteNamesCol1 = emoteNamesCol1;
            EmoteDescriptionsCol1 = emoteDescriptionsCol1;
            IsEmoteLoopingAnimationsCol1 = isEmoteLoopingCol1;
            EmoteAnimationsCol1 = emoteAnimationsCol1;

            EmoteNamesCol2 = emoteNamesCol2;
            EmoteDescriptionsCol2 = emoteDescriptionsCol2;
            IsEmoteLoopingAnimationsCol2 = isEmoteLoopingCol2;
            EmoteAnimationsCol2 = emoteAnimationsCol2;
        }

        public Action OnSelectEmote() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedEmoteIndex = index;
            
            AssignCommand(Player, () => ClearAllActions());
            if (IsEmoteLoopingAnimations[SelectedEmoteIndex])
            {
                var duration = 9999.9f;
                AssignCommand(Player, () => ActionPlayAnimation((Animation) EmoteAnimations[SelectedEmoteIndex], 1f, duration));
            }
            else
            {
                AssignCommand(Player, () => ActionPlayAnimation((Animation) EmoteAnimations[SelectedEmoteIndex]));
            }
        };

        public Action OnSelectEmoteCol1() => () =>
        {
            var index = NuiGetEventArrayIndex();
            AssignCommand(Player, () => ClearAllActions());
            if (IsEmoteLoopingAnimationsCol1[index])
            {
                var duration = 9999.9f;
                AssignCommand(Player, () => ActionPlayAnimation((Animation)EmoteAnimationsCol1[index], 1f, duration));
            }
            else
            {
                AssignCommand(Player, () => ActionPlayAnimation((Animation)EmoteAnimationsCol1[index]));
            }
        };

        public Action OnSelectEmoteCol2() => () =>
        {
            var index = NuiGetEventArrayIndex();
            AssignCommand(Player, () => ClearAllActions());
            if (IsEmoteLoopingAnimationsCol2[index])
            {
                var duration = 9999.9f;
                AssignCommand(Player, () => ActionPlayAnimation((Animation)EmoteAnimationsCol2[index], 1f, duration));
            }
            else
            {
                AssignCommand(Player, () => ActionPlayAnimation((Animation)EmoteAnimationsCol2[index]));
            }
        };

        public Action OnSelectCategory(int index) => () =>
        {
            IsCategoryAllToggled = index == 0;
            IsCategoryCombatToggled = index == 1;
            IsCategoryExplorationToggled = index == 2;
            IsCategoryTasksToggled = index == 3;
            IsCategorySocialToggled = index == 4;
            IsCategoryFeelingsToggled = index == 5;

            SelectedCategoryIndex = index;
        };

        public Action OnSelectLayout(int layoutIndex) => () =>
        {
            var layout = (EmoteLayoutType)layoutIndex;
            IsHorizontalLayoutToggled = layout == EmoteLayoutType.Horizontal;
            IsVerticalLayoutToggled = layout == EmoteLayoutType.Vertical;
            IsHorizontalLayoutVisible = IsHorizontalLayoutToggled;
            IsVerticalLayoutVisible = IsVerticalLayoutToggled;

            SetLocalInt(Player, "EMOTE_GUI_LAYOUT", layoutIndex);

            if (layout == EmoteLayoutType.Horizontal)
            {
                ChangePartialView("EMOTE_LAYOUT_GROUP", "GRID_VIEW");
            }
            else
            {
                ChangePartialView("EMOTE_LAYOUT_GROUP", "COL_VIEW");
            }
        };

        public Action OnClickReset() => () =>
        {
            AssignCommand(Player, () =>
            {
                ClearAllActions(true);
                ActionPlayAnimation(Animation.LoopingPause, 1f, 1f);
            });
            SendMessageToPC(Player, "Animazioni resettate.");
        };
    }
}
