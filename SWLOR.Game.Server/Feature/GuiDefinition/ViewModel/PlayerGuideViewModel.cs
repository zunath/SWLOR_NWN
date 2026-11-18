using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class PlayerGuideViewModel : GuiViewModelBase<PlayerGuideViewModel, GuiPayloadBase>
    {
        private readonly List<int> _filteredTopicIndexes = new();
        private readonly List<int> _relatedTopicIndexes = new();

        private int SelectedTopicIndex { get; set; }

        public string SearchText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> TopicButtonTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> TopicSelections
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public GuiBindingList<string> TopicTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public string SelectedTopicCategory
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedTopicName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedTopicSummary
        {
            get => Get<string>();
            set => Set(value);
        }

        public string SelectedArticleBody
        {
            get => Get<string>();
            set => Set(value);
        }

        public string QuestionSummaryText
        {
            get => Get<string>();
            set => Set(value);
        }

        public GuiBindingList<string> RelatedTopicTexts
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> RelatedTopicTooltips
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            SearchText = string.Empty;
            SelectedTopicIndex = -1;
            LoadTopics();
            SelectTopicByTopicIndex(0);

            WatchOnClient(model => model.SearchText);
        }

        private void LoadTopics()
        {
            var topicButtonTexts = new GuiBindingList<string>();
            var topicSelections = new GuiBindingList<bool>();
            var topicTooltips = new GuiBindingList<string>();
            var search = SearchText ?? string.Empty;

            _filteredTopicIndexes.Clear();

            for (var index = 0; index < PlayerGuideTopicCatalog.Topics.Count; index++)
            {
                var topic = PlayerGuideTopicCatalog.Topics[index];
                if (!MatchesSearch(topic, search))
                    continue;

                _filteredTopicIndexes.Add(index);
                topicButtonTexts.Add(topic.Name);
                topicSelections.Add(index == SelectedTopicIndex);
                topicTooltips.Add(topic.Summary);
            }

            TopicButtonTexts = topicButtonTexts;
            TopicSelections = topicSelections;
            TopicTooltips = topicTooltips;
        }

        private static bool MatchesSearch(PlayerGuideTopic topic, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            return topic.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   topic.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   topic.Summary.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                   topic.Blocks.Any(block =>
                       block.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                       block.Body.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                   topic.Questions.Any(question =>
                       question.Question.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                       question.Answer.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        private void SelectTopicByTopicIndex(int topicIndex)
        {
            if (topicIndex < 0 || topicIndex >= PlayerGuideTopicCatalog.Topics.Count)
                topicIndex = 0;

            if (!_filteredTopicIndexes.Contains(topicIndex))
            {
                SearchText = string.Empty;
                LoadTopics();
            }

            var oldFilteredIndex = _filteredTopicIndexes.IndexOf(SelectedTopicIndex);
            if (oldFilteredIndex >= 0 && oldFilteredIndex < TopicSelections.Count)
            {
                TopicSelections[oldFilteredIndex] = false;
            }

            SelectedTopicIndex = topicIndex;
            var filteredIndex = _filteredTopicIndexes.IndexOf(topicIndex);
            if (filteredIndex >= 0 && filteredIndex < TopicSelections.Count)
            {
                TopicSelections[filteredIndex] = true;
            }

            var topic = PlayerGuideTopicCatalog.Topics[topicIndex];
            SelectedTopicCategory = topic.Category.ToUpperInvariant();
            SelectedTopicName = topic.Name;
            SelectedTopicSummary = topic.Summary;
            SelectedArticleBody = BuildArticleBody(topic.Blocks);
            LoadQuestions(topic);
            LoadRelatedTopics(topic);
        }

        private void LoadQuestions(PlayerGuideTopic topic)
        {
            var sb = new StringBuilder();

            foreach (var question in topic.Questions)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine($"Q: {question.Question}");
                sb.AppendLine($"A: {question.Answer}");
            }

            QuestionSummaryText = sb.ToString().TrimEnd();
        }

        private void LoadRelatedTopics(PlayerGuideTopic topic)
        {
            var relatedTopicTexts = new GuiBindingList<string>();
            var relatedTopicTooltips = new GuiBindingList<string>();

            _relatedTopicIndexes.Clear();

            foreach (var relatedTopicName in topic.RelatedTopics)
            {
                var topicIndex = FindTopicIndex(relatedTopicName);
                if (topicIndex < 0)
                    continue;

                var relatedTopic = PlayerGuideTopicCatalog.Topics[topicIndex];
                _relatedTopicIndexes.Add(topicIndex);
                relatedTopicTexts.Add(relatedTopic.Name);
                relatedTopicTooltips.Add(relatedTopic.Summary);
            }

            RelatedTopicTexts = relatedTopicTexts;
            RelatedTopicTooltips = relatedTopicTooltips;
        }

        private static int FindTopicIndex(string topicName)
        {
            for (var index = 0; index < PlayerGuideTopicCatalog.Topics.Count; index++)
            {
                if (PlayerGuideTopicCatalog.Topics[index].Name.Equals(topicName, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            return -1;
        }

        private static string BuildArticleBody(IReadOnlyList<ArticleBlock> blocks)
        {
            var sb = new StringBuilder();

            foreach (var block in blocks)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine(block.Title.ToUpperInvariant());
                sb.AppendLine(block.Body);
            }

            return sb.ToString().TrimEnd();
        }

        public Action OnClickSearch() => () =>
        {
            LoadTopics();

            if (_filteredTopicIndexes.Count > 0)
                SelectTopicByTopicIndex(_filteredTopicIndexes[0]);
            else
                ShowNoSearchResults();
        };

        public Action OnClickClearSearch() => () =>
        {
            SearchText = string.Empty;
            LoadTopics();
            SelectTopicByTopicIndex(0);
        };

        public Action OnClickTopic() => () =>
        {
            var filteredIndex = NuiGetEventArrayIndex();
            if (filteredIndex < 0 || filteredIndex >= _filteredTopicIndexes.Count)
                return;

            SelectTopicByTopicIndex(_filteredTopicIndexes[filteredIndex]);
        };

        public Action OnClickRelatedTopic() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _relatedTopicIndexes.Count)
                return;

            SelectTopicByTopicIndex(_relatedTopicIndexes[index]);
        };

        private void ShowNoSearchResults()
        {
            SelectedTopicIndex = -1;
            SelectedTopicCategory = "BROWSE";
            SelectedTopicName = "No Matching Topics";
            SelectedTopicSummary = "No guide topics matched your search. Clear the search to return to the full guide.";
            SelectedArticleBody = "Try searching for skills, perks, techniques, espionage, death, combat, crafting, gathering, research, beasts, droids, quests, contracts, travel, housing, markets, ships, guilds, citizenship, disguises, chat, or windows.";
            QuestionSummaryText = string.Empty;
            RelatedTopicTexts = new GuiBindingList<string>();
            RelatedTopicTooltips = new GuiBindingList<string>();
        }
    }
}
