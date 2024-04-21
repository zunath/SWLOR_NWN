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
                ConfigureConversation();
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

        private void ConfigureConversation()
        {
            _chat = _api.Chat.CreateConversation();
            _chat.AppendSystemMessage("Responses should be tailored to fit a Star Wars: The Old Republic MMORPG video game.");
            _chat.AppendSystemMessage("Jedi, Sith, and the Force should not be involved in the quest outline.");
            _chat.AppendSystemMessage("Quests generated should not involve the NPC giving the quest to players. Players should accept the quest and return to the NPC to complete.");
            _chat.AppendSystemMessage("Player responses should include one option to accept the quest and another to decline the quest.");
            _chat.AppendSystemMessage("Include the outline of the quest as well as the dialogue of the NPC(s) involved.");
            _chat.AppendSystemMessage("The name of the NPC should be included in the outline. Avoid NPC names that are from well known media.");
            _chat.AppendSystemMessage("The mission should include killing one or more enemies.");
            _chat.AppendSystemMessage("Include a code template, the NPC's dialogue, and a brief outline in the response.");
            _chat.AppendSystemMessage("Use the following template to create a code file associated with this quest, in addition to the NPC dialogue and outline:" + 
                                      @"builder.Create(""%%QUEST_TAG%%"", ""%%QUEST_NAME%%"")

                                            .AddState()
                                            .SetStateJournalText(""%%JOURNAL_TEXT%%"")
                                            .AddKillObjective(NPCGroupType.%%GROUP_TYPE%%, %%QUANTITY%%)

                                            .AddXPReward(%%XP_REWARD%%)
                                            .AddGoldReward(%%GOLD_REWARD%%)");
            _chat.AppendSystemMessage("Replace %%QUEST_TAG%% with a unique tag that is alphanumeric, lower case only, has no spaces, no longer than 16 characters and has relevance to the quest name.");
            _chat.AppendSystemMessage("Replace %%QUEST_NAME%% with a creative name for the quest.");
            _chat.AppendSystemMessage("Replace %%JOURNAL_TEXT%% with a short description of the task to be given to the player.");
            _chat.AppendSystemMessage("Replace %%GROUP_TYPE%% with a unique name for the type of enemy to be slain by the player. This should adhere to C# enumeration restrictions.");
            _chat.AppendSystemMessage("Replace %%QUANTITY%% with the numeric value of enemies to slay.");
            _chat.AppendSystemMessage("Replace %%XP_REWARD%% with an XP reward.");
            _chat.AppendSystemMessage("Replace %%GOLD_REWARDS%% with a gold reward.");
        }
        
        private async Task GenerateQuest()
        {
            _chat.AppendUserInput("Generate a quest outline involving the fictional planet Hutlar");

            await foreach (var res in _chat.StreamResponseEnumerableFromChatbotAsync())
            {
                Console.Write(res);
            }
        }
        
    }
}
