using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class BugReportViewModel: GuiViewModelBase<BugReportViewModel, GuiPayloadBase>
    {
        public const int MaxBugReportLength = 1000;
        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();
        private static readonly HttpClient _httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(2)
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            client.DefaultRequestHeaders.UserAgent.ParseAdd("SWLOR-BugReporter");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            BugReportText = string.Empty;
            WatchOnClient(model => model.BugReportText);
        }        
        public string BugReportText
        {
            get => Get<string>();
            set
            {
                Set(value);
            }
        }

        public Action OnClickSubmit() => () =>
        {
            if (string.IsNullOrWhiteSpace(BugReportText))
            {
                return;
            }

            var message = BugReportText;

            if (message.Length > 1000)
            {
                SendMessageToPC(Player, "Your message was too long. Please shorten it to no longer than 1000 characters and resubmit the bug. For reference, your message was: \"" + message + "\"");
                return;
            }
            var area = GetArea(Player);
            var position = GetPosition(Player);

            var githubRepository = Environment.GetEnvironmentVariable("SWLOR_BUG_GITHUB_REPOSITORY");
            var githubToken = Environment.GetEnvironmentVariable("SWLOR_BUG_GITHUB_TOKEN");

            if (string.IsNullOrWhiteSpace(githubRepository) || string.IsNullOrWhiteSpace(githubToken))
            {
                SendMessageToPC(Player, ColorToken.Red("ERROR: Unable to send bug report because the server admin has not set SWLOR_BUG_GITHUB_REPOSITORY and SWLOR_BUG_GITHUB_TOKEN environment variables."));
                return;
            }

            var authorName = $"{GetName(Player)} ({GetPCPlayerName(Player)}) [{GetPCPublicCDKey(Player)}]";
            var areaName = GetName(area);
            var areaTag = GetTag(area);
            var areaResref = GetResRef(area);
            var positionGroup = $"({position.X}, {position.Y}, {position.Z})";
            var dateReported = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var playerId = GetObjectUUID(Player);
            var nextReportAllowed = DateTime.UtcNow.AddMinutes(1);
            _ = Task.Run(() => SubmitBugReportToGitHub(
                githubRepository,
                githubToken,
                message,
                authorName,
                areaName,
                areaTag,
                areaResref,
                positionGroup,
                dateReported,
                playerId));

            SetLocalString(Player, "BUG_REPORT_LAST_SUBMISSION", nextReportAllowed.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            SendMessageToPC(Player, "Bug report submitted! Thank you for your report.");
            SendMessageToPC(Player, "Submitted Bug Report: " + BugReportText);
            Gui.TogglePlayerWindow(Player, GuiWindowType.BugReport);
        };


        private async Task SubmitBugReportToGitHub(
            string githubRepository,
            string githubToken,
            string message,
            string authorName,
            string areaName,
            string areaTag,
            string areaResref,
            string positionGroup,
            string dateReported,
            string playerId)
        {
            try
            {
                var title = _appSettings.ServerEnvironment == ServerEnvironmentType.Test
                    ? $"[TEST SERVER] Bug Report: {areaName}"
                    : $"Bug Report: {areaName}";

                var body = $"{message}\n\n---\n@codex please review this issue.\n\n" +
                           $"**Reporter**: {authorName}\n" +
                           $"**Area Name**: {areaName}\n" +
                           $"**Area Tag**: {areaTag}\n" +
                           $"**Area Resref**: {areaResref}\n" +
                           $"**Position**: {positionGroup}\n" +
                           $"**Date Reported (UTC)**: {dateReported}\n" +
                           $"**Player ID**: {playerId}";

                var payload = new
                {
                    title,
                    body
                };

                var endpoint = $"https://api.github.com/repos/{githubRepository}/issues";
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Log.Write(LogGroup.Error, $"Bug report GitHub issue creation failed: {(int)response.StatusCode} {response.StatusCode}. Response: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"Unhandled exception when submitting bug report to GitHub. {ex}");
            }
        }

        public Action OnClickCancel() => () =>
        {
            Gui.TogglePlayerWindow(Player, GuiWindowType.BugReport);
        };
    }
}
