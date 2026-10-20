using System;
using System.Collections.Generic;
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
    }
}
