namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>One run of a chat message: the text and how it is colored and translated.</summary>
    public class ChatComponent
    {
        public string Text { get; set; }
        public bool IsTranslatable { get; set; }
        public bool IsCustomColor { get; set; }
        public bool IsOOC { get; set; }
        public bool IsEmote { get; set; }
    }
}
