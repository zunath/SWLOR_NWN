namespace SWLOR.Game.Server.Legacy.ValueObject
{
    public class StoredItemPropertyDetail
    {
        public int Amount { get; set; }
        public string VariableName { get; set; }

        public StoredItemPropertyDetail(string variableName)
        {
            Amount = 0;
            VariableName = variableName;
        }
    }
}
