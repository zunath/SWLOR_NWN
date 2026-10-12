using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.RoleplayXPService
{
    /// <summary>
    /// OOC detection data: the word and emoticon list, plus optional message-scoring weights and the score
    /// at or above which a message counts as OOC. Loaded from the external detection file.
    /// </summary>
    public sealed class OOCDetectionModel
    {
        public const double DefaultThreshold = 0.5;

        public static OOCDetectionModel Default { get; } = new(
            RoleplayMessageClassifier.DefaultOOCTerms,
            new Dictionary<string, double>(),
            DefaultThreshold);

        public IReadOnlySet<string> Terms { get; }
        public IReadOnlyDictionary<string, double> Weights { get; }
        public double Threshold { get; }
        public bool HasScoring => Weights.Count > 0;

        public OOCDetectionModel(IReadOnlySet<string> terms, IReadOnlyDictionary<string, double> weights, double threshold)
        {
            Terms = terms;
            Weights = weights;
            Threshold = threshold;
        }
    }
}
