namespace SWLOR.Game.Server.Entity
{
    /// <summary>
    /// What one candidate stands for. Holds only the statement for now; Edicts extend it.
    /// </summary>
    public class ElectionPlatform
    {
        public ElectionPlatform()
        {
            Statement = string.Empty;
        }

        /// <summary>The candidate's statement, at most <c>ElectionRules.StatementMaxLength</c> characters.</summary>
        public string Statement { get; set; }
    }
}
