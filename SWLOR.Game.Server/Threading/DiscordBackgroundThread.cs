using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Newtonsoft.Json;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.Threading.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Threading
{
    public class DiscordBackgroundThread: IDiscordThread
    {
        private readonly HttpClient _httpClient;
        private readonly IErrorService _error;
        private readonly IDataService _data;

        public DiscordBackgroundThread(
            HttpClient httpClient,
            IErrorService error,
            IDataService data)
        {
            _httpClient = httpClient;
            _error = error;
            _data = data;
        }
        
        public void Run()
        {
            List<DiscordChatQueue> discordQueue = _data.Where<DiscordChatQueue>(x => x.DatePosted == null || 
                                                                                     x.DatePosted == null && 
                                                                                     x.DateForRetry != null && 
                                                                                     DateTime.UtcNow > x.DateForRetry && 
                                                                                     x.RetryAttempts < 10)
                .OrderBy(o => o.DateForRetry)
                .ThenBy(o => o.DateSent)
                .ToList();

            foreach (var queue in discordQueue)
            {
                PostAsync(queue.ID, queue.SenderName, queue.Message, queue.SenderAccountName);
                queue.DatePosted = DateTime.UtcNow;

                // Directly enqueue the update request because we don't want to cache this data.
                _data.DataQueue.Enqueue(new DatabaseAction(queue, DatabaseActionType.Update));
            }

            Thread.Sleep(6000);
        }


        private async void PostAsync(Guid queueID, string characterName, string message, string accountName)
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

                    var record = _data.Get<DiscordChatQueue>(queueID);
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

                    // Again, directly enqueue this DB update so the record doesn't get cached.
                    _data.DataQueue.Enqueue(new DatabaseAction(record, DatabaseActionType.Update));
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
