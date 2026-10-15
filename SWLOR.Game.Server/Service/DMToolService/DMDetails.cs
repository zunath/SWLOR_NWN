namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// What the DM chooser and creator show for a selected object or blueprint.
    /// </summary>
    public class DMDetails
    {
        /// <summary>
        /// A portrait or inventory icon resref, or an empty string when the object has none.
        /// </summary>
        public string IconResref { get; }
        public string Title { get; }
        public string Body { get; }

        public DMDetails(string iconResref, string title, string body)
        {
            IconResref = iconResref ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
        }
    }
}
