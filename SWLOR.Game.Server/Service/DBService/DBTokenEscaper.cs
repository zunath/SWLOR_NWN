namespace SWLOR.Game.Server.Service.DBService
{
    /// <summary>
    /// Escapes the characters that are tokens in a Redis search query. Pure, so the query builders and the index writer
    /// use it without the service container (<c>DB.EscapeTokens</c> forwards here).
    /// </summary>
    public static class DBTokenEscaper
    {
        public static string Escape(string str)
        {
            return str
                .Replace("@", "\\@")
                .Replace("!", "\\!")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("|", "\\|")
                .Replace("-", "\\-")
                .Replace("=", "\\=")
                .Replace(">", "\\>")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"");
        }
    }
}
