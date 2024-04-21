using System;
using System.IO;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Chat;
using SWLOR.Game.Server.Extension;

namespace SWLOR.CLI
{
    internal class QuestBuilder
    {
        private string _apiKey;
        private OpenAIAPI _api;
        private Conversation _chat;
        
        public async Task ProcessAsync()
        {
            try
            {
                LoadAPIKey();
                ConnectToAPI();
                await GenerateQuest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.ToMessageAndCompleteStacktrace()}");
            }
        }

        private void LoadAPIKey()
        {
            const string FilePath = "./InputFiles/OpenAI.key";
            if (File.Exists(FilePath))
            {
                _apiKey = File.ReadAllText(FilePath);
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine($"Please copy/paste your API key and press enter:");
                _apiKey = Console.ReadLine();
            }
        }

        private void ConnectToAPI()
        {
            Console.WriteLine($"Attempting to connect to Open AI API...");
            try
            {
                _api = new OpenAIAPI(_apiKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to Open AI API.");
                Console.WriteLine($"{ex.ToMessageAndCompleteStacktrace()}");
            }
        }

        private async Task GenerateQuest()
        {
            _chat = _api.Chat.CreateConversation();
            
            Console.WriteLine($"Enter prompt: ");
            _chat.AppendUserInput(Console.ReadLine());

            await foreach (var res in _chat.StreamResponseEnumerableFromChatbotAsync())
            {
                Console.Write(res);
            }
        }
        
    }
}
