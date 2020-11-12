using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Race
    {
        private class RacialAppearance
        {
            public int HeadId { get; set; } = 1;
            public int SkinColorId { get; set; } = 2;
            public int HairColorId { get; set; }
            public AppearanceType AppearanceType { get; set; } = AppearanceType.Human;
            public float Scale { get; set; } = 1.0f;

            public int NeckId { get; set; } = 1;
            public int TorsoId { get; set; } = 1;
            public int PelvisId { get; set; } = 1;

            public int RightBicepId { get; set; } = 1;
            public int RightForearmId { get; set; } = 1;
            public int RightHandId { get; set; } = 1;
            public int RightThighId { get; set; } = 1;
            public int RightShinId { get; set; } = 1;
            public int RightFootId { get; set; } = 1;

            public int LeftBicepId { get; set; } = 1;
            public int LeftForearmId { get; set; } = 1;
            public int LeftHandId { get; set; } = 1;
            public int LeftThighId { get; set; } = 1;
            public int LeftShinId { get; set; } = 1;
            public int LeftFootId { get; set; } = 1;
        }

        private static readonly Dictionary<RacialType, RacialAppearance> _defaultRaceAppearancesMale = new Dictionary<RacialType, RacialAppearance>();
        private static readonly Dictionary<RacialType, RacialAppearance> _defaultRaceAppearancesFemale = new Dictionary<RacialType, RacialAppearance>();

        /// <summary>
        /// When the module loads, cache all default race appearances.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void LoadRaces()
        {
            // Male appearances
            _defaultRaceAppearancesMale[RacialType.Human] = new RacialAppearance();
            _defaultRaceAppearancesMale[RacialType.Bothan] = new RacialAppearance
            {
                SkinColorId = 6,
                HairColorId = 1,
                AppearanceType = AppearanceType.Elf,
                HeadId = 40
            };
            _defaultRaceAppearancesMale[RacialType.Chiss] = new RacialAppearance
            {
                SkinColorId = 137,
                HairColorId = 134,
                HeadId = 33
            };
            _defaultRaceAppearancesMale[RacialType.Zabrak] = new RacialAppearance
            {
                SkinColorId = 88,
                HairColorId = 0,
                HeadId = 103
            };
            _defaultRaceAppearancesMale[RacialType.Twilek] = new RacialAppearance
            {
                SkinColorId = 52,
                HairColorId = 0,
                HeadId = 115
            };
            _defaultRaceAppearancesMale[RacialType.Mirialan] = new RacialAppearance
            {
                SkinColorId = 38,
                HairColorId = 3,
                HeadId = 20
            };
            _defaultRaceAppearancesMale[RacialType.Echani] = new RacialAppearance
            {
                SkinColorId = 164,
                HairColorId = 16,
                HeadId = 182
            };
            _defaultRaceAppearancesMale[RacialType.Cathar] = new RacialAppearance
            {
                SkinColorId = 54,
                HairColorId = 0,
                AppearanceType = AppearanceType.HalfOrc,
                HeadId = 27
            };
            _defaultRaceAppearancesMale[RacialType.Trandoshan] = new RacialAppearance
            {
                SkinColorId = 39,
                HairColorId = 4,
                HeadId = 162,
                NeckId = 201,
                TorsoId = 201,
                PelvisId = 201,

                RightBicepId = 201,
                RightForearmId = 201,
                RightHandId = 201,
                RightThighId = 201,
                RightShinId = 201,
                RightFootId = 201,

                LeftBicepId = 201,
                LeftForearmId = 201,
                LeftHandId = 201,
                LeftThighId = 201,
                LeftShinId = 201,
                LeftFootId = 201
            };
            _defaultRaceAppearancesMale[RacialType.Wookiee] = new RacialAppearance
            {
                AppearanceType = AppearanceType.Elf,
                SkinColorId = 0,
                HairColorId = 0,
                HeadId = 192,
                Scale = 1.0f,

                NeckId = 1,
                TorsoId = 208,
                PelvisId = 208,

                RightBicepId = 208,
                RightForearmId = 208,
                RightHandId = 208,
                RightThighId = 208,
                RightShinId = 208,
                RightFootId = 208,

                LeftBicepId = 208,
                LeftForearmId = 208,
                LeftHandId = 208,
                LeftThighId = 208,
                LeftShinId = 208,
                LeftFootId = 208
            };
            _defaultRaceAppearancesMale[RacialType.MonCalamari] = new RacialAppearance
            {
                SkinColorId = 6,
                HairColorId = 7,
                HeadId = 6,

                NeckId = 1,
                TorsoId = 204,
                PelvisId = 204,

                RightBicepId = 204,
                RightForearmId = 204,
                RightHandId = 204,
                RightThighId = 204,
                RightShinId = 204,
                RightFootId = 204,

                LeftBicepId = 204,
                LeftForearmId = 204,
                LeftHandId = 204,
                LeftThighId = 204,
                LeftShinId = 204,
                LeftFootId = 204
            };
            _defaultRaceAppearancesMale[RacialType.Ugnaught] = new RacialAppearance
            {
                AppearanceType = AppearanceType.Dwarf,

                SkinColorId = 0,
                HairColorId = 0,
                HeadId = 100,
                PelvisId = 7
            };

            // Female appearances
            _defaultRaceAppearancesFemale[RacialType.Human] = new RacialAppearance();
            _defaultRaceAppearancesFemale[RacialType.Bothan] = new RacialAppearance
            {
                SkinColorId = 6,
                HairColorId = 1,
                AppearanceType = AppearanceType.Elf,
                HeadId = 109
            };
            _defaultRaceAppearancesFemale[RacialType.Chiss] = new RacialAppearance
            {
                SkinColorId = 137,
                HairColorId = 134,
                HeadId = 191
            };
            _defaultRaceAppearancesFemale[RacialType.Zabrak] = new RacialAppearance
            {
                SkinColorId = 88,
                HairColorId = 0,
                HeadId = 120
            };
            _defaultRaceAppearancesFemale[RacialType.Twilek] = new RacialAppearance
            {
                SkinColorId = 52,
                HairColorId = 0,
                HeadId = 145
            };
            _defaultRaceAppearancesFemale[RacialType.Mirialan] = new RacialAppearance
            {
                SkinColorId = 38,
                HairColorId = 3,
                HeadId = 20
            };
            _defaultRaceAppearancesFemale[RacialType.Echani] = new RacialAppearance
            {
                SkinColorId = 164,
                HairColorId = 16,
                HeadId = 45
            };
            _defaultRaceAppearancesFemale[RacialType.Cathar] = new RacialAppearance
            {
                SkinColorId = 54,
                HairColorId = 0,
                AppearanceType = AppearanceType.HalfOrc,
                HeadId = 18
            };
            _defaultRaceAppearancesFemale[RacialType.Trandoshan] = new RacialAppearance
            {
                SkinColorId = 39,
                HairColorId = 4,
                HeadId = 135,
                NeckId = 201,
                TorsoId = 201,
                PelvisId = 201,
                RightBicepId = 201,
                RightForearmId = 201,
                RightHandId = 201,
                RightThighId = 201,
                RightShinId = 201,
                RightFootId = 201,

                LeftBicepId = 201,
                LeftForearmId = 201,
                LeftHandId = 201,
                LeftThighId = 201,
                LeftShinId = 201,
                LeftFootId = 201
            };
            _defaultRaceAppearancesFemale[RacialType.Wookiee] = new RacialAppearance
            {
                AppearanceType = AppearanceType.Elf,
                SkinColorId = 0,
                HairColorId = 0,
                HeadId = 110,
                Scale = 1.0f,

                NeckId = 1,
                TorsoId = 208,
                PelvisId = 208,

                RightBicepId = 208,
                RightForearmId = 208,
                RightHandId = 208,
                RightThighId = 208,
                RightShinId = 208,
                RightFootId = 208,

                LeftBicepId = 208,
                LeftForearmId = 208,
                LeftHandId = 208,
                LeftThighId = 208,
                LeftShinId = 208,
                LeftFootId = 208
            };
            _defaultRaceAppearancesFemale[RacialType.MonCalamari] = new RacialAppearance
            {
                SkinColorId = 6,
                HairColorId = 7,
                HeadId = 6,

                NeckId = 1,
                TorsoId = 204,
                PelvisId = 204,

                RightBicepId = 204,
                RightForearmId = 204,
                RightHandId = 204,
                RightThighId = 204,
                RightShinId = 204,
                RightFootId = 204,

                LeftBicepId = 204,
                LeftForearmId = 204,
                LeftHandId = 204,
                LeftThighId = 204,
                LeftShinId = 204,
                LeftFootId = 204
            };
            _defaultRaceAppearancesFemale[RacialType.Ugnaught] = new RacialAppearance
            {
                AppearanceType = AppearanceType.Dwarf,

                SkinColorId = 0,
                HairColorId = 0,
                HeadId = 100,
                PelvisId = 7
            };

        }

        /// <summary>
        /// When a player enters the server, apply the proper scaling to their character.
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void ApplyWookieeScaling()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            var gender = GetGender(player);
            var racialType = GetRacialType(player);

            // Ensure the race + gender configuration exists.
            if (gender == Gender.Male && !_defaultRaceAppearancesMale.ContainsKey(racialType) ||
                gender != Gender.Male && !_defaultRaceAppearancesFemale.ContainsKey(racialType))
                return;

            var config = gender == Gender.Male
                ? _defaultRaceAppearancesMale[racialType]
                : _defaultRaceAppearancesFemale[racialType];

            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, config.Scale);
        }

        /// <summary>
        /// Sets the default race appearance for the player's racial type.
        /// This should be called exactly one time on player initialization.
        /// </summary>
        /// <param name="player">The player whose appearance will be adjusted.</param>
        public static void SetDefaultRaceAppearance(uint player)
        {
            var gender = GetGender(player);
            var racialType = GetRacialType(player);
            var raceConfig = gender == Gender.Male
                ? _defaultRaceAppearancesMale[racialType]
                : _defaultRaceAppearancesFemale[racialType];

            // Appearance, Skin, and Hair
            SetCreatureAppearanceType(player, raceConfig.AppearanceType);
            SetColor(player, ColorChannel.Skin, raceConfig.SkinColorId);
            SetColor(player, ColorChannel.Hair, raceConfig.HairColorId);

            // Body parts
            SetCreatureBodyPart(CreaturePart.Head, raceConfig.HeadId, player);

            SetCreatureBodyPart(CreaturePart.Neck, raceConfig.NeckId, player);
            SetCreatureBodyPart(CreaturePart.Torso, raceConfig.TorsoId, player);
            SetCreatureBodyPart(CreaturePart.Pelvis, raceConfig.PelvisId, player);

            SetCreatureBodyPart(CreaturePart.RightBicep, raceConfig.RightBicepId, player);
            SetCreatureBodyPart(CreaturePart.RightForearm, raceConfig.RightForearmId, player);
            SetCreatureBodyPart(CreaturePart.RightHand, raceConfig.RightHandId, player);
            SetCreatureBodyPart(CreaturePart.RightThigh, raceConfig.RightThighId, player);
            SetCreatureBodyPart(CreaturePart.RightShin, raceConfig.RightShinId, player);
            SetCreatureBodyPart(CreaturePart.RightFoot, raceConfig.RightFootId, player);

            SetCreatureBodyPart(CreaturePart.LeftBicep, raceConfig.LeftBicepId, player);
            SetCreatureBodyPart(CreaturePart.LeftForearm, raceConfig.LeftForearmId, player);
            SetCreatureBodyPart(CreaturePart.LeftHand, raceConfig.LeftHandId, player);
            SetCreatureBodyPart(CreaturePart.LeftThigh, raceConfig.LeftThighId, player);
            SetCreatureBodyPart(CreaturePart.LeftShin, raceConfig.LeftShinId, player);
            SetCreatureBodyPart(CreaturePart.LeftFoot, raceConfig.LeftFootId, player);
        }
    }
}
