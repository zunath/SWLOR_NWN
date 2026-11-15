namespace SWLOR.Game.Server.Domain.Foundation
{
    public class QuestContractObjective
    {
        public QuestContractObjective()
        {
            ItemResref = string.Empty;
            ItemName = string.Empty;
            Quantity = 1;
        }

        public string ItemResref { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
