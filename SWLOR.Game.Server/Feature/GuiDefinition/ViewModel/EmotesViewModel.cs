using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class EmotesViewModel: GuiViewModelBase<EmotesViewModel, GuiPayloadBase>
    {

        public GuiBindingList<string> EmoteNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteDescriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private List<Animation> EmoteAnimations { get; set; }

        public GuiBindingList<bool> IsEmoteLoopingAnimations
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedEmoteIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var emoteNames = ChatCommand.EmoteNames;
            var emoteDescriptions = ChatCommand.EmoteDescriptions;
            var emoteAnimations = ChatCommand.EmoteAnimations;
            var isEmoteLoopingAnimations = ChatCommand.EmoteIsLooping;

            SelectedEmoteIndex = -1;

            EmoteNames = emoteNames;
            EmoteDescriptions = emoteDescriptions;
            EmoteAnimations = emoteAnimations;
            IsEmoteLoopingAnimations = isEmoteLoopingAnimations;
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
    }
}
