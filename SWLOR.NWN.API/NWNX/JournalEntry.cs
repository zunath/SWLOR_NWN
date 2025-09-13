namespace SWLOR.NWN.API.NWNX
{
    public class JournalEntry
    {
        public string Name{ get; set; }
        public string Text { get; set; }
        public string Tag { get; set; }
        public int State { get; set; }
        public int Priority{ get; set; }
        public bool IsQuestCompleted { get; set; }
        public bool IsQuestDisplayed { get; set; }
        public int Updated { get; set; }
        public int CalendarDay{ get; set; }
        public int TimeOfDay{ get; set; }
    }
}
