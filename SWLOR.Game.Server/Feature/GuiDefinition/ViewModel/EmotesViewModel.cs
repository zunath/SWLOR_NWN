using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Models;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class EmotesViewModel: GuiViewModelBase<EmotesViewModel, GuiPayloadBase>
    {
        private readonly IChatCommandService _chatCommandService;

        public EmotesViewModel(IGuiService guiService, IChatCommandService chatCommandService) : base(guiService)
        {
            _chatCommandService = chatCommandService;
        }

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
            var emoteNames = _chatCommandService.EmoteNames;
            var emoteDescriptions = _chatCommandService.EmoteDescriptions;
            var emoteAnimations = _chatCommandService.EmoteAnimations;
            var isEmoteLoopingAnimations = _chatCommandService.EmoteIsLooping;

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
