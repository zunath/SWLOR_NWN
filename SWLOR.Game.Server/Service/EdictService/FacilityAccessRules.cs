using System;

namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// Who may use a city's Bank and Starport, and the 7-day departure pass that lets a character leave one they can no longer use.
    /// The facts (who holds which Edict, who owes what) are looked up by <see cref="EdictEffects"/>; this class only applies the
    /// Plan's order to them and holds no database or engine calls.
    /// </summary>
    public static class FacilityAccessRules
    {
        /// <summary>How long a departure pass lasts.</summary>
        public const int DeparturePassDays = 7;

        public const string EmbargoedMessage = "This city has closed its facilities to citizens of your city.";

        public const string StarportNotWelcomeMessage = "Only citizens may use starport facilities.";

        public const string BankNotWelcomeMessage = "Only citizens may use this terminal.";

        /// <summary>
        /// Applies the access order, first match wins:
        /// 1. a citizen of a city this city has an Embargo against is refused;
        /// 2. this city's citizens are allowed when they owe no taxes, and refused when they do, even under Open Gates;
        /// 3. citizens of an allied city are allowed when they owe no taxes at home;
        /// 4. anyone else is allowed while Open Gates is in effect;
        /// 5. everyone else is refused.
        /// </summary>
        /// <param name="isEmbargoed">This city has an Embargo in effect naming the character's city.</param>
        /// <param name="isCitizenHere">The character is a citizen of this city.</param>
        /// <param name="isAlliedCitizen">The character is a citizen of a city this city has an Alliance in effect with.</param>
        /// <param name="owedTaxes">The taxes the character owes their own city.</param>
        /// <param name="openGatesInEffect">This city has Open Gates in effect.</param>
        public static FacilityAccessResult Decide(
            bool isEmbargoed,
            bool isCitizenHere,
            bool isAlliedCitizen,
            int owedTaxes,
            bool openGatesInEffect)
        {
            if (isEmbargoed)
                return FacilityAccessResult.Embargoed;

            if (isCitizenHere)
                return owedTaxes > 0 ? FacilityAccessResult.OwesTaxes : FacilityAccessResult.Allowed;

            if (isAlliedCitizen && owedTaxes <= 0)
                return FacilityAccessResult.Allowed;

            if (openGatesInEffect)
                return FacilityAccessResult.Allowed;

            return isAlliedCitizen ? FacilityAccessResult.OwesTaxes : FacilityAccessResult.NotWelcome;
        }

        /// <summary>The player-facing reason for a refusal, or an empty string for <see cref="FacilityAccessResult.Allowed"/>.</summary>
        /// <param name="result">The decision.</param>
        /// <param name="owedTaxes">The taxes the character owes.</param>
        /// <param name="isCitizenHere">Whether the taxes are owed to the city whose facility this is.</param>
        /// <param name="notWelcomeMessage">What a character who is not welcome is told, which names the facility.</param>
        public static string DescribeRefusal(FacilityAccessResult result, int owedTaxes, bool isCitizenHere, string notWelcomeMessage)
        {
            switch (result)
            {
                case FacilityAccessResult.Embargoed:
                    return EmbargoedMessage;
                case FacilityAccessResult.OwesTaxes:
                    return isCitizenHere
                        ? $"You owe {owedTaxes} credits in taxes to this city. You cannot use its facilities until these are paid. Use the Citizenship Terminal in City Hall to pay these."
                        : $"You owe {owedTaxes} credits in taxes to your city. You cannot use an allied city's facilities until these are paid.";
                case FacilityAccessResult.NotWelcome:
                    return notWelcomeMessage;
                default:
                    return string.Empty;
            }
        }

        #region Departure pass

        /// <summary>When a pass that began at <paramref name="startedUtc"/> runs out.</summary>
        public static DateTime GetDeparturePassEndUtc(DateTime startedUtc)
        {
            return startedUtc.AddDays(DeparturePassDays);
        }

        /// <summary>Whether a pass that began at <paramref name="startedUtc"/> still admits its holder.</summary>
        public static bool IsDeparturePassValid(DateTime? startedUtc, DateTime nowUtc)
        {
            return startedUtc.HasValue && nowUtc < GetDeparturePassEndUtc(startedUtc.Value);
        }

        /// <summary>
        /// What the daily sweep does with a ship docked at a player Starport, judged by its owner.
        /// </summary>
        /// <param name="ownerCanUseFacility">The owner may use the city's facilities now.</param>
        /// <param name="passStartedUtc">When the owner's pass for this city began, or null for none.</param>
        /// <param name="nowUtc">The time.</param>
        public static DockedShipAction GetDockedShipAction(bool ownerCanUseFacility, DateTime? passStartedUtc, DateTime nowUtc)
        {
            if (ownerCanUseFacility)
                return DockedShipAction.RefreshPass;

            if (!passStartedUtc.HasValue)
                return DockedShipAction.Leave;

            return IsDeparturePassValid(passStartedUtc, nowUtc)
                ? DockedShipAction.Leave
                : DockedShipAction.ReturnToSafeDock;
        }

        #endregion
    }
}
