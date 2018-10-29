using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;

namespace SWLOR.Game.Server.Threading
{
    public class DiscordBackgroundThread: IBackgroundThread
    {
        private readonly HttpClient _httpClient;
        private bool _isExiting;
        private readonly IErrorService _error;

        public DiscordBackgroundThread(
            HttpClient httpClient,
            IErrorService error)
        {
            _httpClient = httpClient;
            _isExiting = false;
            _error = error;
        }

        public void Start()
        {
            while (!_isExiting)
            {
                Run();
            }

            Console.WriteLine("Discord thread shut down successfully!");
        }

        public void Exit()
        {
            _isExiting = true;
        }

        private void Run()
        {
            App.Resolve<IDataContext>(db =>
            {
                List<DiscordChatQueue> discordQueue = db
                    .DiscordChatQueues
                    .Where(x => x.DatePosted == null || x.DatePosted == null && x.DateForRetry != null && DateTime.UtcNow > x.DateForRetry && x.RetryAttempts < 10)
                    .OrderBy(o => o.DateForRetry)
                    .ThenBy(o => o.DateSent)
                    .ToList();

                foreach (var queue in discordQueue)
                {
                    PostAsync(queue.DiscordChatQueueID, queue.SenderName, queue.Message, queue.SenderAccountName);
                    queue.DatePosted = DateTime.UtcNow;
                }

                db.SaveChanges();
            });

            Thread.Sleep(6000);
        }


        private async void PostAsync(int queueID, string characterName, string message, string accountName)
        {
            string url = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL");
            if (string.IsNullOrWhiteSpace(url)) return;

            dynamic data = new ExpandoObject();
            data.content = message;
            data.username = characterName + " (" + accountName + ")";
            data.file = message;

            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            try
            {
                var response = await _httpClient.PostAsync(url, content);

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                    case HttpStatusCode.NoContent:
                        return;
                }

                if (response.Content != null)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.Length <= 0) return;

                    dynamic responseData = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    Console.WriteLine(responseContent);

                    App.Resolve<IDataContext>(db =>
                    {
                        var record = db.DiscordChatQueues.Single(x => x.DiscordChatQueueID == queueID);
                        record.DatePosted = null;

                        if (responseContent.Length > 0)
                        {
                            record.ResponseContent = responseContent;
                        }

                        record.RetryAttempts++;

                        int retryMS = 60000; // 1 minute
                        if (responseData != null && responseData.retry_after != null)
                        {
                            retryMS = responseData.retry_after;
                        }

                        record.DateForRetry = DateTime.UtcNow.AddMilliseconds(retryMS);
                        db.SaveChanges();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("********ERROR ON DISCORD BACKGROUND THREAD****");
                _error.LogError(ex, "DiscordBackgroundThread");
            }
            
        }
    }
}
