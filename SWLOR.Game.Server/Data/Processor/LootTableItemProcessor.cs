using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class LootTableItemProcessor : IDataProcessor<LootTableItem>
    {
        public IValidator Validator => new LootTableItemValidator();

        public void Process(IDataContext db, LootTableItem dataObject)
        {
        }
    }
}
