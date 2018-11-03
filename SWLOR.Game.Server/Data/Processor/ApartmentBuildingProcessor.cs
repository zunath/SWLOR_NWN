using System.Data.Entity.Migrations;
using System.Linq;
using FluentValidation;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Data.Validator;

namespace SWLOR.Game.Server.Data.Processor
{
    public class ApartmentBuildingProcessor: IDataProcessor<ApartmentBuilding>
    {
        public IValidator Validator => new ApartmentBuildingValidator();
        
        public void Process(IDataContext db, ApartmentBuilding dataObject)
        {
            if(dataObject.ApartmentBuildingID <= 0)
            {
                int id = db.ApartmentBuildings.Count() + 1;
                dataObject.ApartmentBuildingID = id;
            }

            db.ApartmentBuildings.AddOrUpdate(dataObject);

        }
    }
}
