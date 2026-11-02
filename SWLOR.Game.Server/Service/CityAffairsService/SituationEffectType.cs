namespace SWLOR.Game.Server.Service.CityAffairsService
{
    /// <summary>What a <see cref="SituationEffect"/> does when a choice or an ignored outcome applies.</summary>
    public enum SituationEffectType
    {
        Invalid = 0,
        TreasuryChange = 1,
        Modifier = 2,
        FollowUp = 3
    }
}
