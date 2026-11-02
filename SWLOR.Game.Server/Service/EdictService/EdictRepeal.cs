namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// What <see cref="EdictChanges.Repeal"/> did to the in-memory city records.
    /// </summary>
    /// <param name="Refusal">Why it was refused, or none. A refusal changed nothing.</param>
    /// <param name="BrokenPromise">True when the Edict came from the mayor's platform, so the mayor's record changed.</param>
    /// <param name="EndedPartnerAlliance">True when the other city's Alliance ended with this one, so that record changed too.</param>
    public readonly record struct EdictRepeal(EdictRefusal Refusal, bool BrokenPromise, bool EndedPartnerAlliance)
    {
        public static EdictRepeal Refused(EdictRefusal refusal) => new(refusal, false, false);
    }
}
