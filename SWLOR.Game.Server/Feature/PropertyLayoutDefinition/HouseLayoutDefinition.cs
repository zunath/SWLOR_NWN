using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class HouseLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();
        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            SmallHouse();
            MediumHouse();
            LargeHouse();

            return _builder.Build();
        }

        private void SmallHouse()
        {
            _builder.Create(PropertyLayoutType.SmallHouseStyle1)
                .PropertyType(PropertyType.House)
                .Name("Small House - Style 1")
                .StructureLimit(80)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_1");

            _builder.Create(PropertyLayoutType.SmallHouseStyle2)
                .PropertyType(PropertyType.House)
                .Name("Small House - Style 2")
                .StructureLimit(80)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_4");

            _builder.Create(PropertyLayoutType.SmallHouseStyle3)
                .PropertyType(PropertyType.House)
                .Name("Small House - Style 3")
                .StructureLimit(80)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_7");

            _builder.Create(PropertyLayoutType.SmallHouseStyle4)
                .PropertyType(PropertyType.House)
                .Name("Small House - Style 4")
                .StructureLimit(80)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_10");
        }

        private void MediumHouse()
        {
            _builder.Create(PropertyLayoutType.MediumHouseStyle1)
                .PropertyType(PropertyType.House)
                .Name("Medium House - Style 1")
                .StructureLimit(90)
                .ItemStorageLimit(60)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_2");

            _builder.Create(PropertyLayoutType.MediumHouseStyle2)
                .PropertyType(PropertyType.House)
                .Name("Medium House - Style 2")
                .StructureLimit(90)
                .ItemStorageLimit(60)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_5");

            _builder.Create(PropertyLayoutType.MediumHouseStyle3)
                .PropertyType(PropertyType.House)
                .Name("Medium House - Style 3")
                .StructureLimit(90)
                .ItemStorageLimit(60)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_8");

            _builder.Create(PropertyLayoutType.MediumHouseStyle4)
                .PropertyType(PropertyType.House)
                .Name("Medium House - Style 4")
                .StructureLimit(90)
                .ItemStorageLimit(60)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_11");
        }

        private void LargeHouse()
        {
            _builder.Create(PropertyLayoutType.LargeHouseStyle1)
                .PropertyType(PropertyType.House)
                .Name("Large House - Style 1")
                .StructureLimit(100)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_3");

            _builder.Create(PropertyLayoutType.LargeHouseStyle2)
                .PropertyType(PropertyType.House)
                .Name("Large House - Style 2")
                .StructureLimit(100)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_6");

            _builder.Create(PropertyLayoutType.LargeHouseStyle3)
                .PropertyType(PropertyType.House)
                .Name("Large House - Style 3")
                .StructureLimit(100)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_9");

            _builder.Create(PropertyLayoutType.LargeHouseStyle4)
                .PropertyType(PropertyType.House)
                .Name("Large House - Style 4")
                .StructureLimit(100)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(0)
                .PricePerDay(0)
                .AreaInstance("house_int_12");
        }
    }
}
