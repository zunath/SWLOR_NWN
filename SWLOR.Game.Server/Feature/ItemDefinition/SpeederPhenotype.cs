using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// Maps a rider's body type to its riding phenotype and back.
    /// Each riding phenotype falls back to its body type for part models (phenotype.2da DefaultPhenoType)
    /// and has its own body roots and robes in the haks (SWLOR_Haks/tools/GenerateSpeederRiderModels.py).
    /// </summary>
    public static class SpeederPhenotype
    {
        public static bool IsRiding(PhenoType phenotype) =>
            phenotype == PhenoType.SpeederBike || phenotype == PhenoType.SpeederBikeLarge;

        /// <summary>
        /// Retrieves the riding phenotype for a base body type. Generated robe phenotypes must be resolved
        /// to their base body type first. Returns false for body types with no riding models.
        /// </summary>
        public static bool TryGetRiding(PhenoType body, out PhenoType riding)
        {
            switch (body)
            {
                case PhenoType.Normal:
                    riding = PhenoType.SpeederBike;
                    return true;
                case PhenoType.Big:
                    riding = PhenoType.SpeederBikeLarge;
                    return true;
                default:
                    riding = body;
                    return false;
            }
        }

        /// <summary>
        /// Retrieves the body type a rider returns to on dismounting.
        /// </summary>
        public static PhenoType GetDismounted(PhenoType riding) =>
            riding == PhenoType.SpeederBikeLarge ? PhenoType.Big : PhenoType.Normal;
    }
}
