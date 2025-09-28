namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for quest journal
        private readonly Dictionary<uint, Dictionary<string, QuestEntry>> _questJournal = new();

        public class QuestEntry
        {
            public string PlotID { get; set; } = "";
            public int State { get; set; } = 0;
            public string Description { get; set; } = "";
            public string Name { get; set; } = "";
            public bool IsCompleted { get; set; } = false;
        }

        public void AddJournalQuestEntry(string szPlotID, int nState, uint oCreature, bool bAllPartyMembers = true, bool bAllowOverrideHigher = false, string sQuestName = "", string sQuestDescription = "") 
        {
            if (!_questJournal.ContainsKey(oCreature))
                _questJournal[oCreature] = new Dictionary<string, QuestEntry>();
            
            _questJournal[oCreature][szPlotID] = new QuestEntry 
            { 
                PlotID = szPlotID, 
                State = nState, 
                Description = sQuestDescription, 
                Name = sQuestName 
            };
        }

        public void RemoveJournalQuestEntry(string szPlotID, uint oCreature, bool bAllPartyMembers = true, bool bAllEntries = false) 
        {
            if (_questJournal.ContainsKey(oCreature))
                _questJournal[oCreature].Remove(szPlotID);
        }

        // Additional quest methods from INWScriptService
        public void AddJournalQuestEntry(string szPlotID, int nState, uint oCreature, bool bAllPartyMembers = true,
            bool bAllPlayers = false, bool bAllowOverrideHigher = false) 
        {
            if (!_questJournal.ContainsKey(oCreature))
                _questJournal[oCreature] = new Dictionary<string, QuestEntry>();
            
            _questJournal[oCreature][szPlotID] = new QuestEntry 
            { 
                PlotID = szPlotID, 
                State = nState 
            };
        }

        // Helper methods for testing
        public Dictionary<uint, Dictionary<string, QuestEntry>> GetQuestJournal() => _questJournal;
        public void ClearQuestJournal() => _questJournal.Clear();
        public QuestEntry GetQuestEntry(uint oCreature, string szPlotID) => 
            _questJournal.GetValueOrDefault(oCreature, new Dictionary<string, QuestEntry>()).GetValueOrDefault(szPlotID, new QuestEntry());
    }
}
