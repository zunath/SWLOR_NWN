using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The records of a clerk harvest: the mark lives on the crop's <see cref="GardenPlot"/>, and finishing the harvest deletes it. The
    /// steps and their order are <see cref="ClerkRecoveryProcess"/>'s.
    /// </summary>
    internal sealed class CropRecoveryStore : IClerkRecoveryStore
    {
        private readonly string _recordId;
        private readonly string _plotId;

        public CropRecoveryStore(string recordId, string plotId)
        {
            _recordId = recordId;
            _plotId = plotId;
        }

        public ClerkRecoveryMark ReadMark() => DB.Get<GardenPlot>(_recordId)?.Recovery;

        public void WriteMark(ClerkRecoveryMark mark)
        {
            var record = DB.Get<GardenPlot>(_recordId) ?? throw new InvalidOperationException($"Garden plot record '{_recordId}' is gone, so its harvest cannot be marked.");
            record.Recovery = mark;
            DB.Set(record);
        }

        public void WriteOverflowItems(string playerId, string recordPrefix, string resref, int count) =>
            ClerkRecovery.WriteOverflowItems(playerId, recordPrefix, resref, count);

        public void WriteOverflowCrate(string playerId, string recordPrefix, LivestockCrate crate) =>
            ClerkRecovery.WriteOverflowCrate(playerId, recordPrefix, crate);

        public void FinishSource(ClerkRecoveryMark mark)
        {
            DB.Delete<GardenPlot>(_recordId);
            Farming.RefreshName(_plotId);
        }
    }
}
