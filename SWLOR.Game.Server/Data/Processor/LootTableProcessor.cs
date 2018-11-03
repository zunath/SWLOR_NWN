using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class LootTableProcessor : IDataProcessor<LootTable>
    {
        public IValidator Validator => new LootTableValidator();

        public void Process(IDataService data, LootTable dataObject)
        {
        }
    }
}
