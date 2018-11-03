using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class PlantProcessor : IDataProcessor<Plant>
    {
        public IValidator Validator => new PlantValidator();

        public void Process(IDataContext db, Plant dataObject)
        {
        }
    }
}
