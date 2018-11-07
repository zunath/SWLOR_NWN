using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SpawnProcessor : IDataProcessor<Spawn>
    {
        public IValidator Validator => new SpawnValidator();

        public DatabaseAction Process(IDataService data, Spawn dataObject)
        {
            return null;
        }
    }
}
