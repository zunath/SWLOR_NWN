using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed class SpaceContribution
    {
        public Dictionary<SkillType, double> Points { get; set; } = new();
        public double EnergyPoints { get; set; }
        public double Weight => Points.Values.Sum() + EnergyPoints;
    }
    public sealed class SpaceContributionLedger
    {
        public double RemainingDamage { get; set; }
        public Dictionary<string, SpaceContribution> Participants { get; set; } = new();
        public double CreditDamage(string playerId, double amount)
        {
            Validate(amount);
            var earned = Math.Min(amount, Math.Max(0, RemainingDamage));
            RemainingDamage -= earned;
            Credit(playerId, SkillType.Gunnery, earned);
            return earned;
        }
        public void Credit(string playerId, SkillType skill, double amount)
        {
            Validate(amount);
            if (string.IsNullOrWhiteSpace(playerId)) throw new ArgumentException("Expected a real participant.", nameof(playerId));
            if (amount <= 0) return;
            var contribution = Get(playerId);
            contribution.Points[skill] = contribution.Points.GetValueOrDefault(skill) + amount;
        }
        public void CreditEnergy(string source, string recipient, double paid, double received)
        {
            Validate(paid); Validate(received);
            if (source == recipient || received <= 0 || !Participants.ContainsKey(recipient)) return;
            Get(source).EnergyPoints += Math.Min(paid, received);
        }
        public IReadOnlyDictionary<string, int> Allocate(int pool, SkillType? skill = null, bool energy = false)
        {
            if (pool < 0) throw new ArgumentOutOfRangeException(nameof(pool));
            var weights = Participants.ToDictionary(x => x.Key, x => skill.HasValue ? energy ? x.Value.EnergyPoints : x.Value.Points.GetValueOrDefault(skill.Value) : x.Value.Weight);
            var total = weights.Values.Sum();
            if (total <= 0 || pool == 0) return new Dictionary<string, int>();
            var exact = weights.Where(x => x.Value > 0).ToDictionary(x => x.Key, x => pool * x.Value / total);
            var result = exact.ToDictionary(x => x.Key, x => (int)Math.Floor(x.Value));
            var remainder = pool - result.Values.Sum();
            foreach (var row in exact.OrderByDescending(x => x.Value - result[x.Key]).ThenBy(x => x.Key, StringComparer.Ordinal).Take(remainder)) result[row.Key]++;
            return result;
        }
        private SpaceContribution Get(string id)
        {
            if (!Participants.TryGetValue(id, out var contribution)) Participants[id] = contribution = new();
            return contribution;
        }
        private static void Validate(double amount)
        { if (!double.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount)); }
    }
    public sealed class SpaceHostileDamageDebt
    {
        public double Hull { get; set; }
        public double Shield { get; set; }
        public double Recover(ShipResource resource, double actual)
        {
            if (!double.IsFinite(actual) || actual < 0) throw new ArgumentOutOfRangeException(nameof(actual));
            if (resource == ShipResource.Capacitor) return 0;
            var earned = Math.Min(actual, resource == ShipResource.Hull ? Hull : Shield);
            if (resource == ShipResource.Hull) Hull -= earned; else Shield -= earned;
            return earned;
        }
    }
}
