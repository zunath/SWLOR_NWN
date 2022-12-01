namespace SWLOR.Game.Server.Service.LanguageService
{
    public class TranslatorGeneric : ITranslator
    {
        public string Translate(string message, int englishChance, out string partiallyScrambled)
        {
            partiallyScrambled = "";
            return message;
        }
    }
}
