using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using AbilityType = SWLOR.NWN.API.NWScript.Enum.AbilityType;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerDescriptor
    {
        private const int GenericDescriptorChancePercent = 25;
        private const string Appearance2DA = "appearance";
        private const string AppearanceLabelColumn = "LABEL";
        private const string DynamicAppearanceLabelPrefix = "(Dynamic)";
        private const string HumanoidSpeciesName = "Humanoid";
        public const string DefaultUnknownDisplayName = "Someone";

        private static readonly AbilityType[] DescriptorAbilityTypes =
        {
            AbilityType.Might,
            AbilityType.Perception,
            AbilityType.Vitality,
            AbilityType.Willpower,
            AbilityType.Agility,
            AbilityType.Social
        };

        private static readonly HashSet<AppearanceType> DescriptorSpeciesAppearanceTypes = new()
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

        private static readonly Dictionary<AbilityType, string[]> StatDescriptorAdjectives = new()
        {
            { AbilityType.Might, new[] { "Strong", "Powerful", "Sturdy", "Athletic", "Broad-Shouldered" } },
            { AbilityType.Perception, new[] { "Watchful", "Keen-Eyed", "Observant", "Alert", "Attentive" } },
            { AbilityType.Vitality, new[] { "Hardy", "Robust", "Resilient", "Steady", "Durable" } },
            { AbilityType.Willpower, new[] { "Wise", "Resolute", "Centered", "Composed", "Focused" } },
            { AbilityType.Agility, new[] { "Nimble", "Quick", "Graceful", "Light-Footed", "Lithe" } },
            { AbilityType.Social, new[] { "Charming", "Commanding", "Well-Spoken", "Poised", "Courteous" } }
        };

        private static readonly string[] GenericDescriptorAdjectives =
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

        public static void SetUnknownDisplayName(uint player, string name)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                throw new ArgumentException("Unknown display names may only be set for player characters.");

            var validationError = PlayerName.ValidateKnownNameInput(name);
            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var sanitizedName = PlayerName.SanitizeKnownName(name);
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.UnknownDisplayName = sanitizedName;
            DB.Set(dbPlayer);

            PlayerName.RefreshNameOverridesForPlayer(player);
        }

        public static bool EnsureUnknownDisplayName(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            if (string.IsNullOrWhiteSpace(playerId))
                return false;

            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null ||
                !string.IsNullOrWhiteSpace(PlayerName.SanitizeKnownName(dbPlayer.UnknownDisplayName)))
            {
                return false;
            }

            var generatedDisplayName = GenerateUnknownDisplayName(dbPlayer);
            if (string.IsNullOrWhiteSpace(generatedDisplayName))
                return false;

            dbPlayer.UnknownDisplayName = generatedDisplayName;
            DB.Set(dbPlayer);
            return true;
        }

        public static string GenerateUnknownDisplayName(Player dbPlayer)
        {
            var adjective = ResolveDescriptorAdjective(dbPlayer);
            var species = ResolveSpeciesName(dbPlayer?.OriginalAppearanceType ?? AppearanceType.Invalid);

            if (string.IsNullOrWhiteSpace(adjective) ||
                string.IsNullOrWhiteSpace(species))
            {
                return DefaultUnknownDisplayName;
            }

            return PlayerName.SanitizeKnownName($"{adjective} {species}");
        }

        public static string GetUnknownDisplayName(uint target)
        {
            var assignedDisplayName = GetAssignedUnknownDisplayName(target);
            return string.IsNullOrWhiteSpace(assignedDisplayName)
                ? DefaultUnknownDisplayName
                : assignedDisplayName;
        }

        public static string GetUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            var assignedDisplayName = GetAssignedUnknownDisplayNameByPlayerId(targetPlayerId);
            return string.IsNullOrWhiteSpace(assignedDisplayName)
                ? DefaultUnknownDisplayName
                : assignedDisplayName;
        }

        private static string ResolveDescriptorAdjective(Player dbPlayer)
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

        private static bool ShouldUseGenericDescriptorAdjective(string seed)
        {
            return GetStableIndex(seed, "descriptor-source", 100) < GenericDescriptorChancePercent;
        }

        private static bool TryResolveDescriptorAbility(Player dbPlayer, string seed, out AbilityType ability)
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

        private static string PickGenericDescriptorAdjective(string seed, string salt)
        {
            return GenericDescriptorAdjectives[GetStableIndex(seed, salt, GenericDescriptorAdjectives.Length)];
        }

        private static string ResolveSpeciesName(AppearanceType appearanceType)
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

        private static int GetStableIndex(string seed, string salt, int count)
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

        private static string GetAssignedUnknownDisplayName(uint target)
        {
            if (!GetIsObjectValid(target) || !GetIsPC(target) || GetIsDM(target))
                return string.Empty;

            return GetAssignedUnknownDisplayNameByPlayerId(GetObjectUUID(target));
        }

        private static string GetAssignedUnknownDisplayNameByPlayerId(string targetPlayerId)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId))
                return string.Empty;

            var dbPlayer = DB.Get<Player>(targetPlayerId);
            return PlayerName.SanitizeKnownName(dbPlayer?.UnknownDisplayName);
        }
    }
}
