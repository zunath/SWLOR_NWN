using System.Data.Entity.Migrations;
using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class ApartmentBuildingProcessor: IDataProcessor<ApartmentBuilding>
    {
        public IValidator Validator => new ApartmentBuildingValidator();
        
        public void Process(IDataContext db, ApartmentBuilding dataObject)
        {
            db.ApartmentBuildings.AddOrUpdate(dataObject);

        }
    }
}
