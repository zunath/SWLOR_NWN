namespace SWLOR.Web.Models
{
    public class GameTopic
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public GameTopicCategory GameTopicCategoryID { get; set; }
        public bool IsActive { get; set; }
        public int Sequence { get; set; }

        public GameTopic(int id, string name, string text, GameTopicCategory category, bool isActive, int sequence)
        {
            ID = id;
            Name = name;
            Text = text;
            GameTopicCategoryID = category;
            IsActive = isActive;
            Sequence = sequence;
        }
    }
}
