using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class LootTableProcessor : IDataProcessor<LootTable>
    {
        public IValidator Validator => new LootTableValidator();

        public void Process(IDataContext db, LootTable dataObject)
        {
        }
    }
}
