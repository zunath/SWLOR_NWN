using System.Collections.Generic;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Feature.PropertyLayoutDefinition
{
    public class ApartmentLayoutDefinition: IPropertyLayoutListDefinition
    {
        private readonly PropertyLayoutBuilder _builder = new();

        public Dictionary<PropertyLayoutType, PropertyLayout> Build()
        {
            SmallApartments();
            MediumApartments();
            LargeApartments();

            return _builder.Build();
        }

        private void SmallApartments()
        {
            _builder.Create(PropertyLayoutType.ApartmentSmallStyle1)
                .PropertyType(PropertyType.Apartment)
                .Name("Small Apartment - Style 1")
                .StructureLimit(50)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .InitialPrice(5000)
                .PricePerDay(200)
                .AreaInstance("apartment_002");

            _builder.Create(PropertyLayoutType.ApartmentSmallStyle2)
                .PropertyType(PropertyType.Apartment)
                .Name("Small Apartment - Style 2")
                .StructureLimit(50)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .InitialPrice(5000)
                .PricePerDay(200)
                .AreaInstance("playerap_s_unf");

            _builder.Create(PropertyLayoutType.ApartmentSmallStyle2Furnished)
                .PropertyType(PropertyType.Apartment)
                .Name("Small Apartment - Style 2 (Furnished)")
                .StructureLimit(30)
                .ItemStorageLimit(25)
                .BuildingLimit(0)
                .InitialPrice(7000)
                .PricePerDay(300)
                .AreaInstance("playerap_s_fur");
        }

        private void MediumApartments()
        {
            _builder.Create(PropertyLayoutType.ApartmentMediumStyle1)
                .PropertyType(PropertyType.Apartment)
                .Name("Medium Apartment - Style 1")
                .StructureLimit(70)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(10000)
                .PricePerDay(300)
                .AreaInstance("apartment_2");

            _builder.Create(PropertyLayoutType.ApartmentMediumStyle2)
                .PropertyType(PropertyType.Apartment)
                .Name("Medium Apartment - Style 2")
                .StructureLimit(70)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(10000)
                .PricePerDay(300)
                .AreaInstance("playerap_m_unf");

            _builder.Create(PropertyLayoutType.ApartmentMediumStyle2Furnished)
                .PropertyType(PropertyType.Apartment)
                .Name("Medium Apartment - Style 2 (Furnished)")
                .StructureLimit(50)
                .ItemStorageLimit(40)
                .BuildingLimit(0)
                .InitialPrice(13000)
                .PricePerDay(400)
                .AreaInstance("playerap_m_fur");
        }

        private void LargeApartments()
        {
            _builder.Create(PropertyLayoutType.ApartmentLargeStyle1)
                .PropertyType(PropertyType.Apartment)
                .Name("Large Apartment - Style 1")
                .StructureLimit(90)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(25000)
                .PricePerDay(800)
                .AreaInstance("apartment_3");

            _builder.Create(PropertyLayoutType.ApartmentLargeStyle2)
                .PropertyType(PropertyType.Apartment)
                .Name("Large Apartment - Style 2")
                .StructureLimit(90)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(25000)
                .PricePerDay(800)
                .AreaInstance("playerap_l_unf");

            _builder.Create(PropertyLayoutType.ApartmentLargeStyle2Furnished)
                .PropertyType(PropertyType.Apartment)
                .Name("Large Apartment - Style 2 (Furnished)")
                .StructureLimit(70)
                .ItemStorageLimit(80)
                .BuildingLimit(0)
                .InitialPrice(30000)
                .PricePerDay(900)
                .AreaInstance("playerap_l_fur");
        }

    }
}
