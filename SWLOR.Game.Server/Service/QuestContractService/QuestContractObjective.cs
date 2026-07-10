namespace SWLOR.Game.Server.Service.QuestContractService
{
    public class QuestContractObjective
    {
        public QuestContractObjective()
        {
            ItemResref = string.Empty;
            ItemName = string.Empty;
        }

        public string ItemResref { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
