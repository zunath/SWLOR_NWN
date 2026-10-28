using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview
{
    /// <summary>
    /// Names of the Appearance Editor part pictures in the sw_part_previews hak. Mirrors
    /// tools/appearance_previews (preview_resref and jobs.build): "a" + key + view letter.
    /// </summary>
    public static class AppearancePreview
    {
        public const int MaxResrefLength = 16;

        /// <summary>A plain tile shown for an empty slot or a part without a picture.</summary>
        public const string EmptyPicture = "apart_empty";
        private static readonly Regex NonHumanBody = new("^p([fm])[deoz]0_(.+)$", RegexOptions.Compiled);
        private static readonly ConcurrentDictionary<string, bool> PictureExists = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The picture to show for a part. Pictures of a non-human body that look the same as the
        /// human body's are not shipped, so a missing one falls back to the human picture.
        /// </summary>
        public static string Picture(string key, AppearancePreviewView view) =>
            Picture(key, view, resref => PictureExists.GetOrAdd(resref,
                name => !string.IsNullOrEmpty(ResManGetAliasFor(name, ResType.DDS))));

        public static string Picture(string key, AppearancePreviewView view, Func<string, bool> exists)
        {
            var resref = Resref(key, view);
            if (resref.Length == 0)
                return EmptyPicture;
            if (exists(resref))
                return resref;
            var twin = HumanTwinKey(key);
            var twinResref = twin == null ? string.Empty : Resref(twin, view);
            return twinResref.Length > 0 && exists(twinResref) ? twinResref : EmptyPicture;
        }

        /// <summary>The human body's key for a part key of another body type, or null.</summary>
        public static string HumanTwinKey(string key)
        {
            var match = NonHumanBody.Match(key ?? string.Empty);
            return match.Success ? $"p{match.Groups[1].Value}h0_{match.Groups[2].Value}" : null;
        }

        public static string Resref(string key, AppearancePreviewView view)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            var resref = $"a{key}{ViewLetter(view)}".ToLowerInvariant();
            return resref.Length <= MaxResrefLength ? resref : string.Empty;
        }

        public static char ViewLetter(AppearancePreviewView view) => view switch
        {
            AppearancePreviewView.Back => 'b',
            AppearancePreviewView.Angle => 'a',
            _ => 'f'
        };

        /// <summary>Body model prefix, e.g. pfh0: gender, appearance.2da RACE, normal phenotype.</summary>
        public static string BodyPrefix(Gender gender, string race)
        {
            if (string.IsNullOrWhiteSpace(race) || race.Length != 1)
                return string.Empty;
            return $"p{(gender == Gender.Female ? 'f' : 'm')}{char.ToLowerInvariant(race[0])}0";
        }

        public static string ArmorPartKey(string body, AppearanceEditorSlot slot, int partId)
        {
            var stem = ModelStem(slot);
            return partId > 0 && stem != null && body.Length > 0 ? $"{body}_{stem}{partId:D3}" : string.Empty;
        }

        public static string HeadKey(string body, int headId) =>
            headId > 0 && body.Length > 0 ? $"{body}_head{headId:D3}" : string.Empty;

        public static string HelmetKey(int helmetId) => helmetId > 0 ? $"helm_{helmetId:D3}" : string.Empty;

        /// <summary>
        /// Model of one part of a three-part weapon: part id 101 (colour 1, model 1) of a longsword top is
        /// wswls_t_011, the engine's model * 10 + colour number.
        /// </summary>
        public static string WeaponPartKey(string itemClass, AppearanceWeapon part, int partId)
        {
            var letter = part switch
            {
                AppearanceWeapon.Top => 't',
                AppearanceWeapon.Middle => 'm',
                AppearanceWeapon.Bottom => 'b',
                _ => '\0'
            };
            var model = partId % 100;
            var color = partId / 100;
            if (letter == '\0' || string.IsNullOrWhiteSpace(itemClass) || model <= 0 || color <= 0)
                return string.Empty;
            return $"{itemClass.Trim().ToLowerInvariant()}_{letter}_{model * 10 + color:D3}";
        }

        /// <summary>Model of a one-part weapon, e.g. a power glove: item class plus the part number.</summary>
        public static string SimpleWeaponKey(string itemClass, int partId) =>
            partId > 0 && !string.IsNullOrWhiteSpace(itemClass) ? $"{itemClass.Trim().ToLowerInvariant()}_{partId:D3}" : string.Empty;

        public static string CloakKey(string body, int cloakRow) =>
            cloakRow > 0 && body.Length > 0 ? $"{body}_clk{cloakRow:D3}" : string.Empty;

        /// <summary>Model name stem of an armor or body part slot (pfh0_<stem>###).</summary>
        public static string ModelStem(AppearanceEditorSlot slot) => slot switch
        {
            AppearanceEditorSlot.Neck => "neck",
            AppearanceEditorSlot.Chest => "chest",
            AppearanceEditorSlot.Belt => "belt",
            AppearanceEditorSlot.Pelvis => "pelvis",
            AppearanceEditorSlot.Robe => "robe",
            AppearanceEditorSlot.RightShoulder => "shor",
            AppearanceEditorSlot.LeftShoulder => "shol",
            AppearanceEditorSlot.RightBicep => "bicepr",
            AppearanceEditorSlot.LeftBicep => "bicepl",
            AppearanceEditorSlot.RightForearm => "forer",
            AppearanceEditorSlot.LeftForearm => "forel",
            AppearanceEditorSlot.RightHand => "handr",
            AppearanceEditorSlot.LeftHand => "handl",
            AppearanceEditorSlot.RightThigh => "legr",
            AppearanceEditorSlot.LeftThigh => "legl",
            AppearanceEditorSlot.RightShin => "shinr",
            AppearanceEditorSlot.LeftShin => "shinl",
            AppearanceEditorSlot.RightFoot => "footr",
            AppearanceEditorSlot.LeftFoot => "footl",
            _ => null
        };

        public static AppearanceArmor ToArmorPart(AppearanceEditorSlot slot) => slot switch
        {
            AppearanceEditorSlot.Neck => AppearanceArmor.Neck,
            AppearanceEditorSlot.Chest => AppearanceArmor.Torso,
            AppearanceEditorSlot.Belt => AppearanceArmor.Belt,
            AppearanceEditorSlot.Pelvis => AppearanceArmor.Pelvis,
            AppearanceEditorSlot.Robe => AppearanceArmor.Robe,
            AppearanceEditorSlot.RightShoulder => AppearanceArmor.RightShoulder,
            AppearanceEditorSlot.LeftShoulder => AppearanceArmor.LeftShoulder,
            AppearanceEditorSlot.RightBicep => AppearanceArmor.RightBicep,
            AppearanceEditorSlot.LeftBicep => AppearanceArmor.LeftBicep,
            AppearanceEditorSlot.RightForearm => AppearanceArmor.RightForearm,
            AppearanceEditorSlot.LeftForearm => AppearanceArmor.LeftForearm,
            AppearanceEditorSlot.RightHand => AppearanceArmor.RightHand,
            AppearanceEditorSlot.LeftHand => AppearanceArmor.LeftHand,
            AppearanceEditorSlot.RightThigh => AppearanceArmor.RightThigh,
            AppearanceEditorSlot.LeftThigh => AppearanceArmor.LeftThigh,
            AppearanceEditorSlot.RightShin => AppearanceArmor.RightShin,
            AppearanceEditorSlot.LeftShin => AppearanceArmor.LeftShin,
            AppearanceEditorSlot.RightFoot => AppearanceArmor.RightFoot,
            AppearanceEditorSlot.LeftFoot => AppearanceArmor.LeftFoot,
            _ => AppearanceArmor.Invalid
        };

        public static AppearanceEditorSlot FromArmorPart(AppearanceArmor part)
        {
            if (part == AppearanceArmor.Invalid)
                return AppearanceEditorSlot.Chest;
            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
            {
                if (ToArmorPart(slot) == part)
                    return slot;
            }
            return AppearanceEditorSlot.Chest;
        }

        /// <summary>
        /// Appearance tab body part category (the order LoadPartCategoryOptions lists), or -1.
        /// </summary>
        public static int BodyCategory(AppearanceEditorSlot slot) => slot switch
        {
            AppearanceEditorSlot.Head => 0,
            AppearanceEditorSlot.Chest => 1,
            AppearanceEditorSlot.Pelvis => 2,
            AppearanceEditorSlot.RightBicep => 3,
            AppearanceEditorSlot.RightForearm => 4,
            AppearanceEditorSlot.RightHand => 5,
            AppearanceEditorSlot.RightThigh => 6,
            AppearanceEditorSlot.RightShin => 7,
            AppearanceEditorSlot.RightFoot => 8,
            AppearanceEditorSlot.LeftBicep => 9,
            AppearanceEditorSlot.LeftForearm => 10,
            AppearanceEditorSlot.LeftHand => 11,
            AppearanceEditorSlot.LeftThigh => 12,
            AppearanceEditorSlot.LeftShin => 13,
            AppearanceEditorSlot.LeftFoot => 14,
            _ => -1
        };

        public static AppearanceEditorSlot FromBodyCategory(int category)
        {
            if (category < 0)
                return AppearanceEditorSlot.Head;
            foreach (AppearanceEditorSlot slot in Enum.GetValues(typeof(AppearanceEditorSlot)))
            {
                if (BodyCategory(slot) == category)
                    return slot;
            }
            return AppearanceEditorSlot.Head;
        }

        /// <summary>Palette position shown to players: column letter A-P, row number 1-11 (e.g. H1).</summary>
        public static string PaletteCode(int colorId)
        {
            if (colorId < 0 || colorId >= TintMap.TintMapMaterialRegistry.PaletteColorCount)
                return string.Empty;
            return $"{(char)('A' + colorId % 16)}{colorId / 16 + 1}";
        }
    }
}
