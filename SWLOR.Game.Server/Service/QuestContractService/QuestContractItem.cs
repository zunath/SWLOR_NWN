namespace SWLOR.Game.Server.Service.QuestContractService
{
    public class QuestContractItem
    {
        public QuestContractItem()
        {
            Data = string.Empty;
            Name = string.Empty;
            Resref = string.Empty;
            IconResref = string.Empty;
        }

        public string Data { get; set; }
        public string Name { get; set; }
        public string Resref { get; set; }
        public int StackSize { get; set; }
        public string IconResref { get; set; }
    }
}
