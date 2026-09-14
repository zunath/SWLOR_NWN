using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service.LightsaberWorkbenchService
{
    /// <summary>
    /// A selectable blade color available at the lightsaber workbench.
    /// Each color maps to different top weapon model part values depending on
    /// the weapon type and whether the chosen hilt is curved. A value of -1
    /// means the color is unavailable for that configuration.
    /// </summary>
    public class SaberBladeColor
    {
        /// <summary>
        /// Player-facing name of the color.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Top model part value for straight lightsaber hilts, or -1 if unavailable.
        /// </summary>
        public int StraightTopValue { get; }

        /// <summary>
        /// Top model part value for curved lightsaber hilts, or -1 if unavailable.
        /// </summary>
        public int CurvedTopValue { get; }

        /// <summary>
        /// Top model part value for saberstaffs, or -1 if unavailable.
        /// </summary>
        public int SaberstaffTopValue { get; }

        /// <summary>
        /// Texture resref displayed as the preview image in the workbench UI.
        /// </summary>
        public string PreviewResref { get; }

        /// <summary>
        /// Color of the dim light emitted by a saber built with this blade.
        /// </summary>
        public LightColor LightColor { get; }

        public SaberBladeColor(
            string name,
            int straightTopValue,
            int curvedTopValue,
            int saberstaffTopValue,
            string previewResref,
            LightColor lightColor)
        {
            Name = name;
            StraightTopValue = straightTopValue;
            CurvedTopValue = curvedTopValue;
            SaberstaffTopValue = saberstaffTopValue;
            PreviewResref = previewResref;
            LightColor = lightColor;
        }
    }
}
