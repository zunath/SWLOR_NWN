namespace SWLOR.Game.Server.ValueObject
{
    public class ImmunityDetails
    {
        public int Amount { get; set; }
        public string VariableName { get; set; }

        public ImmunityDetails(string variableName)
        {
            Amount = 0;
            VariableName = variableName;
        }
    }
}
