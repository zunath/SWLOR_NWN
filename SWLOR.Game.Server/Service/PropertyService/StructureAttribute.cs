namespace SWLOR.Game.Server.Service.PropertyService
{
    public class StructureAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string Resref { get; set; }
        public int ItemStorage { get; set; }
        public PropertyType RestrictedPropertyTypes { get; set; }
        public PropertyLayoutType LayoutType { get; set; }
        public bool CanBeRetrieved { get; set; }
        public StructureCategoryType Category { get; set; }

        public StructureAttribute(
            string name,
            string resref,
            string description,
            bool isActive,
            int itemStorage,
            PropertyType restrictedPropertyTypes =
                PropertyType.Apartment |
                PropertyType.CityHall |
                PropertyType.Starship |
                PropertyType.City |
                PropertyType.Bank |
                PropertyType.MedicalCenter |
                PropertyType.Starport |
                PropertyType.Cantina |
                PropertyType.House |
                PropertyType.Lab,
            PropertyLayoutType layoutType = PropertyLayoutType.Invalid,
            bool canBeRetrieved = true,
            StructureCategoryType category = StructureCategoryType.Structure)
        {
            Name = name;
            Resref = resref;
            Description = description;
            IsActive = isActive;
            ItemStorage = itemStorage;
            RestrictedPropertyTypes = restrictedPropertyTypes;
            LayoutType = layoutType;
            CanBeRetrieved = canBeRetrieved;
            Category = category;
        }
    }
}
