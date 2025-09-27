using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Shared.Domain.Character.Contracts;

public interface IRacialAppearanceService
{
    /// <summary>
    /// Gets the first value from a RacialAppearanceDefinition for the specified racial type, creature part, and gender.
    /// </summary>
    /// <param name="racialType">The racial type to get the appearance for</param>
    /// <param name="creaturePart">The creature part to get the first value for</param>
    /// <param name="gender">The gender to get the appearance for (affects head selection)</param>
    /// <returns>The first integer value for the specified part, or 1 if not found</returns>
    int GetFirstRacialAppearanceValue(RacialType racialType, CreaturePartType creaturePart, GenderType gender);
}