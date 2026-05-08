using System.Net.Http.Headers;
using Newtonsoft.Json;
using SWLOR.BackgroundServices.BackgroundJobs.Models;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.BackgroundJobs.Handlers
{
    public sealed class GitHubIssueJobHandler : IBackgroundJobHandler
    {
        private const string CodexReviewPrompt = "@codex review this issue and provide more details around what you find";
        private readonly HttpClient _httpClient;
        private readonly BackgroundServiceSettings _settings;

        public GitHubIssueJobHandler(HttpClient httpClient, BackgroundServiceSettings settings)
        {
            _httpClient = httpClient;
            _settings = settings;
        }

        public async Task HandleAsync(string payload, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.GitHubToken))
            {
                throw new InvalidOperationException("SWLOR_BUG_GITHUB_TOKEN is not configured.");
            }

            var job = JsonConvert.DeserializeObject<GitHubIssuePayload>(payload)
                      ?? throw new InvalidOperationException("Unable to deserialize GitHub issue payload.");

            var endpoint = $"https://api.github.com/repos/{job.Repository}/issues";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonHttpContent.Create(new
                {
                    title = job.Title,
                    body = BuildBody(job.Body)
                })
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.GitHubToken);
            request.Headers.UserAgent.ParseAdd("SWLOR-BackgroundServices");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"GitHub issue creation failed: {(int)response.StatusCode} {response.StatusCode}. {responseBody}");
            }
        }

        private string BuildBody(string body)
        {
            return _settings.CodexReviewEnabled
                ? $"{CodexReviewPrompt}\n\n{body}"
                : body;
        }
    }
}
