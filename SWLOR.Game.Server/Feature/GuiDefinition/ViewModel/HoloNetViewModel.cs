using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
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

            ShowModal("Are you sure you want to submit this broadcast?", () => { _ = SubmitBroadcastAsync(message); });
        };

        private async Task SubmitBroadcastAsync(string message)
        {
            var player = Player;
            try
            {
                var url = _appSettings.HoloNetWebhookUrl;

                if (string.IsNullOrWhiteSpace(url))
                {
                    SendMessageToPC(player, ColorToken.Red("ERROR: Unable to send the HoloNet broadcast because server admin has not specified the 'SWLOR_HOLONET_WEBHOOK_URL' environment variable."));
                    return;
                }

                if (GetGold(player) < BroadcastPrice)
                {
                    SendMessageToPC(player, ColorToken.Red("Insufficient credits to make this HoloNet broadcast."));
                    return;
                }

                var auditAuthorName = $"{PlayerName.GetAuditName(player)} ({GetPCPlayerName(player)}) [{GetPCPublicCDKey(player)}]";
                AssignCommand(player, () => TakeGoldFromCreature(BroadcastPrice, player, true));

                var playerId = GetObjectUUID(player);
                var enqueued = await BackgroundJob.EnqueueDiscordWebhook(url, "HoloNet Broadcast", message, 3447003);
                await NwTask.SwitchToMainThread();

                if (!GetIsObjectValid(player) || !GetIsPC(player) || GetObjectUUID(player) != playerId)
                    return;

                if (!enqueued)
                {
                    AssignCommand(player, () => GiveGoldToCreature(player, BroadcastPrice));
                    SendMessageToPC(player, ColorToken.Red("ERROR: Unable to queue HoloNet broadcast. Please notify a DM."));
                    return;
                }

                Log.Write(LogGroup.Chat, $"{auditAuthorName} submitted HoloNet broadcast: {message}");

                SendMessageToPC(player, "HoloNet message broadcasted!");
                Gui.ClosePlayerWindow(player, GuiWindowType.HoloNet);

                for (var onlinePlayer = GetFirstPC(); GetIsObjectValid(onlinePlayer); onlinePlayer = GetNextPC())
                {
                    var displayName = PlayerName.GetChatDisplayName(onlinePlayer, player);
                    SendMessageToPC(onlinePlayer, ColorToken.Custom(displayName + " broadcasts a new HoloNet message: ", 0, 180, 255) + ColorToken.White(message));
                }
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "HoloNet broadcast submission failed.");
            }
        }

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.HoloNet);
        };
    }
}
