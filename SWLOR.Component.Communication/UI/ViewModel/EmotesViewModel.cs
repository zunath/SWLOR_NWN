using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.UI.ViewModel
{
    public class EmotesViewModel: GuiViewModelBase<EmotesViewModel, IGuiPayload>
    {
        private readonly IServiceProvider _serviceProvider;

        public EmotesViewModel(IGuiService guiService, IServiceProvider serviceProvider) : base(guiService)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded service to break circular dependency
        private IChatCommandService ChatCommandService => _serviceProvider.GetRequiredService<IChatCommandService>();

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

        private List<AnimationType> EmoteAnimations { get; set; }

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

        protected override void Initialize(IGuiPayload initialPayload)
        {
            var emoteNames = ChatCommandService.EmoteNames;
            var emoteDescriptions = ChatCommandService.EmoteDescriptions;
            var emoteAnimations = ChatCommandService.EmoteAnimations;
            var isEmoteLoopingAnimations = ChatCommandService.EmoteIsLooping;

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
                AssignCommand(Player, () => ActionPlayAnimation((AnimationType) EmoteAnimations[SelectedEmoteIndex], 1f, duration));
            }
            else
            {
                AssignCommand(Player, () => ActionPlayAnimation((AnimationType) EmoteAnimations[SelectedEmoteIndex]));
            }

        };
    }
}
