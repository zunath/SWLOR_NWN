using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class FameRegionProcessor : IDataProcessor<FameRegion>
    {
        public IValidator Validator => new FameRegionValidator();

        public void Process(IDataContext db, FameRegion dataObject)
        {
        }
    }
}
