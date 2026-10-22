using System.Collections.Generic;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>
/// What an experiment has to remember to be settled later: the evidence on each candidate and what Analyze has done. The research
/// job stores it after every action, so closing the window or logging out settles the experiment with the evidence so far.
/// </summary>
public sealed record ExperimentSnapshot(
    IReadOnlyDictionary<ResearchCandidate, int> Evidence,
    int TrialsUsed,
    int SpentEvidence,
    int TimeCutPercent,
    bool Concluded);
