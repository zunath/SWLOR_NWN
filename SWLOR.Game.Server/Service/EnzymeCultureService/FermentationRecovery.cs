namespace SWLOR.Game.Server.Service.EnzymeCultureService
{
    /// <summary>What happens to a batch when its vat goes away (picked up, or lost with its house or city).</summary>
    public enum FermentationRecovery
    {
        /// <summary>The batch is still running: its inputs go to the owner's overflow store.</summary>
        ReturnInputs = 0,

        /// <summary>The batch had finished: the enzyme it made goes to the owner's overflow store.</summary>
        DeliverEnzyme = 1,
    }
}
