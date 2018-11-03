using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Data.Processor
{
    public class FameRegionProcessor : IDataProcessor<FameRegion>
    {
        public IValidator Validator => new FameRegionValidator();

        public void Process(IDataService data, FameRegion dataObject)
        {
        }
    }
}
