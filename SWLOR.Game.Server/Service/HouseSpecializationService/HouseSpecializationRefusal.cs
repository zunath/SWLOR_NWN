namespace SWLOR.Game.Server.Service.HouseSpecializationService
{
    /// <summary>Why a house's specialization cannot be changed. <see cref="None"/> means it can.</summary>
    public enum HouseSpecializationRefusal
    {
        None = 0,

        /// <summary>The property is not a house.</summary>
        NotAHouse = 1,

        /// <summary>The house does not stand in a city or station.</summary>
        NotInCity = 2,

        /// <summary>Only the house's keeper changes its specialization: the player who can place structures in it and grant that permission.</summary>
        NotTheKeeper = 3,

        /// <summary>The specialization changed less than 30 days ago.</summary>
        TooSoon = 4,

        /// <summary>A Workshop needs a crafting skill.</summary>
        NeedsCraftingSkill = 5,

        /// <summary>The house already has that specialization.</summary>
        Unchanged = 6,

        /// <summary>The choice is not a Workshop, a Storehouse or a Greenhouse.</summary>
        NotASpecialization = 7
    }
}
