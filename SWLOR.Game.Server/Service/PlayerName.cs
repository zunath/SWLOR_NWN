using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerName
    {
        private const int MaxKnownNameLength = 64;
        private const string UnknownName = "Someone";
        private static readonly string UnknownNamePrefix = ColorToken.TokenStart(127, 127, 127);
        private static readonly string UnknownNameSuffix = ColorToken.TokenEnd();

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ApplyNameOverridesOnEnter()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player))
                return;

            ApplyNameOverridesForPlayer(player);
            DelayCommand(1.0f, () => ApplyNameOverridesForPlayer(player));
        }

        public static string GetDisplayName(uint observer, uint target)
        {
            return ResolveDisplayName(observer, target, out _);
        }

        public static string GetColoredDisplayName(uint observer, uint target)
        {
            if (!GetIsPC(target) || GetIsDM(target))
                return ColorToken.GetNameNPCColor(target);

            var displayName = ResolveDisplayName(observer, target, out var isUnknown);
            return isUnknown
                ? ColorToken.Gray(displayName)
                : ColorToken.GetPCColor(displayName);
        }

        public static string SanitizeKnownName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var strippedName = UtilPlugin.StripColors(name).Trim();
            var builder = new StringBuilder();

            foreach (var character in strippedName)
            {
                if (char.IsControl(character))
                    continue;

                builder.Append(character);
            }

            var sanitizedName = builder.ToString().Trim();

            while (sanitizedName.Contains("  "))
            {
                sanitizedName = sanitizedName.Replace("  ", " ");
            }

            return sanitizedName;
        }

        public static string ValidateKnownName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Please enter a name.";

            if (name.Length > MaxKnownNameLength)
                return $"Names may be no longer than {MaxKnownNameLength} characters.";

            return string.Empty;
        }

        public static void SetKnownName(uint observer, uint target, string name)
        {
            var sanitizedName = SanitizeKnownName(name);
            var validationError = ValidateKnownName(sanitizedName);

            if (!string.IsNullOrWhiteSpace(validationError))
                throw new ArgumentException(validationError);

            var targetId = GetObjectUUID(target);
            var dbKnownNames = GetKnownNames(observer, true);
            dbKnownNames.KnownNames[targetId] = sanitizedName;
            DB.Set(dbKnownNames);

            ApplyNameOverride(observer, target);
        }

        public static void ForgetKnownName(uint observer, uint target)
        {
            var targetId = GetObjectUUID(target);
            var dbKnownNames = GetKnownNames(observer, false);

            if (dbKnownNames?.KnownNames == null ||
                !dbKnownNames.KnownNames.ContainsKey(targetId))
            {
                ApplyNameOverride(observer, target);
                return;
            }

            dbKnownNames.KnownNames.Remove(targetId);
            DB.Set(dbKnownNames);

            ApplyNameOverride(observer, target);
        }

        private static PlayerKnownName GetKnownNames(uint observer, bool createIfMissing)
        {
            var observerId = GetObjectUUID(observer);
            var dbKnownNames = FindKnownNames(observerId);

            if (dbKnownNames == null && createIfMissing)
            {
                dbKnownNames = new PlayerKnownName(observerId);
            }

            if (dbKnownNames != null && dbKnownNames.KnownNames == null)
            {
                dbKnownNames.KnownNames = new Dictionary<string, string>();
            }

            return dbKnownNames;
        }

        private static PlayerKnownName FindKnownNames(string observerId)
        {
            return DB.Search(new DBQuery<PlayerKnownName>()
                    .AddFieldSearch(nameof(PlayerKnownName.ObserverPlayerId), observerId, false))
                .FirstOrDefault();
        }

        private static void ApplyNameOverridesForPlayer(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player))
                return;

            RenamePlugin.SetPCNameOverride(player, UnknownName, UnknownNamePrefix, UnknownNameSuffix, PlayerNameOverrideType.Default);
            RenamePlugin.SetPCNameOverride(player, GetName(player), string.Empty, string.Empty, PlayerNameOverrideType.Default, player);
            RenamePlugin.SetPCNameOverride(player, UnknownName, UnknownNamePrefix, UnknownNameSuffix, PlayerNameOverrideType.Obfuscate);

            for (var otherPlayer = GetFirstPC(); GetIsObjectValid(otherPlayer); otherPlayer = GetNextPC())
            {
                if (!GetIsPC(otherPlayer) || GetIsDM(otherPlayer))
                    continue;

                ApplyNameOverride(player, otherPlayer);
                ApplyNameOverride(otherPlayer, player);
            }
        }

        private static void ApplyNameOverride(uint observer, uint target)
        {
            if (!GetIsObjectValid(observer) ||
                !GetIsObjectValid(target) ||
                !GetIsPC(observer) ||
                !GetIsPC(target) ||
                GetIsDM(observer) ||
                GetIsDM(target))
            {
                return;
            }

            var displayName = ResolveDisplayName(observer, target, out var isUnknown);
            var prefix = isUnknown ? UnknownNamePrefix : string.Empty;
            var suffix = isUnknown ? UnknownNameSuffix : string.Empty;

            RenamePlugin.SetPCNameOverride(target, displayName, prefix, suffix, PlayerNameOverrideType.Default, observer);
        }

        private static string ResolveDisplayName(uint observer, uint target, out bool isUnknown)
        {
            isUnknown = false;

            if (!GetIsObjectValid(target))
                return string.Empty;

            if (!GetIsPC(target) || GetIsDM(target))
                return GetName(target);

            if (!GetIsObjectValid(observer) ||
                observer == target ||
                GetIsDM(observer) ||
                GetIsDMPossessed(observer))
            {
                return GetName(target);
            }

            if (!GetIsPC(observer))
                return GetName(target);

            if (TryGetKnownName(observer, target, out var knownName))
                return knownName;

            isUnknown = true;
            return UnknownName;
        }

        private static bool TryGetKnownName(uint observer, uint target, out string knownName)
        {
            knownName = string.Empty;
            var targetId = GetObjectUUID(target);
            var dbKnownNames = GetKnownNames(observer, false);

            return dbKnownNames?.KnownNames != null &&
                   dbKnownNames.KnownNames.TryGetValue(targetId, out knownName) &&
                   !string.IsNullOrWhiteSpace(knownName);
        }
    }
}
