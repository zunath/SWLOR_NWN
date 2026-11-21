using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>What a derelict interior holds when it is instanced (<see cref="DerelictRules.PlanInterior"/>).</summary>
    public sealed class DerelictInteriorPlan
    {
        public string TemplateResref { get; init; }
        public string Name { get; init; }
        public List<string> Enemies { get; init; } = new();
        public List<string> Terminals { get; init; } = new();
        public List<string> Dressing { get; init; } = new();
        public string StrongboxResref { get; init; }
    }
}
