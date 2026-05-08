using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

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

                var authorName = $"{GetName(Player)} ({GetPCPlayerName(Player)}) [{GetPCPublicCDKey(Player)}]";
                if (!await BackgroundJob.EnqueueDiscordWebhook(url, authorName, message, 3447003))
                {
                    SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to queue HoloNet broadcast. Please notify a DM."));
                    return;
                }

                AssignCommand(Player, () => TakeGoldFromCreature(BroadcastPrice, Player, true));

                SendMessageToPC(Player, "HoloNet message broadcasted!");
                Gui.TogglePlayerWindow(Player, GuiWindowType.HoloNet);

                for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                {
                    SendMessageToPC(onlinePlayer, ColorToken.Custom(authorName + " broadcasts a new HoloNet message: ", 0, 180, 255) + ColorToken.White(message));
                }
            });
        };

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.HoloNet);
        };
    }
}
