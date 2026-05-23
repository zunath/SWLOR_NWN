using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service
{
    public static class AbilityCooldownVisual
    {
        public const int MaximumCooldownStage = 5;

        private const int CooldownStageCount = MaximumCooldownStage + 1;
        private const int MaxResourceNameLength = 16;
        private const string FeatIconPrefix = "ife_";
        private const string Feat2DA = "feat";
        private const string FeatIconColumn = "ICON";

        private static readonly Dictionary<RecastGroup, List<string>> _texturesByRecastGroup = new();
        private static readonly Dictionary<RecastGroup, List<AbilityDetail>> _abilitiesByRecastGroup = new();
        private static readonly Dictionary<string, Dictionary<RecastGroup, ActiveRecastVisual>> _activeVisuals = new();
        private static bool _isCached;

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheData()
        {
            CacheAbilityCooldownTextures();
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RestoreCooldownVisuals()
        {
            var player = GetEnteringObject();
            if (!CanShowCooldownVisuals(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer?.RecastTimes == null)
            {
                if (dbPlayer != null)
                {
                    dbPlayer.RecastTimes = new Dictionary<RecastGroup, DateTime>();
                    DB.Set(dbPlayer);
                }

                return;
            }

            var now = DateTime.UtcNow;

            foreach (var (group, endsAt) in dbPlayer.RecastTimes.ToList())
            {
                if (endsAt > now)
                {
                    RestoreRecastDelay(player, group, endsAt, now);
                }
            }
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearCooldownVisualsOnExit()
        {
            ClearAllRecastDelays(GetExitingObject());
        }

        public static string GetCooldownTextureName(string sourceTexture, int stage)
        {
            if (stage < 0 || stage > MaximumCooldownStage)
                return null;

            if (string.IsNullOrWhiteSpace(sourceTexture) ||
                !sourceTexture.StartsWith(FeatIconPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var textureName = $"pr{stage}_{sourceTexture.Substring(FeatIconPrefix.Length)}";
            return textureName.Length <= MaxResourceNameLength
                ? textureName
                : null;
        }

        public static int CalculateCooldownStage(DateTime now, DateTime startedAt, DateTime endsAt)
        {
            if (now >= endsAt)
                return -1;

            var totalSeconds = (endsAt - startedAt).TotalSeconds;
            if (totalSeconds <= 0)
                return -1;

            var elapsedSeconds = Math.Max(0, (now - startedAt).TotalSeconds);
            var stage = (int)Math.Floor(elapsedSeconds / totalSeconds * CooldownStageCount);

            return Math.Clamp(stage, 0, MaximumCooldownStage);
        }

        public static void ApplyRecastDelay(uint player, RecastGroup group, DateTime startedAt, DateTime endsAt)
        {
            if (!CanShowCooldownVisuals(player) ||
                group == RecastGroup.Invalid ||
                endsAt <= DateTime.UtcNow)
            {
                return;
            }

            EnsureCached();

            if (!_texturesByRecastGroup.TryGetValue(group, out var textures) ||
                textures.Count <= 0)
            {
                return;
            }

            var playerId = GetObjectUUID(player);
            if (!_activeVisuals.TryGetValue(playerId, out var playerVisuals))
            {
                playerVisuals = new Dictionary<RecastGroup, ActiveRecastVisual>();
                _activeVisuals[playerId] = playerVisuals;
            }

            if (playerVisuals.TryGetValue(group, out var existing))
            {
                ClearTextureOverrides(existing);
            }

            var state = new ActiveRecastVisual(
                player,
                playerId,
                group,
                startedAt,
                endsAt,
                textures);

            playerVisuals[group] = state;
            UpdateAndSchedule(state);
        }

        public static void RefreshRecastDelay(uint player, RecastGroup group, DateTime endsAt)
        {
            if (!CanShowCooldownVisuals(player) ||
                group == RecastGroup.Invalid)
            {
                return;
            }

            if (endsAt <= DateTime.UtcNow)
            {
                ClearRecastDelay(player, group);
                return;
            }

            var startedAt = DateTime.UtcNow;
            var playerId = GetObjectUUID(player);
            if (_activeVisuals.TryGetValue(playerId, out var playerVisuals) &&
                playerVisuals.TryGetValue(group, out var existing) &&
                existing.StartedAt < endsAt)
            {
                startedAt = existing.StartedAt;
            }
            else
            {
                var remainingSeconds = Math.Max(0.1, (endsAt - DateTime.UtcNow).TotalSeconds);
                var totalSeconds = GetEstimatedTotalDelaySeconds(player, group, remainingSeconds);
                startedAt = endsAt.AddSeconds(-totalSeconds);
            }

            ApplyRecastDelay(player, group, startedAt, endsAt);
        }

        public static void ClearRecastDelay(uint player, RecastGroup group)
        {
            if (!GetIsObjectValid(player))
                return;

            ClearRecastDelay(GetObjectUUID(player), group);
        }

        public static void ClearAllRecastDelays(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var playerId = GetObjectUUID(player);
            if (!_activeVisuals.TryGetValue(playerId, out var playerVisuals))
                return;

            foreach (var state in playerVisuals.Values.ToList())
            {
                ClearTextureOverrides(state);
            }

            _activeVisuals.Remove(playerId);
        }

        private static void CacheAbilityCooldownTextures()
        {
            _texturesByRecastGroup.Clear();
            _abilitiesByRecastGroup.Clear();

            var groupsByTexture = new Dictionary<string, HashSet<RecastGroup>>();

            foreach (var (feat, ability) in Ability.GetAllAbilityDetails())
            {
                if (ability.RecastGroup == RecastGroup.Invalid ||
                    ability.RecastDelay == null)
                {
                    continue;
                }

                var texture = Get2DAString(Feat2DA, FeatIconColumn, (int)feat);
                if (string.IsNullOrWhiteSpace(texture) ||
                    texture == "****" ||
                    GetCooldownTextureName(texture, 0) == null)
                {
                    continue;
                }

                if (!_texturesByRecastGroup.TryGetValue(ability.RecastGroup, out var textures))
                {
                    textures = new List<string>();
                    _texturesByRecastGroup[ability.RecastGroup] = textures;
                }

                if (!textures.Contains(texture))
                    textures.Add(texture);

                if (!_abilitiesByRecastGroup.TryGetValue(ability.RecastGroup, out var abilities))
                {
                    abilities = new List<AbilityDetail>();
                    _abilitiesByRecastGroup[ability.RecastGroup] = abilities;
                }

                abilities.Add(ability);

                if (!groupsByTexture.TryGetValue(texture, out var recastGroups))
                {
                    recastGroups = new HashSet<RecastGroup>();
                    groupsByTexture[texture] = recastGroups;
                }

                recastGroups.Add(ability.RecastGroup);
            }

            foreach (var (texture, recastGroups) in groupsByTexture.Where(x => x.Value.Count > 1))
            {
                foreach (var group in recastGroups)
                {
                    if (_texturesByRecastGroup.TryGetValue(group, out var textures))
                    {
                        textures.Remove(texture);
                    }
                }
            }

            foreach (var group in _texturesByRecastGroup
                         .Where(x => x.Value.Count <= 0)
                         .Select(x => x.Key)
                         .ToList())
            {
                _texturesByRecastGroup.Remove(group);
            }

            _isCached = true;
            Console.WriteLine($"Loaded {_texturesByRecastGroup.Values.Sum(x => x.Count)} ability cooldown texture mappings.");
        }

        private static void EnsureCached()
        {
            if (!_isCached)
                CacheAbilityCooldownTextures();
        }

        private static bool CanShowCooldownVisuals(uint player)
        {
            return GetIsObjectValid(player) &&
                   GetIsPC(player) &&
                   !GetIsDM(player) &&
                   !GetIsDMPossessed(player);
        }

        private static void RestoreRecastDelay(uint player, RecastGroup group, DateTime endsAt, DateTime now)
        {
            var remainingSeconds = Math.Max(0.1, (endsAt - now).TotalSeconds);
            var totalSeconds = GetEstimatedTotalDelaySeconds(player, group, remainingSeconds);
            var startedAt = endsAt.AddSeconds(-totalSeconds);

            ApplyRecastDelay(player, group, startedAt, endsAt);
        }

        private static double GetEstimatedTotalDelaySeconds(uint player, RecastGroup group, double remainingSeconds)
        {
            EnsureCached();

            if (!_abilitiesByRecastGroup.TryGetValue(group, out var abilities))
                return remainingSeconds;

            var delaySeconds = abilities
                .Select(ability => ability.RecastDelay?.Invoke(player) ?? 0f)
                .DefaultIfEmpty(0f)
                .Max();

            return Math.Max(remainingSeconds, delaySeconds);
        }

        private static void UpdateAndSchedule(ActiveRecastVisual state)
        {
            if (!IsActiveState(state))
                return;

            var now = DateTime.UtcNow;
            var stage = CalculateCooldownStage(now, state.StartedAt, state.EndsAt);
            if (stage < 0)
            {
                ClearRecastDelay(state.PlayerId, state.Group, state.Token);
                return;
            }

            if (stage != state.Stage)
            {
                ApplyStage(state, stage);
                state.Stage = stage;
            }

            ScheduleNextUpdate(state, now, stage);
        }

        private static void ScheduleNextUpdate(ActiveRecastVisual state, DateTime now, int stage)
        {
            var nextUpdateAt = stage >= MaximumCooldownStage
                ? state.EndsAt
                : GetStageStartTime(state.StartedAt, state.EndsAt, stage + 1);

            var delaySeconds = Math.Max(0.1, (nextUpdateAt - now).TotalSeconds);
            var playerId = state.PlayerId;
            var group = state.Group;
            var token = state.Token;

            AssignCommand(GetModule(), () =>
            {
                DelayCommand((float)delaySeconds, () =>
                {
                    ProcessScheduledUpdate(playerId, group, token);
                });
            });
        }

        private static DateTime GetStageStartTime(DateTime startedAt, DateTime endsAt, int stage)
        {
            var totalTicks = endsAt.Ticks - startedAt.Ticks;
            var stageTicks = totalTicks * stage / CooldownStageCount;
            return startedAt.AddTicks(stageTicks);
        }

        private static void ProcessScheduledUpdate(string playerId, RecastGroup group, Guid token)
        {
            if (!_activeVisuals.TryGetValue(playerId, out var playerVisuals) ||
                !playerVisuals.TryGetValue(group, out var state) ||
                state.Token != token)
            {
                return;
            }

            if (!CanShowCooldownVisuals(state.Player) ||
                GetObjectUUID(state.Player) != playerId)
            {
                ClearRecastDelay(playerId, group, token, false);
                return;
            }

            UpdateAndSchedule(state);
        }

        private static bool IsActiveState(ActiveRecastVisual state)
        {
            return _activeVisuals.TryGetValue(state.PlayerId, out var playerVisuals) &&
                   playerVisuals.TryGetValue(state.Group, out var current) &&
                   current.Token == state.Token;
        }

        private static void ApplyStage(ActiveRecastVisual state, int stage)
        {
            foreach (var sourceTexture in state.SourceTextures)
            {
                var cooldownTexture = GetCooldownTextureName(sourceTexture, stage);
                if (!string.IsNullOrWhiteSpace(cooldownTexture))
                {
                    SetTextureOverride(sourceTexture, cooldownTexture, state.Player);
                }
            }
        }

        private static void ClearRecastDelay(
            string playerId,
            RecastGroup group,
            Guid? token = null,
            bool clearTextureOverrides = true)
        {
            if (!_activeVisuals.TryGetValue(playerId, out var playerVisuals) ||
                !playerVisuals.TryGetValue(group, out var state) ||
                (token.HasValue && state.Token != token.Value))
            {
                return;
            }

            if (clearTextureOverrides)
                ClearTextureOverrides(state);

            playerVisuals.Remove(group);
            if (playerVisuals.Count <= 0)
            {
                _activeVisuals.Remove(playerId);
            }
        }

        private static void ClearTextureOverrides(ActiveRecastVisual state)
        {
            if (!GetIsObjectValid(state.Player))
                return;

            foreach (var sourceTexture in state.SourceTextures)
            {
                SetTextureOverride(sourceTexture, string.Empty, state.Player);
            }
        }

        private sealed class ActiveRecastVisual
        {
            public uint Player { get; }
            public string PlayerId { get; }
            public RecastGroup Group { get; }
            public DateTime StartedAt { get; }
            public DateTime EndsAt { get; }
            public IReadOnlyList<string> SourceTextures { get; }
            public Guid Token { get; } = Guid.NewGuid();
            public int Stage { get; set; } = -1;

            public ActiveRecastVisual(
                uint player,
                string playerId,
                RecastGroup group,
                DateTime startedAt,
                DateTime endsAt,
                IEnumerable<string> sourceTextures)
            {
                Player = player;
                PlayerId = playerId;
                Group = group;
                StartedAt = startedAt;
                EndsAt = endsAt;
                SourceTextures = sourceTextures.ToArray();
            }
        }
    }
}
