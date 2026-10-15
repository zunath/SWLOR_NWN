using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CombatRecorderService
{
    public sealed class CombatRecordSummary
    {
        public string Label { get; set; }
        public string AreaName { get; set; }
        public string AreaResref { get; set; }
        public DateTime StartedUtc { get; set; }
        public DateTime StoppedUtc { get; set; }
        public int EventCount { get; set; }
        public List<CombatRecordParticipantSummary> Participants { get; } = new();

        public double DurationSeconds => Math.Max(0d, (StoppedUtc - StartedUtc).TotalSeconds);

        public CombatRecordParticipantSummary GetParticipant(int participantId)
        {
            return Participants.Find(participant => participant.Id == participantId);
        }
    }
}
