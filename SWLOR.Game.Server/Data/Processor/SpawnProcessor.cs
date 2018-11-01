using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class SpawnProcessor : IDataProcessor<Spawn>
    {
        public IValidator Validator => new SpawnValidator();

        public void Process(IDataContext db, Spawn dataObject)
        {
        }
    }
}
