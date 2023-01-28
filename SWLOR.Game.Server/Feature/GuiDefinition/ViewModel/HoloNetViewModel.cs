using System;
using System.Threading.Tasks;
using Discord;
using Discord.Webhook;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class HoloNetViewModel : GuiViewModelBase<HoloNetViewModel, GuiPayloadBase>
    {
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

            ShowModal("Are you sure you want to submit this broadcast?", () =>
            {
                var url = Environment.GetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL");

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

                AssignCommand(Player, () => TakeGoldFromCreature(BroadcastPrice, Player, true));

                var authorName = $"{GetName(Player)}";

                Task.Run(async () =>
                {
                    using (var client = new DiscordWebhookClient(url))
                    {
                        var embed = new EmbedBuilder
                        {
                            Description = message,
                            Author = new EmbedAuthorBuilder
                            {
                                Name = authorName
                            },
                            Color = Color.Blue
                        };

                        await client.SendMessageAsync(string.Empty, embeds: new[] { embed.Build() });
                    }
                });

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
