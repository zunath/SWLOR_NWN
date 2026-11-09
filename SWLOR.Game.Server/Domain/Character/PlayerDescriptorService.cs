using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The public descriptors ("Someone" names) of players: the logic that used to be static in
    /// <c>Service.PlayerDescriptor</c>, moved here unchanged.
    /// </summary>
    /// <remarks>
    /// <c>PlayerName</c> and the descriptors call each other. <c>PlayerNameService</c> reaches this service through
    /// the <c>PlayerDescriptor</c> facade (two <c>protected virtual</c> methods), this service takes
    /// <see cref="IPlayerNameService"/>, so neither constructor needs the other.
    /// </remarks>
    public class PlayerDescriptorService : IPlayerDescriptorService
    {
        private const int GenericDescriptorChancePercent = 25;
        private const string Appearance2DA = "appearance";
        private const string AppearanceLabelColumn = "LABEL";
        private const string DynamicAppearanceLabelPrefix = "(Dynamic)";
        private const string HumanoidSpeciesName = "Humanoid";
        public const string DefaultUnknownDisplayName = "Someone";

        private readonly IDBService _db;
        private readonly IPlayerNameService _playerName;
        private readonly Lazy<IDisguiseService> _disguise;

        public PlayerDescriptorService(IDBService db, IPlayerNameService playerName, Lazy<IDisguiseService> disguise)
        {
            _db = db;
            _playerName = playerName;
            _disguise = disguise;
        }

        private readonly AbilityType[] DescriptorAbilityTypes =
        {
            AbilityType.Might,
            AbilityType.Perception,
            AbilityType.Vitality,
            AbilityType.Willpower,
            AbilityType.Agility,
            AbilityType.Social
        };

        private readonly HashSet<AppearanceType> DescriptorSpeciesAppearanceTypes = new()
        {
            AppearanceType.Human,
            AppearanceType.Bothan,
            AppearanceType.Chiss,
            AppearanceType.Zabrak,
            AppearanceType.Wookiee,
            AppearanceType.Twilek,
            AppearanceType.Cyborg,
            AppearanceType.Cathar,
            AppearanceType.Trandoshan,
            AppearanceType.Mirialan,
            AppearanceType.Echani,
            AppearanceType.MonCalamari,
            AppearanceType.Ugnaught,
            AppearanceType.Rodian,
            AppearanceType.Togruta,
            AppearanceType.KelDor,
            AppearanceType.Droid,
            AppearanceType.Nautolan,
            AppearanceType.Ewok
        };

        private readonly Dictionary<AbilityType, string[]> StatDescriptorAdjectives = new()
        {
            { AbilityType.Might, new[] { "Strong", "Powerful", "Sturdy", "Athletic", "Broad-Shouldered" } },
            { AbilityType.Perception, new[] { "Watchful", "Keen-Eyed", "Observant", "Alert", "Attentive" } },
            { AbilityType.Vitality, new[] { "Hardy", "Robust", "Resilient", "Steady", "Durable" } },
            { AbilityType.Willpower, new[] { "Wise", "Resolute", "Centered", "Composed", "Focused" } },
            { AbilityType.Agility, new[] { "Nimble", "Quick", "Graceful", "Light-Footed", "Lithe" } },
            { AbilityType.Social, new[] { "Charming", "Commanding", "Well-Spoken", "Poised", "Courteous" } }
        };

        private readonly string[] GenericDescriptorAdjectives =
        {
            "Unfamiliar",
            "Quiet",
            "Calm",
            "Reserved",
            "Unassuming",
            "Curious",
            "Notable",
            "Steady",
            "Composed",
            "Balanced"
        };

        public void SetUnknownDisplayName(uint player, string name)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                throw new ArgumentException("Unknown display names may only be set for player characters.");

            var validationError = _playerName.ValidateKnownNameInput(name);
            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var sanitizedName = _playerName.SanitizeKnownName(name);
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            var previousDescriptor = dbPlayer.UnknownDisplayName ?? string.Empty;
            dbPlayer.UnknownDisplayName = sanitizedName;
            _db.Set(dbPlayer);

            WriteStructuredLog(LogGroup.PublicDescriptors,
                "Base descriptor changed: PlayerId={PlayerId} PlayerName={PlayerName} IdentityKey={IdentityKey} ActiveIdentityKey={ActiveIdentityKey} PreviousDescriptor={PreviousDescriptor} Descriptor={Descriptor}",
                playerId, GetName(player), playerId, GetDisguiseIdentityKey(player), previousDescriptor, sanitizedName);

            _playerName.RefreshNameOverridesForPlayer(player);
        }

        public bool EnsureUnknownDisplayName(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null ||
                !string.IsNullOrWhiteSpace(_playerName.SanitizeKnownName(dbPlayer.UnknownDisplayName)))
            {
                return false;
            }

            var generatedDisplayName = GenerateUnknownDisplayName(dbPlayer);
            if (string.IsNullOrWhiteSpace(generatedDisplayName))
                return false;

            var previousDescriptor = dbPlayer.UnknownDisplayName ?? string.Empty;
            dbPlayer.UnknownDisplayName = generatedDisplayName;
            _db.Set(dbPlayer);

            WriteStructuredLog(LogGroup.PublicDescriptors,
                "Base descriptor generated: PlayerId={PlayerId} PlayerName={PlayerName} IdentityKey={IdentityKey} ActiveIdentityKey={ActiveIdentityKey} PreviousDescriptor={PreviousDescriptor} Descriptor={Descriptor}",
                playerId, GetName(player), playerId, GetDisguiseIdentityKey(player), previousDescriptor, generatedDisplayName);
            return true;
        }

        public string GenerateUnknownDisplayName(Player dbPlayer)
        {
            var adjective = ResolveDescriptorAdjective(dbPlayer);
            var species = ResolveSpeciesName(dbPlayer?.OriginalAppearanceType ?? AppearanceType.Invalid);

            if (string.IsNullOrWhiteSpace(adjective) ||
                string.IsNullOrWhiteSpace(species))
            {
                return DefaultUnknownDisplayName;
            }

            return _playerName.SanitizeKnownName($"{adjective} {species}");
        }

        public string GetUnknownDisplayName(uint target)
        {
            var assignedDisplayName = GetAssignedUnknownDisplayName(target);
            return string.IsNullOrWhiteSpace(assignedDisplayName)
                ? DefaultUnknownDisplayName
                : assignedDisplayName;
        }

        /// <summary>
        /// Same as <see cref="GetUnknownDisplayNameByPlayerId"/>, for a player record already loaded.
        /// </summary>
        public string GetUnknownDisplayName(Player dbPlayer)
        {
            var assignedDisplayName = _playerName.SanitizeKnownName(dbPlayer?.UnknownDisplayName);
            return string.IsNullOrWhiteSpace(assignedDisplayName)
                ? DefaultUnknownDisplayName
                : assignedDisplayName;
        }

        public string GetUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            var assignedDisplayName = GetAssignedUnknownDisplayNameByPlayerId(targetPlayerId);
            return string.IsNullOrWhiteSpace(assignedDisplayName)
                ? DefaultUnknownDisplayName
                : assignedDisplayName;
        }

        private string ResolveDescriptorAdjective(Player dbPlayer)
        {
            if (dbPlayer == null)
                return PickGenericDescriptorAdjective(string.Empty, "missing-player");

            var seed = string.IsNullOrWhiteSpace(dbPlayer.Id)
                ? dbPlayer.Name
                : dbPlayer.Id;

            if (ShouldUseGenericDescriptorAdjective(seed) ||
                !TryResolveDescriptorAbility(dbPlayer, seed, out var ability))
            {
                return PickGenericDescriptorAdjective(seed, "generic-adjective");
            }

            var adjectives = StatDescriptorAdjectives[ability];
            return adjectives[GetStableIndex(seed, $"stat-adjective-{ability}", adjectives.Length)];
        }

        private bool ShouldUseGenericDescriptorAdjective(string seed)
        {
            return GetStableIndex(seed, "descriptor-source", 100) < GenericDescriptorChancePercent;
        }

        private bool TryResolveDescriptorAbility(Player dbPlayer, string seed, out AbilityType ability)
        {
            ability = AbilityType.Invalid;

            if (dbPlayer?.BaseStats == null)
                return false;

            var highestValue = 0;
            var highestAbilities = new List<AbilityType>();

            foreach (var abilityType in DescriptorAbilityTypes)
            {
                if (!dbPlayer.BaseStats.TryGetValue(abilityType, out var value) ||
                    value <= 0)
                {
                    continue;
                }

                if (value > highestValue)
                {
                    highestValue = value;
                    highestAbilities.Clear();
                    highestAbilities.Add(abilityType);
                }
                else if (value == highestValue)
                {
                    highestAbilities.Add(abilityType);
                }
            }

            if (highestAbilities.Count <= 0)
                return false;

            ability = highestAbilities[GetStableIndex(seed, "descriptor-ability", highestAbilities.Count)];
            return ability != AbilityType.Invalid;
        }

        private string PickGenericDescriptorAdjective(string seed, string salt)
        {
            return GenericDescriptorAdjectives[GetStableIndex(seed, salt, GenericDescriptorAdjectives.Length)];
        }

        private string ResolveSpeciesName(AppearanceType appearanceType)
        {
            if (appearanceType == AppearanceType.Invalid ||
                !DescriptorSpeciesAppearanceTypes.Contains(appearanceType))
            {
                return HumanoidSpeciesName;
            }

            var label = Get2DAString(Appearance2DA, AppearanceLabelColumn, (int)appearanceType);
            if (string.IsNullOrWhiteSpace(label) ||
                label == "****")
            {
                return HumanoidSpeciesName;
            }

            label = label.Trim().Trim('"');
            label = label.Replace(DynamicAppearanceLabelPrefix, string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim()
                .Trim('"');

            return string.IsNullOrWhiteSpace(label) || label == "****"
                ? HumanoidSpeciesName
                : label;
        }

        private int GetStableIndex(string seed, string salt, int count)
        {
            if (count <= 0)
                return 0;

            unchecked
            {
                var hash = 2166136261u;
                var value = $"{seed}:{salt}";

                foreach (var character in value)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }

                return (int)(hash % (uint)count);
            }
        }

        private string GetAssignedUnknownDisplayName(uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))
                return string.Empty;

            return GetAssignedUnknownDisplayNameByPlayerId(GetObjectUUID(target));
        }

        private string GetAssignedUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId))
                return string.Empty;

            var dbPlayer = _db.Get<Player>(targetPlayerId);
            return _playerName.SanitizeKnownName(dbPlayer?.UnknownDisplayName);
        }

        /// <summary>The structured log line: <c>Log.WriteStructured</c> (<c>Log</c> stays static in Phase 2).</summary>
        protected virtual void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues)
        {
            Log.WriteStructured(group, messageTemplate, propertyValues);
        }

        /// <summary>The identity key of the player's active identity: <c>Disguise.GetIdentityKey</c>.</summary>
        protected virtual string GetDisguiseIdentityKey(uint player)
        {
            return _disguise.Value.GetIdentityKey(player);
        }
    }
}
