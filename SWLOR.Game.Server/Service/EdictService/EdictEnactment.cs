namespace SWLOR.Game.Server.Service.EdictService
{
    /// <summary>
    /// What <see cref="EdictChanges.Enact"/> did to the in-memory city records.
    /// </summary>
    /// <param name="Refusal">Why it was refused, or none. A refusal changed nothing.</param>
    /// <param name="TakesEffect">False for an Alliance that now waits for the other mayor.</param>
    /// <param name="Charge">The credits the enacting city must now pay from its treasury.</param>
    /// <param name="ActivatedPartner">True when the other city's waiting Alliance took effect with this one, so that record changed too.</param>
    public readonly record struct EdictEnactment(EdictRefusal Refusal, bool TakesEffect, int Charge, bool ActivatedPartner)
    {
        public static EdictEnactment Refused(EdictRefusal refusal) => new(refusal, false, 0, false);
    }
}
