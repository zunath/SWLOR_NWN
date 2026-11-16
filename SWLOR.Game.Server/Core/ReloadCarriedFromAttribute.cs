namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Names where a <see cref="ReloadStateKind.Carried"/> field lived in an earlier build. The old build writes each class's carried
    /// fields under its own class name (<see cref="ReloadStateCarrier.CarryAll"/>), so after a field moves to another class the new
    /// class finds nothing under its own name; <see cref="ReloadStateCarrier.AdoptAll"/> then reads the field from the former class's
    /// text instead. Mark a field whenever it moves to another class or is renamed. <c>ReloadCarryHistoryTests</c> fails until every
    /// field an earlier build carried is put back through this attribute or recorded as dropped in <c>ReloadCarryHistory.txt</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class ReloadCarriedFromAttribute : Attribute
    {
        /// <summary>The full name of the class that carried the field in the earlier build.</summary>
        public string FormerOwner { get; }

        /// <summary>The field's name in the earlier build; null when it kept its name.</summary>
        public string FormerField { get; }

        /// <param name="formerOwner">The full name of the class that carried the field in the earlier build.</param>
        /// <param name="formerField">The field's name in the earlier build; null when it kept its name.</param>
        public ReloadCarriedFromAttribute(string formerOwner, string formerField = null)
        {
            if (string.IsNullOrWhiteSpace(formerOwner))
                throw new ArgumentException("The former owner is the full name of a class.", nameof(formerOwner));

            FormerOwner = formerOwner;
            FormerField = formerField;
        }
    }
}
