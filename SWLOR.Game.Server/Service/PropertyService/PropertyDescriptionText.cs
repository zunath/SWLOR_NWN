namespace SWLOR.Game.Server.Service.PropertyService
{
    public static class PropertyDescriptionText
    {
        /// <summary>
        /// The message a player gets on entering a property, or an empty string when it has no description. The property's name comes
        /// first so owner-written text can't pass for a server message.
        /// </summary>
        public static string Format(string propertyName, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return string.Empty;

            return string.IsNullOrWhiteSpace(propertyName)
                ? description.Trim()
                : $"{propertyName.Trim()}: {description.Trim()}";
        }
    }
}
