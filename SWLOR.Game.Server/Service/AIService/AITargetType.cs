namespace SWLOR.Game.Server.Service.AIService
{
    public enum AITargetType
    {
        Invalid = 0,
        Self = 1,
        CurrentTarget = 2,
        LowestHPAlly = 3,
        AllyWithTreatmentKit1Status = 4,
        AllyWithTreatmentKit2Status = 5,
    }
}
