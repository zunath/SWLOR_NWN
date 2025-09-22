using SWLOR.Component.Communication.Contracts;

namespace SWLOR.Component.Communication.Model
{
    public class TranslatorGeneric : ITranslator
    {
        public string Translate(string message)
        {
            return message;
        }
    }
}
