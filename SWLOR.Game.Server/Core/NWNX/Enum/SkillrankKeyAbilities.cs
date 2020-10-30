namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    // The abilities as bits
    public enum SkillrankKeyAbilities
    {
        Strength = 1,
        Dexterity = 2,
        Constitution = 4,
        Intelligence = 8,
        Wisdom = 16,
        Charisma = 32
    }

    public enum SkillrankAbilityCalculationMethod
    {
        CalcMin = 64,
        CalcMax = 128,
        CalcAverage = 256,
        CalcSum = 512
    }
}