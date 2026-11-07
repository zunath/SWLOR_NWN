using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// The armor part styles offered to each playable appearance. The appearance editor and the
    /// saved-part repair share it, so a repaired part is always one the editor accepts.
    /// </summary>
    public static class ArmorAppearanceCatalog
    {
        public static IReadOnlyDictionary<AppearanceType, BaseArmorAppearanceDefinition> Definitions { get; } =
            new Dictionary<AppearanceType, BaseArmorAppearanceDefinition>
            {
                [AppearanceType.Human] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Bothan] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Chiss] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Zabrak] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Twilek] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Mirialan] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Echani] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.KelDor] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Cyborg] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Cathar] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Rodian] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Trandoshan] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Togruta] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Wookiee] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.MonCalamari] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Ugnaught] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Droid] = new DroidArmorAppearanceDefinition(),
                [AppearanceType.Nautolan] = new GeneralArmorAppearanceDefinition(),
                [AppearanceType.Ewok] = new GeneralArmorAppearanceDefinition(),
            };

        public static int[] GetStyles(BaseArmorAppearanceDefinition definition, AppearanceArmor part) => part switch
        {
            AppearanceArmor.Neck => definition.Neck,
            AppearanceArmor.Torso => definition.Torso,
            AppearanceArmor.Belt => definition.Belt,
            AppearanceArmor.Pelvis => definition.Pelvis,
            AppearanceArmor.LeftShoulder or AppearanceArmor.RightShoulder => definition.Shoulder,
            AppearanceArmor.LeftBicep or AppearanceArmor.RightBicep => definition.Bicep,
            AppearanceArmor.LeftForearm or AppearanceArmor.RightForearm => definition.Forearm,
            AppearanceArmor.LeftHand or AppearanceArmor.RightHand => definition.Hand,
            AppearanceArmor.LeftThigh or AppearanceArmor.RightThigh => definition.Thigh,
            AppearanceArmor.LeftShin or AppearanceArmor.RightShin => definition.Shin,
            AppearanceArmor.LeftFoot or AppearanceArmor.RightFoot => definition.Foot,
            AppearanceArmor.Robe => definition.Robe,
            _ => Array.Empty<int>(),
        };

        /// <summary>
        /// Styles 1 and 2 draw the wearer's own body part, exactly like 0, so they add no armor part. A list
        /// that offers 0 ("None") offers neither; a list without 0 keeps 1 as its bare option and drops 2. Armor
        /// already on 1 or 2 keeps it and stays editable, because moving it would change how the part takes dyes
        /// (SWLOR dyes the material named by the part number). Shoulders, belts and robes show nothing at 1 or 2
        /// and are repaired to 0 instead.
        /// </summary>
        public static IReadOnlyList<int> RetainedBareStyles { get; } = new[] { 1, 2 };

        /// <summary>True when armor saved with this style on this slot may be edited.</summary>
        public static bool AcceptsSavedStyle(BaseArmorAppearanceDefinition definition, AppearanceArmor part, int style) =>
            Offers(definition, part, style) || IsRetainedBareStyle(part, style);

        /// <summary>
        /// The style whose picture and name the editor shows: a retained bare style shows as the list's own
        /// bare option ("None" where the list offers 0, otherwise 1), since it looks exactly like it and its own
        /// picture shows a model the armor never draws.
        /// </summary>
        public static int DisplayedStyle(BaseArmorAppearanceDefinition definition, AppearanceArmor part, int style) =>
            IsRetainedBareStyle(part, style) && !Offers(definition, part, style)
                ? Offers(definition, part, 0) ? 0 : 1
                : style;

        /// <summary>The editor offers 0 ("None") on every neck list.</summary>
        private static bool Offers(BaseArmorAppearanceDefinition definition, AppearanceArmor part, int style) =>
            (part == AppearanceArmor.Neck && style == 0) || GetStyles(definition, part).Contains(style);

        private static bool IsRetainedBareStyle(AppearanceArmor part, int style) =>
            RetainedBareStyles.Contains(style) && part is not (AppearanceArmor.Belt or AppearanceArmor.LeftShoulder or
                AppearanceArmor.RightShoulder or AppearanceArmor.Robe or AppearanceArmor.Invalid);
    }
}
