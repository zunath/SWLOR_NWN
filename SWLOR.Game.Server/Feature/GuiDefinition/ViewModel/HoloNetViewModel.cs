using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HoloNetViewModel : GuiViewModelBase<HoloNetViewModel, GuiPayloadBase>
    {
        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();

        public string HoloNetText
        {
            get => Get<string>();
            set => Set(value);
        }

        public const int MaxHoloNetTextLength = 600;
        public const int BroadcastPrice = 2500;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            HoloNetText = string.Empty;
            WatchOnClient(model => model.HoloNetText);
        }

        public Action OnClickSubmit() => () =>
        {
            if (string.IsNullOrWhiteSpace(HoloNetText))
            {
                return;
            }

            var message = HoloNetText;

            if (message.Length > MaxHoloNetTextLength)
            {
                SendMessageToPC(Player, $"Your HoloNet broadcast was too long. Please shorten it to no longer than {MaxHoloNetTextLength} characters and resubmit the broadcast. For reference, your message was: \"" + message + "\"");
                return;
            }

            ShowModal("Are you sure you want to submit this broadcast?", async () =>
            {
                var url = _appSettings.HoloNetWebhookUrl;

                if (string.IsNullOrWhiteSpace(url))
                {
                    SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to send the HoloNet broadcast because server admin has not specified the 'SWLOR_HOLONET_WEBHOOK_URL' environment variable."));
                    return;
                }

                if (GetGold(Player) < BroadcastPrice)
                {
                    SendMessageToPC(Player, ColorToken.Red("Insufficient credits to make this HoloNet broadcast."));
                    return;
                }

                var auditAuthorName = $"{GetName(Player)} ({GetPCPlayerName(Player)}) [{GetPCPublicCDKey(Player)}]";
                AssignCommand(Player, () => TakeGoldFromCreature(BroadcastPrice, Player, true));

                if (!await BackgroundJob.EnqueueDiscordWebhook(url, "HoloNet Broadcast", message, 3447003))
                {
                    AssignCommand(Player, () => GiveGoldToCreature(Player, BroadcastPrice));
                    SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to queue HoloNet broadcast. Please notify a DM."));
                    return;
                }

                Log.Write(LogGroup.Chat, $"{auditAuthorName} submitted HoloNet broadcast: {message}");

                SendMessageToPC(Player, "HoloNet message broadcasted!");
                Gui.TogglePlayerWindow(Player, GuiWindowType.HoloNet);

                for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                {
                    var displayName = PlayerName.GetChatDisplayName(onlinePlayer, Player);
                    SendMessageToPC(onlinePlayer, ColorToken.Custom(displayName + " broadcasts a new HoloNet message: ", 0, 180, 255) + ColorToken.White(message));
                }
            });
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.HoloNet);
        };
    }
}
