namespace SWLOR.Game.Server.Service.LanguageService
{
    public interface ITranslator
    {
        string Translate(string message, int englishChance, out string partiallyScrambled);
    }
}
