using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal sealed class PlayerGuideTopic
    {
        public string Name { get; }
        public string Category { get; }
        public string Summary { get; }
        public string RailSummary { get; }
        public IReadOnlyList<ArticleBlock> Blocks { get; }
        public IReadOnlyList<QuestionAnswer> Questions { get; }
        public IReadOnlyList<string> RelatedTopics { get; }

        public PlayerGuideTopic(
            string name,
            string category,
            string summary,
            string railSummary,
            IReadOnlyList<ArticleBlock> blocks,
            IReadOnlyList<QuestionAnswer> questions,
            IReadOnlyList<string> relatedTopics)
        {
            Name = name;
            Category = category;
            Summary = summary;
            RailSummary = railSummary;
            Blocks = blocks;
            Questions = questions;
            RelatedTopics = relatedTopics;
        }
    }
}
