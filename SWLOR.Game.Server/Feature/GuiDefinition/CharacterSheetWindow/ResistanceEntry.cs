namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>One row of the resistances table.</summary>
    public sealed class ResistanceEntry
    {
        public string Name { get; }
        public string Score { get; }
        public string DamageTaken { get; }
        public string StatusDuration { get; }

        public ResistanceEntry(string name, string score, string damageTaken, string statusDuration)
        {
            Name = name;
            Score = score;
            DamageTaken = damageTaken;
            StatusDuration = statusDuration;
        }
    }
}
