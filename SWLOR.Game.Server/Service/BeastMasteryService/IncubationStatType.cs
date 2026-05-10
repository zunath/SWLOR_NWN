namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    // Corresponds to iprp_incubation.2da
    public enum IncubationStatType
    {
        Invalid = 0,
        MutationChance = 1,
        AttackPurity = 2,
        AccuracyPurity = 3,
        EvasionPurity = 4,
        LearningPurity = 5,
        PhysicalDefensePurity = 6,
        ForceDefensePurity = 7,
        FireResistancePurity = 8,
        PoisonResistancePurity = 9,
        ElectricalResistancePurity = 10,
        IceResistancePurity = 11,
        // IDs 12-14 were legacy Fortitude/Reflex/Will purities and are retired.
        XPPenalty = 15,
        MindResistancePurity = 16,
        MobilityResistancePurity = 17,
        TraumaResistancePurity = 18,
        DisruptionResistancePurity = 19,
    }
}
