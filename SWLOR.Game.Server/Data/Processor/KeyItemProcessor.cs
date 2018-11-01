using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class KeyItemProcessor : IDataProcessor<KeyItem>
    {
        public IValidator Validator => new KeyItemValidator();

        public void Process(IDataContext db, KeyItem dataObject)
        {
        }
    }
}
