namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    internal sealed class ArticleBlock
    {
        public string Title { get; }
        public string Body { get; }

        public ArticleBlock(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }
}
