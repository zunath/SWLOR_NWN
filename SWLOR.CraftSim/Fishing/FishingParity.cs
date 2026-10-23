using System.Collections.Concurrent;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Fishing
{
    /// <summary>Which simulated player fishes.</summary>
    public enum LinePlayer
    {
        /// <summary>The generic adaptive player (a depth-limited expectimax over the previews): a floor on skilled play.</summary>
        Adaptive = 1,

        /// <summary>A casual player who answers the announced move by a fixed rule.</summary>
        RuleOfThumb = 2
    }

    /// <summary>One case of the parity study: a fish, an angler and a player.</summary>
    public readonly record struct CatchKey(
        int FishLevel,
        FishRarity Rarity,
        int Rank,
        int RodTier,
        LinePlayer Player,
        LineAnglerEffects Effects);

    /// <summary>
    /// Plays the Line against the fish of the game and remembers each case, so the tables of one report share their fights. Every
    /// case takes its dice from its own stream of the seed, so a table repeats exactly whatever order the cases ran in.
    /// </summary>
    public sealed class FishingParity
    {
        private readonly ConcurrentDictionary<CatchKey, LineCatchSample> _samples = new();

        public FishingParity(int runs, int seed)
        {
            if (runs < 1)
                throw new ArgumentOutOfRangeException(nameof(runs), "A case needs at least one fight.");

            Runs = runs;
            Seed = seed;
        }

        /// <summary>Fights played for each case.</summary>
        public int Runs { get; }

        public int Seed { get; }

        /// <summary>The rod tier an angler of this rank has: the best tier they can use.</summary>
        public static int NominalRodTier(int rank) => Math.Min(FishingRodTier.HighestTier, rank / FishingRodTier.RanksPerTier);

        public LineCatchSample Catch(
            int fishLevel,
            FishRarity rarity,
            int rank,
            int rodTier,
            LinePlayer player = LinePlayer.Adaptive,
            LineAnglerEffects effects = null) =>
            Catch(new CatchKey(fishLevel, rarity, rank, rodTier, player, effects ?? LineAnglerEffects.None));

        public LineCatchSample Catch(CatchKey key) => _samples.GetOrAdd(key, Play);

        /// <summary>Plays every case in parallel, so the tables that follow only read.</summary>
        public void Prepare(IEnumerable<CatchKey> keys)
        {
            Parallel.ForEach(keys.Distinct(), key => Catch(key));
        }

        private LineCatchSample Play(CatchKey key)
        {
            var setup = LineSetup.ForFish(key.FishLevel, key.Rarity, key.Rank, key.RodTier, key.Effects);
            Func<int, SWLOR.CraftSim.Play.ICraftPolicy> policy = key.Player == LinePlayer.Adaptive ? LineFights.Adaptive : LineFights.RuleOfThumb;
            return LineFights.Sample(setup, policy, Runs, Seed, Stream(key));
        }

        /// <summary>A number that names the case, the same on every machine: FNV-1a over the case's numbers.</summary>
        internal static int Stream(CatchKey key)
        {
            unchecked
            {
                var effects = key.Effects;
                var hash = 2166136261u;
                foreach (var part in new[]
                         {
                             key.FishLevel, (int)key.Rarity, key.Rank, key.RodTier, (int)key.Player, effects.TensionCapBonus,
                             effects.ReelDistanceBonus, effects.ReelTensionBonus, effects.GiveLineDistanceReduction,
                             effects.SetHookDrainBonus, effects.HoldDrainBonus, effects.ForecastDepth + (effects.ForecastDepth > 1 ? 16 * effects.ForecastTurns : 0),
                             effects.RareFishRungReduction
                         })
                {
                    for (var shift = 0; shift < 32; shift += 8)
                        hash = (hash ^ (uint)((part >> shift) & 0xFF)) * 16777619u;
                }

                return (int)hash;
            }
        }
    }
}
