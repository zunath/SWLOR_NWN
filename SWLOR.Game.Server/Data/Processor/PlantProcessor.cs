using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class PlantProcessor : IDataProcessor<Plant>
    {
        public IValidator Validator => new PlantValidator();

        public void Process(IDataService data, Plant dataObject)
        {
        }
    }
}
