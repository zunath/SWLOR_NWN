namespace SWLOR.Game.Server.Service.LightsaberWorkbenchService
{
    /// <summary>
    /// A selectable hilt (bottom weapon model part) available at the lightsaber workbench.
    /// </summary>
    public class SaberHiltPart
    {
        /// <summary>
        /// The weapon model part value applied to the bottom slot (e.g. 151 = model 15, variant 1).
        /// </summary>
        public int PartValue { get; }

        /// <summary>
        /// Player-facing name of the hilt.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Texture resref displayed as the preview image in the workbench UI.
        /// </summary>
        public string PreviewResref { get; }

        /// <summary>
        /// Curved hilts angle the emitter and require the curved set of blade models.
        /// </summary>
        public bool IsCurved { get; }

        public SaberHiltPart(int partValue, string name, string previewResref, bool isCurved = false)
        {
            PartValue = partValue;
            Name = name;
            PreviewResref = previewResref;
            IsCurved = isCurved;
        }
    }
}
