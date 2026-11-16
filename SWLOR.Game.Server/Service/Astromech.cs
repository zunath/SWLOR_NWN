using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.AstromechService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The astromech player species: which chassis a character wears, its paint and its size.
    /// </summary>
    public static class Astromech
    {
        /// <summary>The astromech voice set: its row in soundset.2da.</summary>
        public const int SoundSetId = 954;

        public static bool IsAstromech(uint creature)
        {
            return GetRacialType(creature) == RacialType.Astromech;
        }

        /// <summary>
        /// The chassis a player has chosen. It is kept as the stored appearance of the character,
        /// which is also what leaving space restores.
        /// </summary>
        public static AstromechChassis GetChassis(Player dbPlayer)
        {
            return AstromechChassisCatalog.Get(dbPlayer?.OriginalAppearanceType ?? AppearanceType.Invalid);
        }

        public static TintMapColor GetPaint(Player dbPlayer, AstromechPaintZone zone)
        {
            if (dbPlayer?.AstromechPaint != null &&
                dbPlayer.AstromechPaint.TryGetValue(zone, out var stored) &&
                TintMapColor.TryFromStoredValue(stored, out var color))
            {
                return color;
            }

            return GetChassis(dbPlayer).GetDefaultPaint(zone);
        }

        /// <summary>
        /// When an astromech enters the server, put its paint variables back before the model is colored.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RestoreOnEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || !IsAstromech(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            ApplyPaint(player, dbPlayer, false);
        }

        /// <summary>
        /// Moves a player into another chassis, keeping their paint and size.
        /// </summary>
        public static void SetChassis(uint player, AppearanceType appearance)
        {
            if (!IsAstromech(player) || !AstromechChassisCatalog.IsChassis(appearance))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            var previous = GetChassis(dbPlayer);
            dbPlayer.OriginalAppearanceType = appearance;
            DB.Set(dbPlayer);

            foreach (var variable in AstromechChassisCatalog.BuildPaintVariables(previous, null).Keys)
                DeleteLocalInt(player, variable);

            // The portrait follows the chassis unless the player has picked a portrait of their own.
            if (GetPortraitResRef(player) == previous.Portrait)
                SetPortraitResRef(player, GetChassis(dbPlayer).Portrait);

            // In space the ship model is showing; the chassis is put back when the player leaves it.
            if (Space.IsPlayerInSpaceMode(player))
                return;

            SetCreatureAppearanceType(player, appearance);
            ApplyPaint(player, dbPlayer, true);
        }

        public static void SetPaint(uint player, AstromechPaintZone zone, TintMapColor color)
        {
            if (!IsAstromech(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.AstromechPaint ??= new Dictionary<AstromechPaintZone, int>();
            dbPlayer.AstromechPaint[zone] = AstromechChassisCatalog.NormalizePaint(color).ToStoredValue();
            DB.Set(dbPlayer);
            ApplyPaint(player, dbPlayer, true);
        }

        /// <summary>
        /// Returns a zone to the paint its chassis comes with.
        /// </summary>
        public static void ResetPaint(uint player, AstromechPaintZone zone)
        {
            if (!IsAstromech(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer?.AstromechPaint == null || !dbPlayer.AstromechPaint.Remove(zone))
                return;

            DB.Set(dbPlayer);
            ApplyPaint(player, dbPlayer, true);
        }

        public static void SetScale(uint player, float scale)
        {
            if (!IsAstromech(player))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.AppearanceScale = AstromechChassisCatalog.ClampScale(scale);
            DB.Set(dbPlayer);

            if (!Space.IsPlayerInSpaceMode(player))
                SetObjectVisualTransform(player, ObjectVisualTransform.Scale, dbPlayer.AppearanceScale);
        }

        /// <summary>
        /// Writes the paint variables for the chassis the player wears and, when asked, redraws the model at once.
        /// </summary>
        public static void ApplyPaint(uint player, Player dbPlayer, bool refreshNow)
        {
            var variables = AstromechChassisCatalog.BuildPaintVariables(GetChassis(dbPlayer), dbPlayer.AstromechPaint);
            foreach (var (name, value) in variables)
                SetLocalInt(player, name, value);

            if (refreshNow)
                TintMapService.RefreshAfterColorChange(player);
        }
    }
}
