using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public sealed class ConversationViewModel : GuiViewModelBase<ConversationViewModel, ConversationPayload>
    {
        private IConversationSession _session;
        private uint _controllerPlayer;
        private bool _hasImplicitCloseChoice;
        private bool _isClosing;

        public string SpeakerName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PortraitResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HasPortrait
        {
            get => Get<bool>();
            set => Set(value);
        }

        public GuiBindingList<string> LineTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> LineColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        public GuiBindingList<string> ChoiceTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<GuiColor> ChoiceColors
        {
            get => Get<GuiBindingList<GuiColor>>();
            set => Set(value);
        }

        protected override void Initialize(ConversationPayload initialPayload)
        {
            if (initialPayload?.Session == null)
                throw new InvalidOperationException("A conversation session is required to open the conversation window.");

            EnsureReadableWindowGeometry();
            _session = initialPayload.Session;
            _controllerPlayer = initialPayload.ControllerPlayer;
            _isClosing = false;

            try
            {
                RefreshConversation();
            }
            catch (Exception ex)
            {
                HandleRuntimeError(ex);
            }
        }

        protected override void OnClientPropertyUpdated(string propertyName)
        {
            if (propertyName == nameof(Geometry))
                EnsureReadableWindowGeometry();
        }

        public Action OnClickChoice() => () =>
        {
            if (_session == null || _session.HasEnded || _isClosing)
                return;

            try
            {
                var index = NuiGetEventArrayIndex();
                if (_hasImplicitCloseChoice && index == _session.VisibleChoices.Count)
                {
                    _session.End(ConversationEndReason.Completed);
                    CloseWindow();
                    return;
                }

                if (index >= 0 && index < _session.VisibleChoices.Count)
                {
                    var selectedChoice = _session.VisibleChoices[index];
                    PlayPresentation(
                        _session.Context.Player,
                        selectedChoice.SoundResref,
                        selectedChoice.Animation,
                        selectedChoice.AnimationLoops);
                }

                var result = _session.SelectChoice(index);
                if (result == ConversationSelectionResult.InvalidChoice)
                    return;

                if (_session.HasEnded)
                {
                    CloseWindow();
                    return;
                }

                RefreshConversation();
            }
            catch (Exception ex)
            {
                HandleRuntimeError(ex);
            }
        };

        public override Action OnWindowClosed() => () =>
        {
            _session?.End(ConversationEndReason.Aborted);
            _session = null;
            _isClosing = false;
        };

        private void RefreshConversation()
        {
            var node = _session.CurrentNode;
            if (node == null)
                throw new InvalidOperationException("The active conversation has no current NPC line.");

            var speaker = ResolveSpeakerObject(node);
            SpeakerName = ResolveSpeakerName(node, speaker);
            PortraitResref = ResolvePortrait(node, speaker);
            HasPortrait = !string.IsNullOrWhiteSpace(PortraitResref);
            PlayPresentation(speaker, node.SoundResref, node.Animation, node.AnimationLoops);

            var lineTexts = new GuiBindingList<string>();
            var lineColors = new GuiBindingList<GuiColor>();
            foreach (var block in _session.CurrentText)
            {
                if (block == null || string.IsNullOrWhiteSpace(block.Text))
                    continue;

                var resolvedText = NormalizeNuiText(_session.ResolveText(block.Text));
                foreach (var segment in SplitDialogueText(resolvedText))
                {
                    lineTexts.Add(segment);
                    lineColors.Add(ToGuiColor(block));
                }
            }

            if (lineTexts.Count == 0)
            {
                lineTexts.Add(string.Empty);
                lineColors.Add(GuiColor.White);
            }

            var choiceTexts = new GuiBindingList<string>();
            var choiceColors = new GuiBindingList<GuiColor>();
            foreach (var choice in _session.VisibleChoices)
            {
                choiceTexts.Add(NormalizeNuiText(_session.ResolveText(choice.Text.Text)));
                choiceColors.Add(ToGuiColor(choice.Text));
            }

            _hasImplicitCloseChoice = choiceTexts.Count == 0;
            if (_hasImplicitCloseChoice)
            {
                choiceTexts.Add("Goodbye.");
                choiceColors.Add(ToGuiColor(new ConversationTextBlock
                {
                    Style = ConversationTextStyle.PlayerReply
                }));
            }

            LineTexts = lineTexts;
            LineColors = lineColors;
            ChoiceTexts = choiceTexts;
            ChoiceColors = choiceColors;
        }

        private void EnsureReadableWindowGeometry()
        {
            var current = Geometry;
            if (current == null || current.Width >= ConversationWindowDefinition.MinimumWindowWidth)
                return;

            Geometry = new GuiRectangle(
                current.X,
                current.Y,
                ConversationWindowDefinition.MinimumWindowWidth,
                current.Height);
        }

        private static IEnumerable<string> SplitDialogueText(string text)
        {
            const int segmentCharacterLimit = 80;
            const int minimumPreferredSegmentLength = segmentCharacterLimit / 2;

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var normalizedText = text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');

            foreach (var line in normalizedText.Split('\n'))
            {
                var remaining = line.Trim();
                while (remaining.Length > segmentCharacterLimit)
                {
                    var splitIndex = remaining.LastIndexOf(' ', segmentCharacterLimit);
                    if (splitIndex < minimumPreferredSegmentLength)
                        splitIndex = segmentCharacterLimit;

                    var segment = remaining[..splitIndex].Trim();
                    if (segment.Length > 0)
                        yield return segment;

                    remaining = remaining[splitIndex..].TrimStart();
                }

                if (remaining.Length > 0)
                    yield return remaining;
            }
        }

        private uint ResolveSpeakerObject(ConversationNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.SpeakerTag))
            {
                var alternateSpeaker = GetNearestObjectByTag(
                    node.SpeakerTag,
                    _session.Context.Owner);
                if (GetIsObjectValid(alternateSpeaker))
                    return alternateSpeaker;
            }

            return _session.Context.Owner;
        }

        private string ResolveSpeakerName(ConversationNode node, uint speaker)
        {
            if (!string.IsNullOrWhiteSpace(node.SpeakerName))
                return NormalizeNuiText(_session.ResolveText(node.SpeakerName));

            if (!GetIsObjectValid(speaker))
                return string.Empty;

            var observer = _session.Context.Player;
            var name = GetIsPC(speaker)
                ? PlayerName.GetDisplayName(observer, speaker)
                : GetName(speaker);

            return NormalizeNuiText(name);
        }

        private string ResolvePortrait(ConversationNode node, uint speaker)
        {
            if (!string.IsNullOrWhiteSpace(node.PortraitResref))
                return _session.ResolveText(node.PortraitResref);

            if (!GetIsObjectValid(speaker) || !SupportsAutomaticPortrait(GetObjectType(speaker)))
                return string.Empty;

            var portrait = GetPortraitResRef(speaker);
            return string.IsNullOrWhiteSpace(portrait) ? string.Empty : portrait + "l";
        }

        private static bool SupportsAutomaticPortrait(ObjectType objectType) =>
            objectType == ObjectType.Creature;

        private static void PlayPresentation(
            uint actor,
            string soundResref,
            uint animation,
            bool animationLoops)
        {
            if (!GetIsObjectValid(actor))
                return;

            if (!string.IsNullOrWhiteSpace(soundResref))
                AssignCommand(actor, () => PlaySound(soundResref));

            // Zero is the DLG format's default animation value. Non-zero values map directly to
            // NWScript's animation constants. A short duration keeps looping dialogue gestures
            // from continuing after the player advances to another speaker.
            if (animation != 0)
            {
                var duration = animationLoops ? 3f : 0f;
                AssignCommand(actor, () => ActionPlayAnimation((Animation)animation, 1f, duration));
            }
        }

        private void HandleRuntimeError(Exception exception)
        {
            var conversationId = _session?.Title ?? "<unknown>";
            Log.Write(LogGroup.Error, $"NUI conversation '{conversationId}' failed while open. {exception}");

            if (GetIsObjectValid(_controllerPlayer) && GetIsPC(_controllerPlayer))
                SendMessageToPC(_controllerPlayer, ColorToken.Red("This conversation encountered an error and was closed."));

            _session?.End(ConversationEndReason.RuntimeError);
            CloseWindow();
        }

        private void CloseWindow()
        {
            if (_isClosing)
                return;

            _isClosing = true;
            if (GetIsObjectValid(_controllerPlayer) && Gui.IsWindowOpen(_controllerPlayer, GuiWindowType.Conversation))
                Gui.TogglePlayerWindow(_controllerPlayer, GuiWindowType.Conversation);
        }

        private static string NormalizeNuiText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return UtilPlugin.StripColors(text)
                .Replace("<StartAction>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("<StartHighlight>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("</Start>", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static GuiColor ToGuiColor(ConversationTextBlock block)
        {
            if (block.Style == ConversationTextStyle.Custom && block.Color != null)
            {
                return new GuiColor(
                    block.Color.Red,
                    block.Color.Green,
                    block.Color.Blue,
                    block.Color.Alpha);
            }

            return block.Style switch
            {
                ConversationTextStyle.Action => new GuiColor(1, 254, 1),
                ConversationTextStyle.Highlight => new GuiColor(80, 140, 255),
                ConversationTextStyle.Check => new GuiColor(254, 80, 80),
                ConversationTextStyle.PlayerReply => new GuiColor(102, 178, 255),
                ConversationTextStyle.Muted => GuiColor.Grey,
                _ => GuiColor.White
            };
        }
    }
}
