using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class StarshipLayoutDefinition : IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();
        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            LightFreighter();
            LightEscort();

            Condor();
            Consular();
            Falchion();
            Hound();
            Merchant();
            Mule();
            Panther();
            Saber();
            Striker();
            Throne();
            Fighter();
            Corvette();
            Basilisk();

            return _builder.Build();
        }

        private void LightFreighter()
        {
            _builder.Create(PropertyLayoutType.LightFreighter1)
                .PropertyType(PropertyType.Starship)
                .Name("Light Freighter 1")
                .StructureLimit(60)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("starship1_int");
        }

        private void LightEscort()
        {
            _builder.Create(PropertyLayoutType.LightEscort1)
                .PropertyType(PropertyType.Starship)
                .Name("Light Escort 1")
                .StructureLimit(60)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("starship2_int");
        }

        private void Condor()
        {
            _builder.Create(PropertyLayoutType.Condor)
                .PropertyType(PropertyType.Starship)
                .Name("Condor")
                .StructureLimit(60)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_condor_z");
        }

        private void Consular()
        {
            _builder.Create(PropertyLayoutType.Consular)
                .PropertyType(PropertyType.Starship)
                .Name("Consular")
                .StructureLimit(80)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_consul_z");
        }

        private void Falchion()
        {
            _builder.Create(PropertyLayoutType.Falchion)
                .PropertyType(PropertyType.Starship)
                .Name("Falchion")
                .StructureLimit(70)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_falchion_z");
        }

        private void Hound()
        {
            _builder.Create(PropertyLayoutType.Hound)
                .PropertyType(PropertyType.Starship)
                .Name("Hound")
                .StructureLimit(60)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_hound_z");
        }

        private void Merchant()
        {
            _builder.Create(PropertyLayoutType.Merchant)
                .PropertyType(PropertyType.Starship)
                .Name("Merchant")
                .StructureLimit(70)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_merchant_z");
        }

        private void Mule()
        {
            _builder.Create(PropertyLayoutType.Mule)
                .PropertyType(PropertyType.Starship)
                .Name("Mule")
                .StructureLimit(70)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_mule_z");
        }

        private void Panther()
        {
            _builder.Create(PropertyLayoutType.Panther)
                .PropertyType(PropertyType.Starship)
                .Name("Panther")
                .StructureLimit(60)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_panth_z");
        }

        private void Saber()
        {
            _builder.Create(PropertyLayoutType.Saber)
                .PropertyType(PropertyType.Starship)
                .Name("Saber")
                .StructureLimit(70)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_saber_z");
        }

        private void Striker()
        {
            _builder.Create(PropertyLayoutType.Striker)
                .PropertyType(PropertyType.Starship)
                .Name("Striker")
                .StructureLimit(60)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_strike_z");
        }

        private void Throne()
        {
            _builder.Create(PropertyLayoutType.Throne)
                .PropertyType(PropertyType.Starship)
                .Name("Throne")
                .StructureLimit(80)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_throne_z");
        }

        private void Fighter()
        {
            _builder.Create(PropertyLayoutType.Fighter)
                .PropertyType(PropertyType.Starship)
                .Name("Fighter")
                .StructureLimit(6)
                .ItemStorageLimit(5)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_fight_v");
        }

        private void Corvette()
        {
            _builder.Create(PropertyLayoutType.Corvette)
                .PropertyType(PropertyType.Starship)
                .Name("Throne")
                .StructureLimit(100)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_corv_v");
        }

        private void Basilisk()
        {
            _builder.Create(PropertyLayoutType.Basilisk)
                .PropertyType(PropertyType.Starship)
                .Name("Throne")
                .StructureLimit(40)
                .ItemStorageLimit(5)
                .BuildingLimit(0)
                .ResearchDeviceLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("ship_basi_v");
        }
    }
}
