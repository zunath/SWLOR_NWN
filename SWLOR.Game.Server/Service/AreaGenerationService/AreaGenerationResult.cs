using System;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    public class AreaGenerationResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string InstanceId { get; set; } = string.Empty;
        public uint Area { get; set; } = OBJECT_INVALID;
        public ResolvedLayout Layout { get; set; }
        public int SeedUsed { get; set; }
        public int AttemptsUsed { get; set; }
    }
}
