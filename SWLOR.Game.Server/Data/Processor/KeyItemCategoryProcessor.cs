using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class KeyItemCategoryProcessor : IDataProcessor<KeyItemCategory>
    {
        public IValidator Validator => new KeyItemCategoryValidator();

        public void Process(IDataContext db, KeyItemCategory dataObject)
        {
        }
    }
}
